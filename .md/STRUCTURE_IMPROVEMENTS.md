# PrisonCollege 구조 개선사항

작성일: 2026-06-14

## 요약

현재 구조는 게임잼 또는 빠른 기능 구현에는 적합하지만, 유지보수 관점에서는 몇 가지 위험이 크다.

가장 큰 개선 포인트는 다음 순서다.

1. 자동 생성 싱글턴의 위험 제거
2. `PostStudent` 책임 분리
3. `StageController`의 UI/게임 규칙/이벤트 처리 분리
4. AI Behaviour Tree 노드의 생명주기와 이벤트 구독 안정화
5. ScriptableObject 런타임 변형 방지
6. 입력 키/문서/코드 불일치 정리
7. `_GameJam`, `_deprecated`, 본편 코드 경계 명확화

## 1. 싱글턴 자동 생성 정책 개선

### 현재 상태

`PersistentSingleton<T>`와 `SceneSingleton<T>` 모두 `Instance` 접근 시 씬에 인스턴스가 없으면 새 GameObject를 생성한다.

문제:

- Unity Inspector에서 주입해야 하는 필드가 비어 있는 인스턴스가 생성될 수 있다.
- `StageController`, `AttributeSystem`, `FireSuppressionSystem`처럼 씬 참조가 필수인 타입이 자동 생성되면 런타임 오류로 이어진다.
- 초기화 순서 문제가 숨겨진다.

### 개선안

씬 배치가 필수인 매니저는 자동 생성하지 않도록 분리한다.

권장 구조:

```text
PersistentSingleton<T>
  - 전역 유지용
  - 자동 생성 금지 또는 명시 옵션화

SceneSingleton<T>
  - 씬 배치 필수
  - 없으면 에러 로그 후 null 반환

Bootstrapper
  - 반드시 필요한 전역 매니저를 MainScene 또는 초기화 씬에서 명시적으로 생성/검증
```

예상 효과:

- 잘못된 씬 구성 문제를 빠르게 발견할 수 있다.
- null 필드 싱글턴이 조용히 생기는 문제를 줄인다.
- 씬 의존성이 명확해진다.

## 2. PostStudent 책임 분리

### 현재 상태

`PostStudent.cs`는 약 800줄 이상이며 다음 책임을 모두 가진다.

- 컴포넌트 참조 수집
- AI 트리 생성
- Blackboard 생성
- 데미지/부스트 이벤트 처리
- 사망/래그돌/기상 처리
- 애니메이션 부착물 관리
- 학생 상태 판정
- 탈출 이벤트 처리
- 교수 공격 타겟 해제

문제:

- 학생 행동 하나를 수정하려 해도 피격, 사망, 부스트, 탈출, 협동 행동까지 함께 이해해야 한다.
- AI 트리 생성 코드가 길어져 테스트와 리뷰가 어렵다.
- 데이터와 실행 로직이 강하게 섞여 있다.

### 개선안

`PostStudent`를 얇은 facade로 축소하고 책임을 컴포넌트로 나눈다.

제안 구조:

```text
PostStudent
  - 학생 식별자와 public facade
  - 하위 컴포넌트 연결
  - 주요 이벤트 expose

StudentBrain
  - Blackboard 생성
  - Behaviour Tree tick
  - 루트 트리 교체/재시작

StudentBehaviorTreeFactory
  - 행동 트리 조립 전담
  - BehaviorType -> BT_Node 매핑

StudentVitals
  - DamageReceiver/BoostReceiver 연결
  - 피격/사망/회복 이벤트

StudentRagdollController
  - 래그돌 trigger, impact, standup

StudentAnimationState
  - animator bool/trigger/layer reset
  - attachers hide/show

StudentStatus
  - IsWorking, IsCausingChaos, IsDoingHazardBehavior 등 상태 계산
```

우선순위 높은 1차 분리:

1. `ConstructBehaviorTree()`를 별도 factory로 이동
2. 사망/래그돌 처리를 `StudentVitals` 또는 `StudentRagdollController`로 이동
3. `IsWorking`, `IsCausingChaos`, `IsDoingHazardBehavior` 계산을 별도 상태 클래스로 이동

## 3. StageController 비대화 완화

### 현재 상태

`StageController`는 스테이지의 거의 모든 런타임 책임을 가진다.

역할:

