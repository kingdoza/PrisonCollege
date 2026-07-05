# PrisonCollege 코드 설계 구조

작성일: 2026-06-14

## 프로젝트 개요

이 프로젝트는 Unity 6000.2.6f2 기반의 1인칭 스테이지 진행형 게임이다. 플레이어는 교수 역할로 스테이지 안의 대학원생을 통제하고, 제한 시간 동안 프로젝트 진행도를 채우는 구조다.

핵심 코드는 대부분 `Assets/Scripts` 아래에 있으며, 별도 커스텀 asmdef 없이 `Assembly-CSharp`에 함께 컴파일된다. `Assets/Scripts/_GameJam`과 `Assets/Scripts/_deprecated`는 현재 본편 프로젝트에서 사용하지 않는 레거시 또는 폐기 코드로 취급하며, 현재 본편 흐름은 `Systems`, `Controllers`, `Weapons`, `BehaviourTree`, `SO`, `UIs` 중심이다.

## 레거시/미사용 코드 범위

`Assets/Scripts/_GameJam` 아래 코드는 현재 프로젝트의 본편 런타임 흐름에서 사용하지 않는다. 해당 폴더에는 게임잼 프로토타입 시절의 `GameSystem`, `Player`, `Student`, `ChaosSystem`, `HUDController`, `Spot` 계열 코드가 남아 있으나, 현재 구조 분석과 기능 수정 대상에서는 제외한다.

다만 별도 asmdef 또는 컴파일 제외 설정이 없기 때문에 Unity의 `Assembly-CSharp`에는 함께 컴파일될 수 있다. 따라서 미사용 코드라도 문법 오류나 타입 충돌이 생기면 프로젝트 컴파일에 영향을 줄 수 있다.

## 상위 실행 흐름

빌드 씬 순서는 `ProjectSettings/EditorBuildSettings.asset`에 정의되어 있다.

주요 플레이 루프는 다음과 같다.

1. `Assets/Scenes/MainScene.unity`
   - 메인 화면, 스테이지 선택, 전역 싱글턴 초기화가 포함된다.
   - `GameManager`, `WaveSystem`, `InventorySystem`, `StudentDB`, `SoundManager` 등이 배치된다.

2. `Assets/Scenes/Prepare.unity`
   - 스테이지 시작 전 상점/장착/웨이브 정보 화면이다.
   - `SlotPackage`가 웨이브를 증가시키고 아이템 UI를 구성한다.

3. `Assets/Scenes/FinalStage/SStage1.unity`
   - 실제 스테이지 씬이다.
   - `StageController`가 시간, 혼잡도, 탈출 수, 프로젝트 진행도, 학생 스폰, 승패 처리를 관리한다.

4. `Assets/Scenes/Store.unity`
   - 웨이브 종료 후 상점 루프에 사용된다.

5. `Assets/Scenes/Arena.unity`
   - 일부 웨이브 종료 후 별도 아레나 콘텐츠로 연결된다.

## 싱글턴 구조

프로젝트는 두 종류의 싱글턴 베이스를 사용한다.

### PersistentSingleton

파일: `Assets/Scripts/PersistenSingleton.cs`

씬 전환 후에도 유지되는 전역 상태에 사용된다. 내부에서 `DontDestroyOnLoad(gameObject)`를 호출한다.

주요 사용처:

- `GameManager`
- `InventorySystem`
- `WaveSystem`
- `StudentDB`
- `SoundManager`

주의점:

- `Instance` 접근 시 씬에 없으면 새 GameObject를 생성한다.
- Unity 씬 의존 직렬화 필드가 필요한 싱글턴은 씬에 배치된 인스턴스가 먼저 살아 있어야 한다.
- 잘못된 순서로 `Instance`를 호출하면 필드가 비어 있는 런타임 생성 인스턴스가 생길 수 있다.

### SceneSingleton

파일: `Assets/Scripts/SceneSingleton.cs`

씬 단위로 존재해야 하는 매니저에 사용된다. 씬 전환 시 파괴된다.

주요 사용처:

- `StageController`
- `AttributeSystem`
- `EscapeInputSystem`
- `FireSuppressionSystem`
- `LabLightSystem`
- `CameraShaker`
- `KillFeedbackController`

주의점:

- `Instance` 접근 시 씬에 없으면 새 GameObject를 생성한다.
- 씬에 필요한 참조가 많은 타입은 자동 생성되면 정상 동작하지 않을 가능성이 높다.

## 전역 진행 관리

### GameManager

파일: `Assets/Scripts/GameManager.cs`

