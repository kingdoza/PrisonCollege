# PrisonCollege 튜토리얼 기능 설계 제안

작성일: 2026-07-14  
최종 갱신: 2026-07-15  
대상 구성: 신임 교수 연수 0~9단계

> 상태: **확정 정책 기준으로 코드 구현 가능 / Unity Editor 설정 일부 대기**  
> 확정 정책은 [TUTORIAL_POLICY_DECISIONS.md](./TUTORIAL_POLICY_DECISIONS.md)를 따른다. `T-01`~`T-06`은 승인 또는 확정됐다. `P-23`, `P-28`, `P-29`의 남은 Unity Editor 설정값은 코드에서 임의로 채우지 않는다.

## 1. 기술 구조 제안

확정 구조는 `SStage1`의 일반 웨이브 흐름에 조건문을 추가하는 대신 **전용 씬 + 단계 상태 머신 + 기존 스테이지 기능을 제어하는 어댑터**를 사용하는 것이다.

핵심 구조는 다음과 같다.

```text
MainScene
  -> TutorialStage 씬
      -> TutorialSceneBootstrap
          -> StageController (혼잡도/프로젝트/탈출/학생 집계 재사용)
          -> TutorialDirector (현재 단계와 전환의 유일한 소유자)
              -> TutorialStep (진입/달성/종료)
              -> TutorialActorDirector (DB 전체 학생 풀, 대기 공간, 단계별 선발과 행동 연출)
              -> TutorialCheckpointService (단계 기준 상태 복원)
              -> TutorialHUDPresenter (목표/안내/강조/결과)
          -> TutorialStageFacade (기존 게임 시스템의 공개 제어·관측 창구)
```

`TutorialDirector`가 `StageController`의 private 필드나 학생의 `Blackboard`를 직접 바꾸면 안 된다. 대신 기존 시스템에 최소한의 공개 이벤트와 제어 메서드를 추가하고, 튜토리얼은 `TutorialStageFacade`를 통해서만 이를 사용한다.

튜토리얼 프로젝트 보상은 세션 HUD에만 표시하고 정규 준비 자금으로 지급하지 않는다. 정규 웨이브·인벤토리 진행 데이터는 분리하며, 완료 상태만 저장한다. 6단계 장비 지급 정책은 `P-21`을 따르고 미니웨이브 로드아웃 구성은 `P-28`의 에디터 설정을 따른다.

## 2. 현재 코드에서 재사용할 부분과 보완할 부분

### 그대로 재사용할 기능

| 기능 | 현재 코드 | 재사용 내용 |
|---|---|---|
| 바리케이드 설치 | `ExitGate`, `ClickAndWait`, `PlayerInteraction` | F 입력, 시선 이탈/공격 시 설치 취소, 설치 진행도 |
| 탈출구 표시 | `FadeOutline` | 빨간색 점멸을 제어하는 래퍼를 추가해 사용 |
| 학생 행동 | `PostStudent`, Behaviour Tree, `BehaviorWeightSet` | 일반 행동과 미니웨이브 행동 수행 |
| 혼잡도 | `StageController`, `Stat`, `ChaosUI` | 수치, 증감률, 무고한 학생 페널티, 자연 감소 |
| 부스터 | `BoostReceiver`, `BoostData`, 투척 무기 | 작업 강제 효과와 투척/보충 흐름 |
| 학생 작업 | `PostStudent.IsWorking` | 작업 중 인원 집계 |
| 교수 작업 | `ProfessorTask` | F 시작, 이동 입력 시 중단, 작업 자세 |
| 프로젝트 | `StageController`의 프로젝트 진행/보상 | 학생·교수 기여, 완성 이벤트, 게이지 |
| 정규 HUD | `StageController`의 시간/혼잡도/탈출/작업/프로젝트 UI | 8단계에서 전체 표시 |

### 반드시 공개 포트가 필요한 부분

현재 구조에는 다음 제약이 있다.

- `StageController`가 준비 시간, 타이머, 혼잡도, 프로젝트, 승패, HUD를 직접 처리한다. 튜토리얼 초반에 타이머만 멈추거나 혼잡도 증가 원인만 제한할 수 있는 API가 없다.
- `StageController.Start()`는 `WaveSystem.CurrentWave`와 정규 인벤토리를 전제로 학생을 자동 스폰한다. 튜토리얼 씬을 직접 열면 웨이브 인덱스와 장비 상태가 잘못될 수 있다.
- `PostStudent.StartBehavior()`는 private이고 메인 스테이지의 애니메이션 이벤트 흐름에 결합돼 있다. 튜토리얼은 이 이벤트를 기다리지 않고 `T-06`의 전용 명시적 초기화 API로 전체 학생 행동 런타임을 0단계 전에 한 번 구성해야 한다.
- `PostStudent.Blackboard`를 외부에서 바꿀 수는 있지만, spot 해제·애니메이터 초기화·행동 트리 우선순위를 함께 처리하지 않아 안전한 강제 행동 수단이 아니다.
- `ExitGate.PlaceBarricade()`와 `BreakBarricade()`는 상태 변경 이벤트를 내보내지 않는다. 설치 수를 화면 문자열이나 매 프레임 폴링으로 판정하면 안 된다.
- `StageController.OnProjectSuccessed()`는 private이며 프로젝트 완성 이벤트가 없다.
- `ProfessorTask`는 작업 시작/중단 이벤트가 없다.
- `Microwave.RemoveFood()`는 정상/위험 음식 제거 결과를 외부에 알리지 않는다.

따라서 기존 기능을 복제하지 말고 아래의 작은 이벤트와 제어 API를 먼저 추가한다.

## 3. 런타임 구성

### 3.1 `TutorialDirector`

튜토리얼 진행 상태의 유일한 소유자다.

```csharp
public enum TutorialStepId
{
    Intro,
    Movement,
    Barricades,
    RiskResponse,
    InnocentStudent,
    ChaosDecay,
    StudentWork,
    ProfessorWork,
    MiniWave,
    CourseSummary
}
```

표시 번호 대신 위의 안정적인 ID를 저장한다. 기획 단계 번호가 다시 바뀌어도 세이브와 코드 분기가 깨지지 않는다.