- 스테이지 타이머
- 준비 타이머
- 혼잡도 계산
- 프로젝트 진행도 계산
- 학생 스폰
- 학생 사망/탈출 이벤트 처리
- UI 직접 갱신
- 보상/돈 관리
- 게임오버 처리
- 장착 UI 갱신
- 반사 프로브 렌더
- 난이도 반영

문제:

- 변경 이유가 너무 많다.
- UI 변경과 게임 규칙 변경이 같은 파일에서 충돌한다.
- 스테이지 규칙을 테스트하기 어렵다.

### 개선안

역할별 하위 클래스로 분리한다.

제안 구조:

```text
StageController
  - 스테이지 수명주기 orchestration
  - 하위 시스템 연결

StageRuntimeState
  - money, workingStudentCount, isPreparing 등 상태 보관

StageTimer
  - 준비 시간, 제한 시간

ChaosController
  - 혼잡도 증가/감소 규칙
  - 학생 위험 행동, 탈출, 총기 발사, 무고한 학생 공격 반영

ProjectProgressController
  - 학생/교수 작업 진행도 계산
  - 프로젝트 완료 보상

StageStudentController
  - 학생 스폰
  - 학생 이벤트 구독/해제

StageHudPresenter
  - TMP, Image, EquipInfo 등 UI 갱신

StageEndController
  - 성공/실패/웨이브 종료/클리어 처리
```

단계적 적용:

1. UI 갱신만 `StageHudPresenter`로 분리
2. 혼잡도 계산을 `ChaosController`로 분리
3. 프로젝트 진행 계산을 `ProjectProgressController`로 분리
4. 학생 이벤트 구독을 `StageStudentController`로 분리

## 4. Behaviour Tree 노드 이벤트 구독 안정화

### 현재 상태

AI 노드는 C# 객체로 직접 생성되고, 일부 노드는 이벤트 구독/해제를 직접 수행한다.

위험 요소:

- 람다로 구독한 이벤트를 다른 람다로 해제하면 실제 해제가 되지 않는다.
- 노드가 Reset될 때 모든 상태가 안정적으로 정리된다는 보장이 약하다.
- `ActionNode(null, NodeState.Running)`처럼 영구 Running 노드가 있어 흐름 추적이 어렵다.

### 개선안

BT 노드에 명확한 생명주기 규약을 둔다.

권장 규약:

```text
SetBlackboard()
  - 읽기 전용 참조 연결

Enter()
  - 최초 진입 시 1회 실행

Tick()
  - 매 프레임 실행

Exit()
  - Success/Failure/Reset/Interrupt 시 반드시 호출

Reset()
  - 내부 상태 초기화
```

이벤트 구독 규칙:

- 구독 delegate를 필드에 보관한다.
- `Exit()` 또는 `Dispose()`에서 같은 delegate로 해제한다.
- 노드 생성자에서는 Unity 객체 이벤트를 구독하지 않는다.

추가 개선:

- `BT_Node`에 debug name을 부여한다.
- 현재 실행 중인 노드 경로를 디버그 UI 또는 로그로 출력할 수 있게 한다.
- `PatternNode`와 leaf node를 파일 단위로 더 작게 분리한다.

## 5. ScriptableObject 런타임 변형 안정화

### 현재 상태

무기와 행동 확률은 일부 deep copy를 수행한다.

예:

- `BehaviorWeightSet.CreateDeepCopy()`
- `WeaponBase.DeepCopyWeaponData()`

하지만 `ScriptableObject`는 에셋 자체가 공유되므로 런타임에 직접 값을 바꾸면 다른 인스턴스에 영향이 갈 수 있다.

주의 지점:

- `Projectile.Start()`에서 `WeaponData.effect.value`와 `WeaponData.hitImpulse`를 직접 변경한다.
- 무기 데이터 deep copy 범위가 파생 무기와 발사체 전체에서 일관적인지 확인이 필요하다.

### 개선안

런타임용 데이터와 에셋 데이터를 명확히 분리한다.

제안:

```text
WeaponData ScriptableObject
  - 원본 설정 에셋

RuntimeWeaponStats
  - 런타임 복사본
  - damage, staminaCost, hitImpulse, spread 등 순수 값

EffectData ScriptableObject
  - 원본 효과 타입/프리팹 설정

RuntimeEffect
  - value, receiver type, visual prefab 참조
```

단기 개선:

- 발사체에 `WeaponData` 전체를 넘기지 말고 필요한 값만 복사한 runtime struct를 넘긴다.
- `EffectData.value`를 직접 수정하지 않고 최종 데미지 값을 별도 변수로 전달한다.