역할:

- 스테이지 목록과 잠금/클리어 진행도 관리
- 난이도 선택
- 씬 전환
- BGM 플레이리스트 전환
- 전역 조작 상태 초기화

주요 메서드:

- `PrepareStage(int stageNum, DifficultyLevel difficultyLevel)`
- `StartStage()`
- `StageCleared()`
- `GoStore()`
- `ShowMainScreen()`
- `ShowStageSelect()`
- `GoArena()`

진행도 저장:

- `PlayerPrefs`의 `MaxClearStage`
- `StageDifficulty_{stageNum}`

씬 이름 규칙:

- 메인: `MainScene`
- 준비: `Prepare`
- 스테이지 prefix: `SStage`
- 상점: `Store`
- 아레나: `Arena`

### WaveSystem

파일: `Assets/Scripts/Systems/WaveSystem.cs`

역할:

- 현재 웨이브 번호 관리
- 웨이브별 `BehaviorWeightSet` 제공
- 낮/밤 상태와 skybox 적용
- 혼잡도/프로젝트 진행 배율 제공
- 웨이브 종료 후 아레나 연결 여부 제공

웨이브 진입은 `SlotPackage.Start()`에서 `WaveSystem.Instance.NewWaveEntered()`로 수행된다. 즉, 준비/상점 화면에 들어갈 때 다음 웨이브 정보가 확정된다.

### InventorySystem

파일: `Assets/Scripts/Systems/InventorySystem.cs`

역할:

- 전체 아이템 목록 관리
- 구매/미구매 아이템 집합 관리
- 장착 무기 목록 관리
- 돈 관리
- 상점/장착 UI 슬롯 채우기
- 패시브 아이템 활성화

스테이지 시작 시 `StageController.Awake()`에서 `InventorySystem.Instance.ActivatePassiveItems()`가 호출된다.

### StudentDB

파일: `Assets/Scripts/Systems/StudentDB.cs`

역할:

- 학생 프리팹과 프로필 데이터 보관
- 스테이지 입장 시 랜덤 학생 목록 제공

`RandomStudentSpawner`가 `StudentDB.Instance.GetRandomStudentEntries(...)`를 호출해 스테이지별 학생을 스폰한다.

## 스테이지 런타임 구조

### StageController

파일: `Assets/Scripts/Controllers/StageController.cs`

스테이지 씬의 중심 컨트롤러다.

관리하는 주요 상태:

- 남은 시간
- 준비 시간
- 혼잡도
- 탈출 수
- 프로젝트 진행도
- 현재 돈
- 작업 중 학생 수
- 교수 작업 여부
- 스폰된 학생 목록

주요 흐름:

1. `Awake`
   - 스탯 초기화
   - 스탯 이벤트 연결
   - 현재 돈 저장
   - 패시브 아이템 활성화
   - 장착 UI 참조 수집

2. `Start`
   - 학생끼리 충돌 무시
   - 준비 상태 시작
   - 반사 프로브 렌더
   - 웨이브 skybox 적용
   - 난이도별 초기 혼잡도 적용
   - 장착 슬롯 채우기
   - 학생 스폰 및 학생 이벤트 연결

3. `Update`
   - 준비 중이면 준비 타이머 감소
   - 시작 후에는 작업 학생 계산, 교수 작업 체크, 프로젝트 진행, 타이머 감소
   - 혼잡도 증가/감소 계산
   - UI 갱신

스테이지 종료:

- 제한 시간 종료: 성공 처리
- 탈출 수 최대 도달: 실패 처리
- 마지막 웨이브 클리어: `GameManager.StageCleared()`
- 중간 웨이브 종료: 돈 저장 후 웨이브 종료 패널 표시

### Professor

파일: `Assets/Scripts/Professor.cs`

플레이어 본체 역할이다.

구성:

- 이동: 패키지 `FirstPersonController`
- 카메라: `PlayerCamera`
- 무기: `WeaponController`
- 상호작용: `PlayerInteraction`
- 체력/스태미나: `Health`, `Stamina`, `DamageReceiver`, `StatRecovery`

주요 역할:

- 체력 감소/사망/부활 처리
- 스태미나 회복 및 달리기 소모
- 무기 공격 입력 처리
- 숫자키/마우스휠 무기 교체
- 교수 작업 자세 전환

주의:

- `IAttackable` 구현 일부는 `NotImplementedException` 상태다.
- 실제 학생 AI의 방어/회피 코드가 `IAttackable`을 참조하므로 기능 확장 시 구현이 필요하다.