단계 수명 주기는 항상 다음 순서를 따른다.

```text
Enter
  -> 단계 정책 적용
  -> 체크포인트 기준 상태 구성
  -> 목표/안내 UI 표시
  -> 초기화된 학생 풀에서 필요한 대상 선발 및 모드 전환
Active
  -> 게임 이벤트로 진행도 갱신
  -> 달성 조건 충족
Complete
  -> 완료 문구 표시 및 최소 확인 시간 보장
Exit
  -> 이벤트 구독 해제
  -> 강조/임시 대상/단계 전용 제한 해제
  -> 다음 단계 Enter
```

필수 규칙:

- 한 프레임에 두 번 완료돼도 다음 단계 전환은 한 번만 수행한다.
- `Enter` 중에는 달성 이벤트를 받지 않는다. 학생 행동 런타임은 0단계 진입 전에 이미 초기화돼 있으므로 단계 진입 중 Ready 이벤트나 polling을 기다리지 않는다.
- 단계가 끝나면 해당 단계가 추가한 이벤트 구독, 코루틴, DOTween, 임시 오브젝트를 모두 정리한다.
- 단계 전환 도중 플레이어 입력으로 다음 조건이 선입력되지 않도록 1프레임 입력 버퍼를 비운다.

### 3.2 단계 구현 방식

문구와 수치는 ScriptableObject에, 서로 다른 판정 로직은 코드에 둔다.

```text
TutorialCourseDefinition (ScriptableObject)
  - 표시 순서
  - 단계별 제목/목표/안내/완료 문구
  - 목표 수치
  - 6단계 작업 확인 시간
  - 힌트 지연 시간
  - 체크포인트 정의

TutorialStepBase
  - Enter / Exit / Restart
  - 진행도 갱신
  - CompleteOnce

Steps/
  - IntroStep
  - MovementStep
  - BarricadeStep
  - RiskResponseStep
  - InnocentStudentStep
  - ChaosDecayStep
  - StudentWorkStep
  - ProfessorWorkStep
  - MiniWaveStep
  - CourseSummaryStep
```

완전히 데이터만으로 모든 단계를 만들려 하면 조건 조합이 복잡해지고 Inspector 설정 오류가 늘어난다. 반대로 문구까지 코드에 넣으면 수정과 번역이 어렵다. 위의 혼합 구성이 현재 프로젝트 규모에 적합하다.

### 3.3 `TutorialStageFacade`

튜토리얼 코드가 기존 객체를 직접 찾아다니지 않게 하는 씬 단위 어댑터다.

제공할 읽기 상태:

- `TimerRemaining`
- `Chaos`, `ChaosRate`
- `EscapeCount`, `EscapeFailureThreshold`
- `ProjectProgress`
- `WorkingStudentCount`
- `IsProfessorWorking`
- 등록된 학생과 탈출구 목록

제공할 이벤트:

```text
ChaosChanged(current, delta, rate, reason)
EscapeCountChanged(current, threshold)
ProjectProgressChanged(previous, current)
ProjectCompleted(completionId, contributors)
WorkingStudentCountChanged(count)
StudentRegistered(student)
StudentDowned(student, hitInfo, wasHazardous)
StudentEscaped(student)
StageFinished(result)
```

`reason`은 최소한 `ContinuousHazard`, `InnocentDown`, `Escape`, `Gunshot`, `NormalFoodRemoved`, `NaturalDecay`, `Reset`으로 구분한다. 튜토리얼 판정이 단순히 “수치가 올랐다”만 보지 않고 올바른 원인을 확인할 수 있어야 한다.

제공할 제어:

```text
ApplyPolicy(TutorialStagePolicy)
SetChaos(value)
SetProjectProgress(value)
SetEscapeCount(value)
SetTimer(value)
RegisterStudent / UnregisterStudent
BeginTutorialMiniWave(config)
StopAllStageSimulation()
```

`Set...` 계열은 튜토리얼 또는 개발 모드에서만 허용하고 정규 스테이지 코드에서는 노출하지 않는다.

### 3.4 `TutorialStagePolicy`

단계 진입 때 이전 단계와의 차이만 바꾸지 말고, 모든 제한 상태를 완전하게 다시 적용한다. 그래야 재시작과 단계 건너뛰기가 동일하게 동작한다.

```csharp
[Serializable]
public struct TutorialStagePolicy
{
    public bool runTimer;
    public bool runProject;
    public bool allowContinuousChaosSources;
    public bool allowChaosDecay;
    public bool evaluateEscapeFailure;
    public bool allowAutonomousStudentBehavior;
    public bool allowProfessorTask;
    public bool showFullStageHud;
}
```

학생 피격에 의한 일회성 혼잡도 페널티와 지속 위험 행동에 의한 초당 혼잡도는 별도 플래그로 다룬다. 4-1에서는 무고한 학생 페널티는 허용하지만 다른 학생의 지속 혼잡도는 막아야 하기 때문이다.

### 3.5 정규 웨이브와 튜토리얼 규칙 분리

튜토리얼 진입을 위해 `WaveSystem.NewWaveEntered()`를 호출하면 전역 웨이브 상태가 오염된다. `StageController`가 사용할 규칙을 아래처럼 공급받게 한다.

```text
IStageRuntimeConfig
  - BehaviorWeightSet
  - ChaosFactor
  - ProjectFactor
  - Duration
  - EscapeFailureThreshold
  - AutoSpawnStudents
  - UseInventoryLoadout
  - FinishPolicy

NormalStageRuntimeConfig
  -> WaveSystem, GameManager, InventorySystem의 현재 값 사용

TutorialStageRuntimeConfig
  -> TutorialMiniWaveConfig와 연수용 장비 사용
```

튜토리얼은 정규 인벤토리 장비를 가져오지 않고 Inspector에서 직접 설정한 고정 로드아웃을 사용한다. 학생과 미니웨이브 수치는 튜토리얼 전용 runtime config와 `BehaviorWeightSet`에서 공급한다.

## 4. 학생 AI 연수 제어

### 4.1 시작 전 1회 행동 런타임 초기화 — `T-06`

현재 메인 스테이지는 학생 생성, 기상, 애니메이션 이벤트를 통한 `StartBehavior()`, 자율 행동 시작 순서를 사용한다. 이 정규 흐름은 변경하지 않는다.