## 6. EffectReceiver 버그 후보 수정

### 현재 상태

`EffectReceiver.IncreaseStat()` 내부가 `EffectedStat.Decrease(amount)`와 `StatDownEvent`를 호출한다.

현재 코드 의도상 증가 함수라면 다음이 자연스럽다.

```text
EffectedStat.Increase(amount)
StatUpEvent.Invoke(...)
MaxReachEvent.Invoke(...)
```

### 개선안

`IncreaseStat()` 동작을 실제 증가로 수정한다.

검증 필요:

- 현재 `IncreaseStat()`을 호출하는 코드가 있는지 검색
- 호출자가 기존 잘못된 동작에 의존하고 있지 않은지 확인
- boost/heal 계열 테스트

## 7. 입력 키 불일치 정리

### 현재 상태

`README.md`에는 상호작용 키가 `E`로 작성되어 있다. 실제 `PlayerInteraction`은 `F` 키를 사용한다.

문제:

- 플레이어 문서와 실제 조작이 다르다.
- `IPlayerInteractable.InteractionPrompt`에도 `[F]`가 표시된다.

### 개선안

입력 키를 한 곳에서 관리한다.

제안:

```text
InputSettings
  - interactKey
  - attackButton
  - sprintKey
  - menuKey

PlayerInteraction
  - InputSettings.InteractKey 사용

UI Prompt
  - InputSettings에서 키 이름 생성

README
  - 실제 키와 동기화
```

단기적으로는 README를 `F`로 수정하거나 코드 입력을 `E`로 변경해야 한다.

## 8. AttributeSystem 초기화/해제 정책 개선

### 현재 상태

`AttributeSystem`은 SceneSingleton이고, 각 modifier는 인스턴스 생성 시 새로 만들어진다. 패시브 아이템은 `StageController.Awake()`에서 활성화된다.

위험:

- 스테이지 씬에서 `AttributeSystem.Instance` 접근 순서가 중요하다.
- 패시브 적용 타이밍이 `StageController`에 묶여 있다.
- 어떤 패시브가 어떤 modifier를 바꾸는지 추적이 어렵다.

### 개선안

패시브 적용을 별도 서비스로 분리한다.

제안:

```text
AttributeSystem
  - modifier 저장소 역할만 수행
  - ResetAll() 제공

PassiveItemApplier
  - InventorySystem의 구매 목록을 읽음
  - AttributeSystem에 적용
  - 적용 로그 또는 디버그 목록 제공
```

추가 제안:

- `AttributeModifier`에 source id를 가진 modifier entry를 추가한다.
- `AddPercent(float)` 단순 누적 대신 `AddModifier(source, value)` 형식으로 추적 가능하게 한다.

## 9. Stage와 UI의 의존성 줄이기

### 현재 상태

`StageController`가 TMP, Image, CanvasGroup, EquipInfo 등 UI 컴포넌트를 직접 참조한다.

문제:

- UI 프리팹 변경이 스테이지 규칙 코드 변경으로 이어진다.
- 자동 테스트가 어렵다.
- 같은 스테이지 규칙을 다른 HUD에 재사용하기 어렵다.

### 개선안

Stage runtime model과 presenter를 분리한다.

예:

```text
StageRuntimeSnapshot
  - time
  - chaos
  - escapeCount
  - maxEscapeCount
  - money
  - workingStudentCount
  - projectRatio
  - chaosDeltaPerSecond

StageHudPresenter.Render(snapshot)
```

`StageController.Update()`는 계산만 하고, UI는 snapshot을 받아 렌더링한다.

## 10. 데이터 중심 설정 정리

### 현재 상태

스테이지별 수치가 `StageController`의 serialized field로 씬마다 박혀 있다.

예:

- 혼잡도 기본 감소량
- 학생당 혼잡도 증가량
- 탈출 패널티
- 총기 패널티
- 학생 작업 진행량
- 교수 작업 진행량
- 프로젝트 보상

문제:

- 스테이지별 밸런스 비교가 어렵다.
- 씬 복사 시 값이 달라져도 추적하기 어렵다.
- Git diff가 YAML 위주로 커진다.

### 개선안

스테이지 설정 ScriptableObject를 만든다.

제안:

```text
StageConfig : ScriptableObject
  - stageNumber
  - timer
  - prepareTime
  - chaosSettings
  - projectSettings
  - escapeSettings
  - spawnSettings
  - lightingSettings
```

`StageController`는 `StageConfig` 하나를 참조하고, 씬에는 참조만 남긴다.