### PlayerInteraction

파일: `Assets/Scripts/PlayerInteraction.cs`

카메라 정면 레이캐스트로 `IPlayerInteractable` 대상을 찾고, 입력 시 상호작용을 시작한다.

현재 입력:

- `F` 키로 상호작용 시작

인터페이스:

- `InteractionPrompt`
- `CanInteract`
- `UIFillRatio`
- `OnInteractStart()`
- `OnInteractCancel()`

대표 구현:

- `Click`
- `ClickAndWait`

## 학생 AI 구조

### PostStudent

파일: `Assets/Scripts/PostStudent.cs`

학생 런타임의 중심 클래스다. `PostStudent`는 다음 책임을 한 번에 가진다.

- NavMeshAgent, Animator, Collider 참조 수집
- DamageReceiver/BoostReceiver 이벤트 연결
- Behaviour Tree 생성
- Blackboard 생성
- 학생 상태 판정
- 피격/사망/래그돌/기상 처리
- 행동별 애니메이션 부착물 관리
- 탈출 이벤트 발행
- 교수 공격 타겟 해제

스폰 후 흐름:

1. `RandomStudentSpawner`가 학생 프리팹을 생성한다.
2. `BehaviorWeightSet`, 이름, 좌석 spot을 주입한다.
3. 학생은 처음에 잠든 상태다.
4. `StageController.StageStartEvent`가 발생하면 `Wakeup()`이 호출된다.
5. 이후 Behaviour Tree가 `Update()`마다 `Evaluate()`된다.

### Blackboard

파일: `Assets/Scripts/BehaviourTree/Blackboard.cs`

AI 노드 사이의 공유 상태다.

주요 필드:

- `Agent`
- `Anim`
- `Avatar`
- `BehaviorWeightSet`
- `StageSpots`
- `Player`
- `destSpot`
- `destBehavior`
- `targetObject`
- `targetDamageable`
- `isDamaged`
- `isStunned`
- `isEscaping`
- `hasToWork`
- `hasToFrenzy`
- `coopData2`

### Behaviour Tree 노드

폴더: `Assets/Scripts/BehaviourTree`

기본 구조:

- `BT_Node`
- `Sequence`
- `Selector`
- `RandomSelector`
- `ReactiveSelector`
- `ParallelNode`
- `ParallelOR`
- `ConditionDecorator`
- `ActionNode`

행동 패턴:

- `WorkPattern`
- `TryEscapePattern`
- `RushThroughPattern`
- `CombatApproachPattern`
- `MeleeAttackPattern`
- `TacklePattern`
- `TakeHitReactivePattern`
- `BoostReactivePattern`
- `CoopReactivePattern`
- `SwimOverridePattern`

학생 AI의 최종 루트는 `PostStudent.ConstructBehaviorTree()`에서 조립된다. 기본 행동 선택 후 reactive 패턴이 여러 겹으로 감싸는 형태다.

개념적 구조:

```text
TakeHitReactivePattern
  -> AttackReactivePattern
    -> SwimOverridePattern
      -> BoostReactivePattern
        -> CoopReactivePattern
          -> 일반 행동 루프
```

### BehaviorWeightSet

파일: `Assets/Scripts/SO/BehaviorWeightSet.cs`

웨이브별 학생 행동 확률표다. `BehaviorType`별 가중치를 들고 있으며, 학생 시작 시 deep copy를 만들어 학생별 확률 보정에 사용한다.

`BehaviorType`은 안전/위험 행동 정보도 함께 가진다.

안전 행동 예:

- Work
- LookAround
- Game
- Talk
- Dance
- Worship
- Sleep
- SitChair
- Sing

위험 행동 예:

- Escape
- RushThrough
- Fight
- Smoke
- Tackle
- Hack

### StageSpots

파일: `Assets/Scripts/StageSpots.cs`

스테이지 안의 행동 위치를 `BehaviorType`별로 매핑한다.

특징:

- 자식 오브젝트에서 `BehaveSpot`을 수집한다.
- `Normals`, `Coops` 그룹은 일부 spot만 랜덤 선택한다.
- 학생의 컴퓨터 행동은 자기 좌석 `SeatSpot`으로 고정된다.

## 전투와 효과 구조

### WeaponController

파일: `Assets/Scripts/Weapons/WeaponController.cs`

역할:

- 무기 프리셋 배열 보관
- 인벤토리 장착 목록 기반 실제 무기 배열 구성
- 현재 무기 교체
- 공격 요청 전달
- 무기 UI 갱신
- 원거리 무기 탄약 이벤트를 `StageController`로 전달