튜토리얼에서는 `TutorialSceneBootstrap`이 `StudentDB` 전체 학생을 생성하고 필수 씬 참조를 연결한 뒤, `TutorialActorDirector`가 각 학생의 튜토리얼 전용 명시적 초기화 API를 한 번 호출한다. 이 호출은 `Blackboard`와 루트 Behaviour Tree를 동기적으로 구성하며, 전체 학생 호출이 반환된 뒤 `TutorialDirector`가 0단계에 진입한다.

초기화 API는 정규 `StartBehavior()`를 단순 공개해서 재사용하는 형태가 아니라, 중복 호출 방지와 튜토리얼 모드 평가 제어를 포함한 별도 공개 포트로 둔다. 정규 스테이지가 이 API를 호출하지 않으면 기존 애니메이션 이벤트 순서와 동작이 그대로 유지돼야 한다.

```text
TutorialSceneBootstrap
  -> StudentDB 전체 학생 생성
  -> 대기 슬롯, StageSpots, Player, 튜토리얼 BehaviorWeightSet 연결
  -> 각 학생 InitializeTutorialBehaviorRuntime() 1회 호출
      -> Blackboard 생성
      -> 루트 Behaviour Tree 생성 및 연결
      -> 행동 평가 비활성 상태로 유지
  -> 전체 호출 반환
  -> TutorialDirector가 0단계 Enter
```

`Loyalty`, `Standby`, `TrainingTransit`, `ReturnTransit`, `Cheer`에서는 루트 행동 트리를 평가하지 않는다. `Training`에서는 Director가 요청한 지정 행동만 수행하고, `MiniWave`에서는 연결된 튜토리얼 전용 `BehaviorWeightSet`으로 기존 자율 행동 트리를 평가한다.

3단계, 4-1, 6단계와 8단계의 학생 선발은 이미 초기화된 풀의 모드만 바꾼다. 별도 Ready 이벤트, `IsBehaviorReady` polling, 제한 시간, 학생 재스폰, 자동 복구 또는 자동 단계 재시작을 추가하지 않는다. `OnWorkTriggered()`를 포함해 `Blackboard`가 필요한 호출은 초기화 완료 후에만 가능한 모드에서 허용한다.

### 4.2 고정 행동 API

튜토리얼이 `Blackboard.destBehavior`와 `isForceBehavior`를 직접 설정하지 않도록 아래 API를 추가한다.

```csharp
public readonly struct ScriptedBehaviorRequest
{
    public string scenarioId;
    public BehaviorType behavior;
    public BehaveSpot fixedSpot;
    public bool holdUntilResolved;
}

BeginScriptedBehavior(request)
ResolveScriptedBehavior(scenarioId)
CancelScriptedBehavior(scenarioId)
```

내부에서는 이전 spot 해제, 협동 행동 탈퇴, 애니메이터/부착물 초기화, 목적 행동 설정을 한 번에 처리한다. `holdUntilResolved`의 종료 조건과 피격 후 행동 복귀 여부는 `P-11`, `P-13` 결정값을 따른다.

추가 텔레메트리:

```text
BehaviorSelected
BehaviorActionStarted
BehaviorInterrupted
BehaviorCompleted
```

`BehaviorActionStarted`는 목적 행동이 enum에 설정된 순간이 아니라, 학생이 spot에 도착해 실제 행동을 시작한 순간 발생해야 한다. 성공 판정 준비 상태를 여는 데 이 이벤트를 사용한다.

### 4.3 DB 학생 풀과 대기 공간

튜토리얼 씬 시작 시 `StudentDB`의 모든 학생을 생성해 `TutorialActorDirector`가 관리한다. 현재 DB 기준 9명이며, 각 학생은 고정 대기 슬롯과 현재 연출 모드를 가진다.

```csharp
public enum TutorialStudentMode
{
    Loyalty,
    Standby,
    TrainingTransit,
    Training,
    ReturnTransit,
    Cheer,
    MiniWave
}
```

`Loyalty`, `Standby`, `Cheer`에서는 학생이 화면에 보이고 지정 자세를 유지하지만 공격·부스터 효과, 자율 행동, 탈출, 작업, 혼잡도 원인과 단계/HUD 집계에서 제외된다.

`TrainingTransit`과 `ReturnTransit`에서도 공격과 부스터 효과를 차단한다. 연수 지점에 도착해 `Training`으로 전환된 학생 또는 8단계에서 `MiniWave`로 전환된 학생만 해당 단계의 효과 대상이 된다.

씬 연결값:

```text
TutorialActorDirector
  - waitingSlots[]              // DB 학생별 고정 대기 위치
  - trainingEntryNavPoint       // 대기 공간 밖 연수 출발 지점
  - waitingReturnNavPoint       // 대기 공간 인접 복귀 지점
```

연수 이동 규칙:

```text
대기 슬롯
  -> 연수용 NavMesh 이동 지점으로 Warp
  -> 연수 지점까지 달리기
  -> Training 전환 및 지정 행동 시작

연수 종료
  -> 대기 공간 인접 복귀 지점까지 달리기
  -> 원래 대기 슬롯으로 Warp
  -> Standby 전환
```

복귀 완료는 단계 전환 조건이 아니다. 다음 단계는 즉시 시작하고 이전 연수 학생의 복귀는 배경에서 진행한다. 3단계에서 쓰러진 학생은 단계가 끝난 뒤 자동 기상을 다시 허용하고, 기상 완료 후 복귀를 시작한다.

### 4.4 위험 표식

`P-14`의 사용자 결정에 따라 튜토리얼 전용 위험 표식을 사용하지 않는다. `TutorialDangerMarker`와 관련 외곽선 로직은 구현 범위에서 제외한다.

## 5. 단계별 기능 명세

아래 표의 “초기화”는 해당 단계의 체크포인트 기준 상태이기도 하다.

### 0. 시작 (`Intro`)