## 11. 본편 코드와 레거시 코드 경계 정리

### 현재 상태

`Assets/Scripts/_GameJam`과 `Assets/Scripts/_deprecated`가 본편 코드와 같은 Assembly에 포함된다.

문제:

- 동일/유사 이름 클래스가 많아 탐색성이 떨어진다.
- 컴파일 대상에 계속 포함된다.
- 실제 사용 여부를 알기 어렵다.

### 개선안

1. `_GameJam`, `_deprecated`를 별도 asmdef 또는 Editor 제외 폴더로 이동
2. 실제 참조 여부를 GUID 검색으로 확인
3. 사용하지 않는 코드는 `Assets/Archive` 또는 별도 브랜치로 이동
4. 본편 코드만 `Assets/Scripts/Game` 같은 루트로 정리

단기 체크:

```text
각 레거시 script .meta guid 검색
  -> scene/prefab 참조 없음 확인
  -> 삭제 또는 Archive 이동
```

## 12. 네임스페이스와 Assembly Definition 도입

### 현재 상태

커스텀 본편 코드는 네임스페이스 없이 `Assembly-CSharp`에 모여 있다.

문제:

- 클래스명 충돌 위험
- 컴파일 경계 없음
- 의존 방향을 강제하기 어렵다.

### 개선안

단계적으로 네임스페이스와 asmdef를 도입한다.

예:

```text
PrisonCollege.Core
PrisonCollege.Runtime.Systems
PrisonCollege.Runtime.Stage
PrisonCollege.Runtime.AI
PrisonCollege.Runtime.Combat
PrisonCollege.Runtime.Items
PrisonCollege.Runtime.UI
PrisonCollege.Editor
```

asmdef 도입 순서:

1. Editor 전용 코드 분리
2. 순수 데이터/유틸 분리
3. Runtime UI/Combat/AI 분리
4. 순환 참조를 제거하며 경계 강화

주의:

- asmdef는 한 번에 크게 나누면 깨질 가능성이 높다.
- 먼저 네임스페이스만 도입하고, 이후 asmdef로 경계를 강화하는 편이 안전하다.

## 13. 이벤트 구독/해제 규칙 통일

### 현재 상태

여러 클래스가 UnityEvent와 C# 이벤트를 직접 구독한다. 일부는 해제 코드가 부족하거나 람다 해제 문제가 생길 수 있다.

위험:

- 씬 전환 후 죽은 객체 참조
- 중복 이벤트 호출
- 메모리 누수
- BT 노드 내부 이벤트 해제 실패

### 개선안

규칙:

- `OnEnable`에서 구독, `OnDisable`에서 해제
- `Awake`에서 구독했다면 `OnDestroy`에서 해제
- 람다 구독은 delegate 필드로 저장
- `UnityEvent.RemoveListener`가 가능한 형태로 작성

예:

```csharp
private UnityAction<HitInfo> _onTargetDepleted;

private void Subscribe(DamageReceiver receiver)
{
    _onTargetDepleted = OnTargetDepleted;
    receiver.DepletedEvent.AddListener(_onTargetDepleted);
}

private void Unsubscribe(DamageReceiver receiver)
{
    if (_onTargetDepleted != null)
        receiver.DepletedEvent.RemoveListener(_onTargetDepleted);
}
```

## 14. 플레이어 입력 구조 개선

### 현재 상태

입력이 여러 클래스에 흩어져 있다.

예:

- `Professor`: 공격, 무기 교체
- `PlayerInteraction`: 상호작용
- `FirstPersonController`: 이동, 점프, 달리기, 줌
- `EscapeInputSystem`: Escape 메뉴
- `SlotPackage`: Prepare 씬 Escape 처리

문제:

- 키 변경이 어렵다.
- UI 문구와 실제 입력이 어긋날 수 있다.
- 입력 잠금/모드 전환 규칙이 분산되어 있다.

### 개선안

Unity Input System 또는 자체 `InputRouter`를 둔다.

제안 구조:

```text
PlayerInputReader
  - raw input 수집

InputContext
  - Gameplay
  - UI
  - Task
  - Dead
  - Paused

Professor
  - Gameplay input만 소비

PlayerInteraction
  - Gameplay input만 소비

EscapeInputSystem
  - UI/Menu input만 소비
```

## 15. 상태와 이벤트 이름 정리

### 현재 상태

오타와 혼재된 이름이 있다.

예:

- `PersistenSingleton.cs`
- `ComsumableItem`
- `StageController.OnProjectSuccessed`
- `_progectReward`
- `Recrease` 계열 오타
- `Modifer` 오타

문제:

- 검색성이 떨어진다.
- 새 개발자가 의미를 오해할 수 있다.
- API가 굳기 전에 정리하지 않으면 계속 누적된다.

### 개선안

공용 API부터 순차적으로 rename한다.

우선순위:

1. 파일명/클래스명 오타
2. public/protected 멤버 오타
3. serialized private field 오타

Unity serialized field rename 시 주의:

- `[FormerlySerializedAs]`를 사용해 기존 씬/프리팹 값 손실 방지

## 16. 스테이지 테스트 가능성 확보

### 현재 상태

게임 규칙이 MonoBehaviour와 씬 참조에 강하게 묶여 있다.

문제:

- 혼잡도 계산, 프로젝트 진행, 웨이브 진행을 자동 테스트하기 어렵다.
- 씬을 열어야만 검증 가능하다.

### 개선안

순수 C# 규칙 클래스를 만든다.

예:

```text
ChaosRule
  - CalculateDelta(...)

ProjectProgressRule
  - CalculateProgress(...)

StageEndRule
  - Evaluate(...)

WaveRule
  - GetNextWave(...)
```

MonoBehaviour는 입력값을 모아 규칙 클래스에 전달하고 결과를 반영한다.

## 17. 권장 리팩터링 순서

### 1단계: 위험 버그 후보와 문서 불일치 수정

- `EffectReceiver.IncreaseStat()` 동작 확인 및 수정
- README 상호작용 키와 실제 입력 통일
- `IAttackable` 미구현 멤버 처리
- 씬에 필수 싱글턴이 없는 경우 자동 생성 대신 에러로 드러나게 조정

### 2단계: StageController UI 분리

- `StageHudPresenter` 생성
- `StageRuntimeSnapshot` 생성
- `StageController.UpdateUIs()` 이동

### 3단계: PostStudent AI 생성 분리

- `StudentBehaviorTreeFactory` 생성
- `PostStudent.ConstructBehaviorTree()` 이동
- `BehaviorType -> BT_Node` 매핑을 별도 메서드/클래스로 분리

### 4단계: 학생 생명주기 분리

- `StudentVitals`
- `StudentRagdollController`
- `StudentAnimationState`

### 5단계: 데이터 설정 ScriptableObject화

- `StageConfig`
- `ChaosSettings`
- `ProjectSettings`
- `StudentSpawnSettings`

### 6단계: 네임스페이스/asmdef 도입

- Editor 코드 분리
- Runtime 코드 네임스페이스 부여
- 순환 참조 제거 후 asmdef 도입

## 18. 단기 체크리스트

- [ ] `README.md`의 상호작용 키와 `PlayerInteraction` 키 통일
- [ ] `EffectReceiver.IncreaseStat()` 구현 검토
- [ ] `Professor.IAttackable` 미구현 멤버 처리
- [ ] `SceneSingleton.Instance` 자동 생성 정책 검토
- [ ] `PostStudent.ConstructBehaviorTree()`를 별도 파일로 이동
- [ ] `StageController.UpdateUIs()`를 presenter로 이동
- [ ] 레거시 `_GameJam`, `_deprecated` 코드 참조 여부 확인
- [ ] ScriptableObject 런타임 값 변경 지점 검색
- [ ] 이벤트 구독/해제 람다 패턴 검색
- [ ] 주요 매니저에 초기화 순서 검증 로그 추가

## 19. 장기 목표 구조

장기적으로는 다음 의존 방향을 목표로 삼는 것이 좋다.

```text
Data(ScriptableObjects)
  -> Rules(Pure C#)
    -> Runtime Systems(MonoBehaviour)
      -> Scene Controllers
        -> UI Presenters
```

AI:

```text
Behavior Data
  -> BehaviorTreeFactory
    -> StudentBrain
      -> StudentMotor / StudentAnimation / StudentVitals
```

전투:

```text
WeaponItem / WeaponData
  -> RuntimeWeaponStats
    -> WeaponController
      -> WeaponBase
        -> RuntimeEffect
          -> EffectReceiver
```

스테이지:

```text
StageConfig
  -> StageRule classes
    -> StageController
      -> StageHudPresenter
```

이 방향으로 가면 씬 참조와 게임 규칙이 분리되어 테스트, 밸런싱, 기능 추가가 쉬워진다.