### WeaponBase

파일: `Assets/Scripts/Weapons/WeaponBase.cs`

모든 무기의 베이스 클래스다.

주요 필드:

- 무기 이름
- `WeaponData`
- owner
- `WeaponAnimator`
- 스태미나 소모량
- `EffectData`

공격 흐름:

```text
Professor.HandleWeaponAttack()
  -> WeaponController.TryAttack()
    -> WeaponBase.PlayAttackAnim()
      -> WeaponAnimator.StartAttack(callback)
        -> WeaponBase.ExecuteAttack()
```

### 근접 무기

파일: `Assets/Scripts/Weapons/MeleeWeapon.cs`

카메라 정면으로 `SphereCastAll`을 수행한다. 장애물 레이어에 막히면 중단하고, 대상에게 `DamageReceiver.TakeEffect(...)`를 호출한다.

점프 중 공격이면 `AttributeSystem.JumpDamageMod`를 반영한다.

### 원거리 무기

파일: `Assets/Scripts/Weapons/RangedWeapon.cs`

탄창을 `Stat` 컴포넌트로 관리한다.

주요 개념:

- `CanAttack`: 탄창이 비어 있지 않아야 함
- `SpreadIntensity`: 스테이지에서는 `AttributeSystem.ShotSpreadMod` 반영
- `Shot(Vector3 shotDestination)`는 파생 클래스가 구현

파생 예:

- `GunWeapon`
- `GunWeapon2`
- `ThrowWeapon`
- `ThrowWeapon2`

### Projectile

파일: `Assets/Scripts/Weapons/Projectile.cs`

물리 충돌 기반 발사체다.

흐름:

1. 충돌 대상 레이어가 학생인지 확인
2. owner 제외
3. 이미 맞은 대상 제외
4. 최소 속도 임계값 확인
5. `WeaponData.effect.GetActorReceiver(...)`로 receiver 획득
6. `receiver.TakeEffect(...)`

### EffectData와 Receiver

관련 파일:

- `Assets/Scripts/SO/EffectData.cs`
- `Assets/Scripts/SO/DamageData.cs`
- `Assets/Scripts/SO/BoostData.cs`
- `Assets/Scripts/Receivers/EffectReceiver.cs`
- `Assets/Scripts/Receivers/DamageReceiver.cs`
- `Assets/Scripts/Receivers/BoostReceiver.cs`

설계:

- `EffectData`는 효과 수치와 이펙트 프리팹을 가진다.
- `DamageData`는 `DamageReceiver`를 찾는다.
- `BoostData`는 `BoostReceiver`를 찾는다.
- `EffectReceiver`는 공통 이벤트와 적용 진입점을 가진다.

데미지 효과:

```text
Weapon/Projectile
  -> EffectData.GetActorReceiver(actor)
  -> DamageReceiver.TakeEffect(data, hitInfo)
  -> Health 감소
  -> DepletedEvent 발생
```

부스트 효과:

```text
BoostReceiver.TakeEffect()
  -> 작업 강제 이벤트 또는 광분 이벤트 확률 발동
  -> PostStudent.OnWorkTriggered / OnFrenzyTriggered
```

## 스탯과 속성 구조

### Stat

파일: `Assets/Scripts/Stats/Stat.cs`

공통 수치 컴포넌트다.

파생 클래스:

- `Health`
- `Stamina`
- `Progress`
- `Duration`

제공 이벤트:

- `IncreaseEvent`
- `DecreaseEvent`
- `DepletedEvent`
- `MaxReachEvent`
- `ResetEvent`

### AttributeSystem

파일: `Assets/Scripts/Systems/AttributeSystem.cs`

스테이지 내 각종 보정치를 보관하는 SceneSingleton이다.

예:

- 학생 이동 속도
- 교수 이동 속도
- 작업 효율
- 바리케이드 설치 속도
- 해킹 수리 속도
- 무기 보급 속도
- 혼잡도 감소량
- 근접 공격 속도
- 근접 데미지
- 투척 데미지
- 사격 탄퍼짐

### AttributeModifier

파일: `Assets/Scripts/AttributeModifier.cs`

flat과 percent 보정을 합산해 최종 값을 계산한다.

```text
final = (original + flat) * percent
```

## 아이템 구조

ScriptableObject 기반이다.

기본 타입:

- `Item`
- `WeaponItem`
- `PassiveItem`
- `Ability`

무기 아이템:

- `WeaponItem.inStageIndex`로 `WeaponController`의 무기 프리셋 인덱스를 참조한다.