| 항목 | 설계 |
|---|---|
| 초기화 | DB 전체 학생을 일렬 배치하고 충성 자세 고정. 자율 AI·이동과 모든 공격·부스터 효과 차단 |
| UI | 목표와 안내 패널 표시. `Tab`만 진행 입력으로 표시 |
| 판정 | `TutorialInput.AdvancePressed` 1회 |
| 완료 | 안내 패널을 닫고 1단계 진입 |

중요: 현재 `StageController.Update()`도 준비 중 Tab을 처리한다. 튜토리얼 모드에서는 이 분기를 비활성화해 `TutorialDirector`만 Tab을 소비하게 한다.

### 1. 이동과 기본 조작 (`Movement`)

| 항목 | 설계 |
|---|---|
| 초기화 | `P-06`에서 확정한 목표 위치와 표시 오브젝트 활성화 |
| 학생 연출 | DB 전체 학생이 일렬 배치와 충성 자세 유지 |
| 목표 | `표시된 지점까지 이동하세요. (0/1)` |
| 판정 | 플레이어 전용 Trigger 진입. Transform 거리 폴링은 보조 수단으로만 사용 |
| 완료 | 목표 달성 시 현재 단계 UI 전체를 숨기고 즉시 다음 단계로 전환 |

마커는 교수 Collider만 받아야 하며, 낙하 복귀나 순간이동으로 진입해도 한 번만 완료한다.

### 2. 탈출구 봉쇄 (`Barricades`)

| 항목 | 설계 |
|---|---|
| 초기화 | 튜토리얼 씬 최초 진입 시 모든 문/창문을 미설치 상태로 구성. 2단계 진입 시에는 현재 상태를 유지해 이전 단계에서 설치된 대상도 진행도에 포함 |
| 학생 연출 | 전원이 대기 공간에서 Idle. 공격·부스터·자율 행동·이동과 게임 집계 제외 |
| 강조 | 모든 대상 출구를 빨간색으로 점멸하고 단계 완료까지 유지 |
| 목표 | 현재 설치 수 / 등록된 출구 수 |
| 판정 | 모든 등록 출구의 **현재** `IsBarricadePlaced == true` |
| 완료 | 모든 출구가 동시에 설치 상태일 때 완료 |

`ExitGate`에 `BarricadePlacedEvent`, `BarricadeBrokenEvent`, `SetBarricadeStateForSetup(bool)`를 추가한다. 2단계 학생은 대기 공간에 보이지만 활성 행동 학생이 아니므로 설치된 바리케이드를 파괴하지 않는다.

시선 이탈 취소와 공격 취소는 현재 `PlayerInteraction.CheckFocusLost()`와 `Professor.HandleWeaponAttack()`의 `CancelActiveInteraction()`을 그대로 재사용한다.

### 3. 위험 행동 구분과 대응 (`RiskResponse`)

DB 전체 학생 9명 중 8명을 무작위로 선택하고 정상 역할 4명과 위험 역할 4명을 배정한다. 나머지 1명은 대기 공간에 남으며 4-1 대상자로 보관한다. 선택된 8명은 연수용 이동 지점으로 순간이동한 뒤 각 행동 지점까지 달려간다.

| 정상 행동 | 위험 행동 | 행동 위치 |
|---|---|---|
| 숭배 | 탈출구 공격 | 기존 씬의 해당 행동 spot |
| 게임 | 해킹 | 기존 씬의 해당 행동 spot |
| 정상 노래 | 저질 노래 | 기존 씬의 해당 행동 spot |
| 춤 | 흡연 | 기존 씬의 해당 행동 spot |

각 학생은 단계가 끝날 때까지 지정된 행동을 유지한다. 위험 학생은 체력이 0이 될 때 저지 1회로 판정한다. 이 단계의 탈출구 공격과 해킹은 행동 연출만 재생하고 실제 바리케이드 피해·탈출·조명 OFF를 발생시키지 않는다. 학생 공격력은 0이며, 저질 노래와 흡연을 포함한 모든 혼잡도 증가 원인을 차단한다.

시나리오 상태:

```text
SelectEightActorsFromStudentPool
  -> 초기화된 풀에서 8명을 TrainingTransit으로 전환
  -> 연수용 이동 지점으로 Warp
  -> 각 행동 spot까지 달리기
  -> 지정 행동 시작 및 유지
  -> 위험 학생 체력 0
      -> 해당 학생 Resolved
      -> 위험 행동 저지 수 +1
  -> 4명 모두 Resolved
      -> 단계 완료
      -> 다음 단계 즉시 시작
      -> 8명은 배경에서 자동 기상 및 대기 공간 복귀
```

판정 규칙:

- 위험 학생은 체력 0 도달 시 학생별로 한 번만 성공 판정한다.
- 정상 학생 오공격도 실제 피해를 적용하지만 혼잡도, 문제 상태와 목표 진행도는 변경하지 않으며 경고도 표시하지 않는다.
- 3단계에서 체력 0이 된 정상 학생은 자동 기상하거나 재스폰하지 않는다. 체력 0이 된 위험 학생도 자동 기상하지 않는다.
- 3단계 종료 뒤에는 쓰러진 학생의 자동 기상을 다시 허용하고, 기상한 학생부터 배경에서 대기 공간으로 복귀시킨다.
- 지정 행동은 시간 초과 없이 유지한다.
- 같은 해결 이벤트의 중복 카운트 방지는 기술 요구사항으로 유지한다.

`0/4`는 체력이 0이 된 서로 다른 위험 학생 수다.

### 4-1. 무고한 대학원생 처치 (`InnocentStudent`)

| 항목 | 설계 |
|---|---|
| 초기화 | 혼잡도 0, 추가 혼잡도 원인 차단. 3단계에 선택되지 않은 대기 학생 1명을 연수 지점으로 이동 |
| 목표 | 무고한 학생을 쓰러뜨리기 |
| 판정 A | 플레이어가 해당 학생을 실제로 Down 상태로 만듦 |
| 판정 B | `ChaosChanged.reason == InnocentDown`이며 현재 혼잡도가 0보다 커짐 |
| 표시 확인 | 혼잡도 HUD가 새 revision을 한 프레임 이상 그린 뒤 완료 |

근접·총기·투척을 포함한 모든 공격 수단을 인정한다. 4-1을 포함한 튜토리얼 전체 학생의 체력 규칙과 값은 정규 스테이지 학생과 동일하게 사용하며 체력 1 보정을 두지 않는다. 혼잡도 페널티도 정규 스테이지의 무고한 학생 처치 페널티를 그대로 사용한다. 4-1 학생은 체력 0 이후 메인 스테이지와 동일하게 자동 기상한다. 판정 구현에서는 지정된 연수 학생 ID와 플레이어 공격자를 확인해야 한다.

단계 완료 후 해당 학생은 자동 기상과 대기 공간 복귀를 배경에서 진행하며, 4-2 진입은 복귀 완료를 기다리지 않는다.

### 4-2. 혼잡도 감소 (`ChaosDecay`)

| 항목 | 설계 |
|---|---|
| 초기화 | 4-1 학생의 배경 복귀 시작, 전투 타겟·위험 행동·화재·해킹 등 모든 증가 원인 제거. 현재 혼잡도는 유지 |
| 정책 | 지속 혼잡도 증가 금지, 자연 감소 허용, 타이머/프로젝트 정지 |
| 관찰 판정 | `ChaosRate < 0` 이벤트와 실제 수치 감소를 모두 한 번 이상 확인 |
| 강조 | 감소가 시작되는 순간 혼잡도 수치와 초당 변화량 UI 강조 |
| 완료 | Tab 입력 또는 혼잡도 0 도달. 감소 확인 전 Tab도 즉시 인정 |

혼잡도 감소 시작 대기 시간과 감소량은 본편 값을 그대로 사용한다.

### 6. 학생 작업 유도 (`StudentWork`)

| 항목 | 설계 |
|---|---|
| 초기화 | 프로젝트 진행도 0. 연수용 부스터를 빈 슬롯에 지급하고 탄약을 최대치로 설정 |
| 대상 학생 | 4-1에 참여하지 않았으며 배경 복귀를 완료한 `Standby` 학생 중 1명을 선택해 연수 지점으로 이동 |
| 부스터 | 작업 100%, 광분 0%인 별도 `BoostData` 사용 |
| 보충 | 자판기의 보충량·비용·횟수 제한은 메인 스테이지 정책 사용 |
| 장비 선택 | 지급 직후 연수용 부스터 슬롯 자동 선택 |
| 회수 | 빗나간 부스터는 회수할 수 없음 |
| 판정 시작 | 지정 학생의 `IsWorking == true`가 된 시점 |
| 완료 | 판정 시작 후 Inspector 설정값 `workConfirmationSeconds`가 지나면 성공 |
| 단계 종료 | 성공 처리와 동시에 지정 학생의 작업 강제 중단 |
| 복귀 | 단계 완료와 동시에 배경 복귀 시작. 다음 단계 전환은 기다리지 않음 |

권장 에셋:

```text
Assets/ScriptableObjects/Tutorial/BoostData_TutorialGuaranteedWork.asset
  workProbability = 1.0
  frenzyProbability = 0.0
```

정규 fresh 부스터 에셋은 수정하지 않는다. 연수용 부스터의 작업/광분 확률에는 패시브 보정을 적용하지 않고 100%/0%를 강제한다. 투척 속도와 공격 동작 등 나머지 무기 수치는 본편과 동일하게 사용한다.

학생이 작업하는 동안 프로젝트 게이지와 작업 중 인원 HUD는 본편 규칙대로 갱신하지만, 프로젝트 게이지 상승 자체는 더 이상 단계 완료 조건이 아니다. `workConfirmationSeconds`의 실제 값은 Unity Editor에서 설정한다.

확인 시간이 끝나기 전에 `IsWorking`이 false가 되면 누적 시간을 0으로 초기화하고, 다시 작업 상태가 될 때 처음부터 센다. 프로젝트가 확인 시간보다 먼저 완성되는 것은 허용하며 프로젝트 보상과 게이지 초기화는 튜토리얼 세션 규칙으로 처리한다. 프로젝트 완성만으로 6단계를 조기 완료하지는 않고 작업 확인 타이머 판정을 계속 사용한다.

### 7. 교수 작업 (`ProfessorWork`)

| 항목 | 설계 |
|---|---|
| 초기화 | 프로젝트 진행도 0. 6단계 학생은 작업 중단 상태 |
| 학생 정책 | 부스터 사용 입력은 허용하지만 학생의 부스터 수용을 차단하여 작업 상태가 될 수 없게 함 |
| 정책 | 프로젝트 진행 허용, 타이머/혼잡도 정지, 교수만 프로젝트에 기여 |
| 판정 | `ProjectCompleted`가 발생한 순간 지정 `ProfessorTask.IsTasking == true` |
| 완료 | 교수 작업 기여만으로 프로젝트 1회 완성 |

`ProfessorTask`에 `TaskStartedEvent`와 `TaskStoppedEvent(reason)`를 추가한다. 중단 이유는 `Movement`, `InteractToggle`, `Death`, `StepExit` 정도로 구분한다.

7단계에 한해 학생의 `BoostReceiver`가 연수용 부스터의 작업 효과를 거부하도록 단계 정책으로 차단한다. 플레이어의 장비 사용 입력과 투척 자체는 막지 않는다. 학생은 다른 경로로도 작업 상태가 될 수 없으며 프로젝트는 교수 기여만으로 완성한다. 프로젝트 보상은 튜토리얼 세션 HUD에만 표시하고 정규 준비 자금에는 반영하지 않는다.

### 8. 잔여시간 미니웨이브 (`MiniWave`)

진입 시 8단계 전용 체크포인트를 적용한다. 확정된 기본값은 제한 시간 30초, 탈출 허용 수 3명, 시작 혼잡도 0, 프로젝트 진행도 0, 학생 4명이다. 값은 Inspector에서 변경 가능하게 한다.

```text
플레이어: 시작 위치, 체력/스태미나
탈출구: 설치 여부와 체력
혼잡도/프로젝트/탈출 횟수/남은 시간
학생: 로스터, 위치, 행동 설정
장비: 로드아웃과 탄약
```

정책:

- 타이머, 프로젝트, 지속 혼잡도, 자율 학생 행동, 탈출 실패 판정을 모두 활성화한다.
- 정규 `StageOver`나 상점/아레나 전환은 사용하지 않고 결과를 `TutorialDirector`에 반환한다.
- 위험 표식은 사용하지 않는다.
- 전용 `BehaviorWeightSet`을 사용하며 실제 행동 가중치는 에디터에서 설정한다.
- 위험 행동 동시 수행 인원에는 별도 제한을 두지 않는다.
- 최초 진입 시 기존 학생 풀 9명 중 설정 인원 `n`명을 무작위 선택해 연수 출발용 이동 가능 지점으로 Warp한 직후 `MiniWave` 모드와 웨이브 행동을 시작한다. 별도의 메인 스폰 위치까지 달려가지 않는다. 기본 `n`은 4이며 Inspector에서 변경한다.
- 선택된 학생만 `MiniWave` 모드로 전환해 게임 행동과 HUD 집계에 포함한다.
- 선택되지 않은 학생은 대기 공간에서 응원 자세를 유지하고 공격·부스터·미니웨이브 행동과 집계에서 제외한다.

표시 HUD:

- 남은 시간
- 탈출 횟수 / 허용 횟수
- 혼잡도와 초당 변화량
- 작업 중 학생 수
- 프로젝트 게이지
- 등록된 탈출구별 바리케이드 상태와 체력

시간 종료 시 탈출 횟수가 허용 수 미만이면 성공한다. 탈출 횟수가 허용 수 이상이 되는 순간 즉시 실패한다.

```text
timer == 0 && escapeCount < allowedEscapeCount  -> 성공
timer == 0 && escapeCount >= allowedEscapeCount -> 실패
escapeCount >= allowedEscapeCount               -> 즉시 실패
```

탈출 횟수 UI는 요구사항대로 `탈출 횟수 / 허용 횟수`로 표시한다.

실패 패널:

- 실패 즉시 `8단계 다시 시작`과 `건너뛰기` 버튼을 표시한다.
- 건너뛰기는 별도 확인 팝업 없이 즉시 9단계로 이동하고 튜토리얼 완료로 판정한다.

성공 조건을 만족하면 현재 단계 UI를 숨기고 다음 단계로 전환한다.

### 9. 상점·멀티웨이브 안내 (`CourseSummary`)

진입 시 모든 학생 AI와 스테이지 시뮬레이션을 정지하고 커서를 표시한다.

- 정규 스테이지 1~4웨이브 설명 표시
- 2웨이브 뒤 아레나 베팅 안내
- 4웨이브 자발적 작업 없음 안내
- `재연수`: 확인 팝업 없이 튜토리얼 처음부터 다시 시작하며 기존 완료 기록은 유지
- `메인 메뉴`: 완료 상태를 저장한 뒤 메인 메뉴로 이동

튜토리얼 완료 상태는 저장한다. 단계별 진행은 저장하지 않으며 도중에 종료했다가 다시 진입하면 처음부터 시작한다.

## 6. 체크포인트 설계

Behaviour Tree와 NavMeshAgent의 런타임 내부 상태를 그대로 직렬화하는 대신 단계 상태를 다시 구성하는 **레시피형 체크포인트**를 사용한다. 채택 여부는 `T-04`, 복원 항목은 `P-32`, 사용 위치는 `P-33`에서 확정됐다.

8단계는 최초 진입 직후 플레이어, 장비, 수치, 탈출구, 학생, 시설과 임시 자금의 의미 있는 상태를 모두 캡처한다. 재시작 시 이 캡처를 기준으로 모든 상태를 복원한다. Behaviour Tree와 NavMeshAgent 내부 런타임 객체는 저장하지 않는다. `P-36`과 `T-06`에 따라 씬 시작 시 생성하고 초기화한 DB 전체 학생 풀은 유지하며, 최초 진입 때 선택된 학생 로스터와 의미 상태를 이용해 같은 학생 인스턴스의 행동 런타임과 모드를 동기적으로 재구성한다.

```csharp
[Serializable]
public class TutorialCheckpointDefinition
{
    public TutorialStepId stepId;
    public TransformId playerSpawn;
    public float chaos;
    public float projectProgress;
    public float timer;
    public int escapeCount;
    public GateSetup[] gates;
    public LoadoutId loadout;
    public ActorScenarioId actorScenario;
}
```

레시피형 방식을 채택할 경우의 복원 순서 제안:

1. 현재 단계 입력과 시뮬레이션 정지
2. 단계 이벤트 구독/코루틴/트윈 정리
3. 튜토리얼 발사체와 임시 위험 오브젝트 제거. DB 전체 학생 풀은 제거하지 않음
4. 화재, 조명 해킹, 전자레인지, 교수 작업 상태 종료
5. `P-32`에서 복원 대상으로 확정한 플레이어·월드·수치 상태 적용
6. `P-32`에서 복원 대상으로 확정한 탈출구·장비 상태 적용
7. 기존 학생 풀의 체력, 위치, 모드와 행동 런타임을 최초 8단계 진입 기준으로 동기적으로 재구성
8. 단계 정책 적용 후 입력 재개

학생 풀을 파괴하거나 재스폰하지 않으며 별도 Ready 확인도 하지 않는다. Behaviour Tree 내부 상태는 저장본을 주입하지 않고 튜토리얼 전용 reset API를 통해 기준 상태로 재구성한다.

8단계 실패 화면의 `8단계 다시 시작`만 위의 전체 상태 복원을 사용한다. 다른 단계에는 현재 단계 체크포인트 재시작을 제공하지 않는다. ESC 일시정지 메뉴에서 재시작을 선택하면 현재 단계와 관계없이 튜토리얼 전체를 0단계부터 다시 구성한다. 일반적인 비정상 상태 감지와 자동 복구·자동 재시작 기능은 구현하지 않는다. 세션 종료 후에도 단계 진행을 저장하지 않고 처음부터 시작한다.

## 7. UI 설계

### 7.1 `TutorialHUDPresenter`

구성:

```text
TutorialCanvas
  - ObjectivePanel
      - ObjectiveText
      - ProgressText
  - GuidePanel
      - Title
      - Body
      - InputHint
  - CompletionToast
  - ContextHint
  - WrongActionFeedback
  - MiniWaveResultPanel
```