패시브 아이템:

- `Activate()`에서 `AttributeSystem`의 modifier를 조정한다.

예:

- `Shoe`: 교수 이동 속도 증가
- `MeleeStrong`: 근접 공격 속도/데미지 증가
- `BarricadeFaster`: 바리케이드 설치/해킹 수리 속도 증가
- `ThrowStrong`: 투척 관련 보정
- `Scope`: 사격 탄퍼짐 보정

## UI 구조

### 메인/스테이지 선택

관련 파일:

- `Assets/Scripts/UIs/MainScreen.cs`
- `Assets/Scripts/UIs/StageLayout.cs`
- `Assets/Scripts/UIs/StageSlot.cs`
- `Assets/Scripts/UIs/SimplePanel.cs`
- `Assets/Scripts/UIs/MenuPanel.cs`

`StageLayout`은 `GameManager.StageEntries`를 읽어 스테이지 슬롯을 동적으로 생성한다.

### 준비/상점/장착

관련 파일:

- `Assets/Scripts/UIs/SlotPackage.cs`
- `Assets/Scripts/ItemSlots/ItemSlot.cs`
- `Assets/Scripts/ItemSlots/IconSlot.cs`
- `Assets/Scripts/ItemSlots/ShopSlot.cs`
- `Assets/Scripts/ItemSlots/DragItem.cs`
- `Assets/Scripts/ItemSlots/DragSlot.cs`
- `Assets/Scripts/UIs/ItemInfoPanel.cs`

흐름:

1. `SlotPackage.Start()`
2. `WaveSystem.NewWaveEntered()`
3. 상점 슬롯 생성
4. 패시브 슬롯 생성
5. 무기 슬롯 채우기
6. 장착 슬롯 채우기
7. 드래그 드롭으로 장착 상태 갱신

### 스테이지 HUD

관련 파일:

- `StageController`
- `StageOver`
- `ChaosUI`
- `WeaponPanel`
- `EquipInfo`
- `InteractionUI`
- `Crosshair`
- `StaminaFadePanel`

스테이지 HUD의 상당 부분은 `StageController`가 직접 TextMeshPro와 Image를 갱신한다.

## 사운드 구조

관련 파일:

- `Assets/Scripts/Audios/SoundManager.cs`
- `Assets/Scripts/Audios/SoundEmitter.cs`
- `Assets/Scripts/Audios/SoundData.cs`
- `Assets/Scripts/SO/BGMPlayListData.cs`

`SoundManager`는 `PersistentSingleton`이며 emitter 풀을 가진다. `SoundUtils` 정적 헬퍼를 통해 2D/3D SFX, BGM을 재생한다.

`GameManager`는 씬 로드 이벤트를 받아 BGM 플레이리스트를 교체한다.

## 환경/상호작용 오브젝트

대표 타입:

- `ExitGate`, `DoorGate`, `WindowGate`
- `FuseBox`
- `FireSuppressionSystem`
- `Fire`
- `Microwave`
- `Monitor`
- `Barricade`
- `ProfessorTask`

상호작용은 대부분 `IPlayerInteractable` 또는 `ClickAndWait` 이벤트 연결로 처리된다.

## 현재 구조 요약

프로젝트는 기능별 폴더가 나뉘어 있지만, 의존 방향은 꽤 중앙집중적이다.

중심축:

```text
GameManager
  -> 씬 전환, 진행도, BGM

StageController
  -> 스테이지 상태, UI, 학생 이벤트, 승패 처리

InventorySystem
  -> 아이템, 돈, 장착 상태

WaveSystem
  -> 웨이브, 행동 확률, 난이도 배율

PostStudent
  -> 학생 AI, 피격, 사망, 행동 실행

WeaponController
  -> 플레이어 무기 런타임
```

데이터 흐름:

```text
ScriptableObjects
  -> GameManager / WaveSystem / InventorySystem / StudentDB
    -> StageController / SlotPackage
      -> PostStudent / WeaponController / UI
```

런타임 이벤트 흐름:

```text
학생 사망/탈출 이벤트
  -> StageController
    -> 혼잡도, 탈출 수, 돈, UI, 게임오버 판단

플레이어 무기 공격
  -> WeaponController
    -> WeaponBase
      -> EffectReceiver
        -> 학생/문/플레이어 상태 변화

패시브 아이템
  -> InventorySystem.ActivatePassiveItems()
    -> AttributeSystem modifier 변경
      -> 이동/공격/AI/작업/상호작용 속도에 반영
```