UI는 게임 상태를 판정하지 않는다. `TutorialStep`이 상태 이벤트를 판정하고 Presenter에 표시 모델을 전달한다.

목표 진행도는 요구사항에 수치가 있는 단계에서 `0/n` 형식으로 표현한다. 목표 달성 시 현재 단계 UI 전체를 한 번에 숨긴다.

### 7.2 강조

- 월드 대상: `TutorialHighlightTarget` ID로 조회
- UI 대상: `TutorialUIHighlight`가 CanvasGroup/Outline을 점멸
- 단계 종료 시 `ClearAllHighlights()` 강제 호출
- 게임 오브젝트 이름이나 `Find()` 문자열에 의존하지 않음

탈출구에는 기존 `FadeOutline`을 재사용하되 `TutorialHighlighter`가 원래 색상·폭·enabled 값을 저장하고 종료 때 복원한다. 기존 `OutlineFader`는 준비 단계 `StageStartEvent`에 결합돼 있으므로 튜토리얼 단계 강조 용도로 직접 사용하지 않는다.

### 7.3 안내와 입력 충돌

안내 UI는 게임을 정지하지 않는 오버레이로 표시하며, 소개 전 기능을 포함해 플레이어 입력을 차단하지 않는다.

입력은 가능하면 `TutorialInput` 한 곳에서 Tab과 결과 버튼을 처리한다. 기존 클래스가 직접 폴링하는 Tab과 중복되지 않게 한다.

## 8. 기존 클래스 변경 목록

| 파일/클래스 | 필요한 변경 | 정규 게임 영향 |
|---|---|---|
| `StageController` | 런타임 config 적용, auto spawn 선택, 학생 등록 API, 시뮬레이션 policy, 상태 이벤트, 커스텀 finish policy | 기본 config에서는 기존 동작 유지 |
| `PostStudent` | 튜토리얼 전용 1회 행동 런타임 초기화·reset API, 모드별 평가 제어, scripted behavior API와 행동 단계 텔레메트리 | 정규 AI는 전용 API를 호출하지 않으며 기존 애니메이션 이벤트와 랜덤 행동 유지 |
| `ExitGate` | 설치/파괴 이벤트, setup 전용 상태 설정, 설치 대상 ID | 이벤트 추가 외 기존 동작 유지 |
| `ProfessorTask` | 작업 시작/중단 이벤트와 중단 이유, 강제 종료 API | 기존 상호작용 유지 |
| `Microwave` | 위험 여부를 포함한 음식 제거/화재 이벤트, setup reset API | 기존 페널티 유지 |
| `StageSpots` | 튜토리얼 행동 지점 직접 연결 지원 | 정규 랜덤 조회 유지 |
| `WeaponController` | 연수용 loadout 직접 설정, 선택 무기/탄약 복원 | 정규 인벤토리 로드 유지 |
| `CharacterRagdoll` | 튜토리얼 대상 자동 기상 제어 또는 강제 정리 API | 기본 자동 기상 유지 |
| `TutorialActorDirector` | DB 전체 학생 생성, 고정 대기 슬롯, 단계별 무작위 선발, 연수 이동·배경 복귀, 대기 효과 차단 | 튜토리얼 씬에서만 사용 |
| `GameManager` | `StartTutorial()`, `ExitTutorialToMain()` | 일반 스테이지 흐름과 분리 |

새 파일 권장 위치:

```text
Assets/Scripts/Tutorial/
  TutorialDirector.cs
  TutorialSceneBootstrap.cs
  TutorialStageFacade.cs
  TutorialStagePolicy.cs
  TutorialActorDirector.cs
  TutorialCheckpointService.cs
  TutorialHUDPresenter.cs
  TutorialHighlighter.cs
  TutorialInput.cs
  Steps/*.cs

Assets/ScriptableObjects/Tutorial/
  TutorialCourseDefinition.asset
  TutorialStageRuntimeConfig.asset
  TutorialMiniWaveConfig.asset
  TutorialRiskScenarios.asset
  BoostData_TutorialGuaranteedWork.asset
  BehaviorWeightSet_TutorialMiniWave.asset

Assets/Scenes/Tutorial/
  TutorialStage.unity
```

`TutorialStage.unity`는 `SStage1` 환경을 복제하거나 Prefab화한 공통 스테이지 루트를 사용하되, 씬 자체는 분리한다. 완성 후 `EditorBuildSettings`에 추가한다.

## 9. 구현 순서

사용자가 우선 준비 대상으로 지정한 네 항목을 기반으로 다음 순서가 적합하다.

### 1차: 기반 계약

1. `T-06`에 따라 Ready 상태나 이벤트 없이 튜토리얼 전용 1회 행동 런타임 초기화·reset API 추가
2. `StageController` 상태 이벤트와 tutorial policy/config 경계 추가
3. `ExitGate`, `ProfessorTask`, `Microwave` 결과 이벤트 추가
4. 정규 스테이지 회귀 테스트

### 2차: 튜토리얼 골격

1. 전용 씬과 `TutorialDirector`
2. 목표/안내 HUD
3. 레시피형 체크포인트와 현재 단계 재시작
4. `TutorialHighlighter`, 이동 마커
5. 0~2단계 완성

### 3차: 학생 연출

1. DB 전체 학생 풀과 `Loyalty`/`Standby`/`Cheer` 모드
2. 연수 진입 Warp·달리기와 비동기 복귀 흐름
3. scripted behavior API
4. 행동 실제 시작 텔레메트리
5. 3단계 무작위 8명 행동과 위험 학생 4명 판정
6. 정상 학생 오공격 피해·진행도·혼잡도 차단 정책

### 4차: 혼잡도와 작업

1. 혼잡도 reason/rate 이벤트
2. 4-1, 4-2
3. 확정 성공 부스터와 자판기
4. 6, 7단계

### 5차: 미니웨이브와 마무리

1. 미니웨이브 config와 전체 HUD
2. 8단계 성공/실패/재시작/건너뛰기
3. 9단계와 `P-02`, `P-35`에서 확정한 완료 처리
4. 메인 메뉴 진입 버튼과 빌드 씬 등록

## 10. 수용 테스트

아래에서 정책 ID가 붙은 항목은 해당 결정값이 확정된 뒤 기대 결과를 치환한다. 현재 문구는 확정 테스트가 아니다.

### 진행과 복원

- 0단계 진입 전에 DB 전체 학생의 튜토리얼 행동 런타임 초기화 호출이 완료되며, 이후 단계 선발에서 Ready 이벤트나 polling을 기다리지 않는다.
- Intro에서 Tab 한 번에 정확히 Movement로 이동한다. 그 외 입력과 타이머 처리는 `P-04`, `P-05` 결정값과 일치한다.
- 0~1단계에는 DB 전체 학생이 일렬로 충성 자세를 유지하고, 2단계부터 대기 공간의 Idle 자세로 전환한다.
- 충성·대기·응원 상태 학생은 공격과 부스터 영향을 받지 않고 모든 게임/HUD 집계에서 제외된다.
- 연수 지점 이동 중과 대기 공간 복귀 중에도 공격과 부스터 영향을 받지 않는다.
- 체크포인트 재시작 노출 단계와 복원 상태가 `P-32`, `P-33` 결정값과 일치한다.
- 단계 재시작을 10회 반복해도 이벤트가 중복 실행되거나 목표 카운트가 한 번에 2 증가하지 않는다.

### 바리케이드

- 지정 출구가 n개면 목표가 정확히 `0/n`으로 시작한다.
- 설치 완료 후 해당 출구 강조와 카운트가 `P-07`, `P-08` 결정값대로 바뀐다.
- 설치 중 시선 이탈과 좌클릭 공격으로 진행도가 0으로 취소된다.
- 설치된 출구 파괴 시 카운트가 `P-08` 결정값대로 처리된다.

### 위험 행동

- `P-10`에서 정한 학생은 실제 지정 행동을 시작한 뒤에만 판정한다.
- 정상 학생 오공격은 실제 피해만 적용하고 혼잡도·문제 상태·진행도·경고를 발생시키지 않으며, 체력 0인 학생을 복구하거나 재스폰하지 않는다.
- 3단계의 정상·위험 학생은 체력 0 이후 자동 기상하지 않으며, 그 외 단계의 학생은 메인 스테이지와 동일하게 자동 기상한다.
- 탈출구 공격과 해킹은 연출만 수행하고 실제 바리케이드 피해·탈출·조명 OFF를 발생시키지 않는다.
- 총기 발사, 정상 학생 처치, 저질 노래와 흡연을 포함한 어떤 원인으로도 3단계 혼잡도가 증가하지 않는다.
- 위험 행동을 올바른 방법으로 해결할 때만 문제당 정확히 1 증가한다.
- 네 번째 해결 뒤 `(4/4)`가 보인 다음 단계로 넘어간다.
- 3단계 완료 직후 다음 단계가 시작되고, 연수 학생의 자동 기상과 대기 공간 복귀는 배경에서 진행된다.
- 위험 표식이 생성되거나 표시되지 않는다.

### 혼잡도

- 모든 튜토리얼 학생의 체력이 정규 스테이지 학생과 동일하며 튜토리얼 전용 체력 1 보정이 적용되지 않는다.
- 4-1 시작 시 혼잡도는 정확히 0이며 다른 증가 요인이 없다.
- 지정 무고한 학생을 플레이어가 쓰러뜨렸을 때만 `InnocentDown` 증가가 기록된다.
- 4-2 감소 시작 지연과 감소량이 본편과 일치한다.
- 4-2는 감소 확인 전 Tab도 즉시 완료로 인정한다.

### 작업

- 연수용 부스터를 반복 시험해 작업 실패와 광분이 발생하지 않는다.
- 정규 fresh 부스터 에셋의 20%/5% 값은 바뀌지 않는다.
- 학생이 실제 작업 상태가 된 후 Inspector의 `workConfirmationSeconds`가 지나야 6단계가 완료되고, 완료와 동시에 작업이 중단된다.
- 프로젝트 게이지 상승은 표시되지만 6단계 완료 조건으로 사용하지 않는다.
- 6단계 작업이 확인 시간 전에 끊기면 타이머가 0으로 초기화되며, 프로젝트 선완성은 허용된다.
- 7단계 학생은 부스터에 맞아도 작업 상태가 되지 않으며 교수 기여만으로 프로젝트가 완성된다.
- 이동 입력으로 교수 작업이 중단된다.

### 미니웨이브와 진행 데이터

- 시간 종료 시 탈출 수가 허용 수 미만이면 성공한다.
- 탈출 수가 허용 수 이상일 때 실패를 판정하는 시점이 `P-26` 결정값과 일치한다.
- 8단계 최초 진입 때 모든 의미 상태를 캡처하고, 재시작 후 플레이어·장비·수치·탈출구·학생·시설·임시 자금이 모두 그 상태와 일치한다.
- 최초 미니웨이브 진입에서는 기존 학생 풀 전체에서 `n`명을 무작위 선택해 연수 출발 지점으로 순간이동시킨 직후 웨이브 행동을 시작하고, 재시작에서는 최초 진입 때 선택된 로스터와 상태가 복원된다.
- 미선택 학생은 대기 공간에서 응원 자세를 유지하며 미니웨이브 행동·피격·부스터·HUD 집계에서 제외된다.
- 건너뛰기는 확인 팝업 없이 즉시 9단계로 이동하고 완료로 판정된다.
- 돈·장비·웨이브·완료 기록이 `P-02`, `P-25`, `T-05` 결정값과 일치한다.

## 11. 확정된 기획 문구 적용사항

1. 화면 표시 번호는 사용자 결정에 따라 5-1/5-2 대신 4-1/4-2를 사용한다.
2. 기존 5-2의 사전 동작에 적힌 “5-2 달성 후”는 4-2 기준으로 “4-1 달성 후”로 정정한다.
3. 미니웨이브는 탈출 수가 Inspector에 설정된 허용 수 이상이 되는 순간 실패하고, 시간 종료 시 그 수보다 적으면 성공한다.
4. 9단계 제목 다음의 `적용됩니다.`는 앞 문장을 새로 작성하지 않고 제공된 문장 그대로 표시한다.

0단계 안내의 괄호 문장도 제공된 원문에 포함된 문구이므로 임의로 삭제하거나 합치지 않는다.
