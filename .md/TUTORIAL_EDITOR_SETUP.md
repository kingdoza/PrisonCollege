# TutorialStage Unity Editor 연결 체크리스트

이 문서는 코드가 생성하거나 수정하지 않은 `TutorialStage` 씬과 전용 asset을 Unity Editor에서 연결하기 위한 인계 문서다. 오브젝트 이름이나 계층 탐색으로 참조를 보완하지 않으므로 아래 필드는 모두 Inspector에서 직접 연결해야 한다.

## 1. 씬과 진입 버튼

- `TutorialStage` 씬을 생성하고 Build Settings에 등록한다.
- 메인 메뉴의 신임 교수 연수 버튼은 `GameManager.StartTutorial()`에 연결한다.
- 튜토리얼 내부 메인 메뉴 버튼은 `GameManager.ExitTutorialToMain()`을 사용한다. 기존 `ShowMainScreen()`에 연결하지 않는다.
- 정규 스테이지용 씬, 프리팹, Animator Controller, ScriptableObject는 변경하지 않는다.
- 씬에 정규 시스템이 요구하는 persistent singleton(`GameManager`, `AttributeSystem` 등)이 정상적인 기존 부트 경로로 존재하는지 확인한다.

## 2. 전용 asset

### StageRuntimeConfig

튜토리얼 전용 `StageRuntimeConfig` asset을 새로 만들고 `TutorialStage`의 `StageController.Runtime Config`에만 연결한다.

- Mode: `Tutorial`
- Use Preparation: 끔
- Auto Spawn Students: 끔
- Use Inventory Loadout: 끔
- Use Wave Presentation: 끔
- Finish Policy: `ReportOnly`
- Tutorial Stage Title: 튜토리얼 메뉴에 표시할 이름. 예: `신임교수 연수`. 정규 현재 스테이지 제목은 사용하지 않는다.
- Tutorial Behavior Weight Set: P-29에서 사용자가 정한 미니웨이브 전용 asset
- Training Loadout: 0~7단계 전용 고정 장비. 장비 슬롯에는 기존 `WeaponItem` asset을 연결하고, 최소 한 슬롯은 `Is Empty Slot`을 켠다. 실제 피해를 줄 수 있는 장비도 포함한다.
- Work Training Boost: 6단계에서 지급할 기존 부스터의 `WeaponItem` asset을 연결하고 `Fill To Maximum`을 켠다. 연결된 부스터의 기존 작업/광분 확률과 패시브 보정 계산을 그대로 사용한다.
- Mini Wave Loadout: P-28의 `WeaponItem`, 슬롯 순서와 탄약 방식. 최대 탄약이면 `Fill To Maximum`을 켜고, 아니면 `Ammunition`에 시작 수량을 입력한다. 의도적인 빈 슬롯은 `Is Empty Slot`을 사용한다.
- Tutorial Chaos Factor / Project Factor: 설계에 맞춘 전용 계수. 정규 asset 값을 수정하지 않는다.

### TutorialCourseDefinition

- 실제 런타임 단계인 `Intro`부터 `MiniWave`까지와 `MiniWavePreparation`, `PowerRecovery`를 포함한 11개 문구를 각각 정확히 한 번 등록한다.
- 제목, 부제, 안내, 목표, 입력, 완료 문구는 결정문서의 확정 문구를 그대로 입력한다.
- `CourseSummary` 문구는 등록하지 않는다.
- `MiniWavePreparation`에는 준비 안내, 목표와 Tab 입력 문구를 등록한다. 문구는 코드에서 임의 생성하지 않는다.
- `Risk Behavior Contents`에는 `ExitAttack`, `Hacking`, `BadSinging`, `Smoking`을 각각 정확히 한 번 등록하고 각 Title과 Description을 작성한다. 문구가 비거나 ID가 중복되면 시작 검증이 실패한다.
- P-23 `Work Confirmation Seconds`는 사용자가 실제 `n` 값을 입력한다. 0이면 시작 검증이 실패한다.
- 8단계 기본값은 제한 시간 30초, 탈출 실패 기준 3명, 학생 수 4명이다.

### 학생 행동 데이터

- `TutorialActorDirector.Training Behavior Weight Set`에는 0단계 전 DB 전체 학생을 동기 초기화할 별도 전용 asset을 연결한다.
- P-29 미니웨이브 `BehaviorWeightSet`과 행동별 가중치는 `StageRuntimeConfig.Tutorial Behavior Weight Set`에 연결한다.
- 정규 `BehaviorWeightSet`, 학생 프리팹의 기존 값, 정규 부스터 asset은 수정하지 않는다.

## 3. TutorialRuntime 루트

전용 루트 오브젝트에 다음 컴포넌트를 배치한다. 한 오브젝트에 함께 두거나 자식으로 나눠도 되지만 모든 참조는 직접 연결한다.

- `TutorialSceneBootstrap`
- `TutorialDirector`
- `TutorialStageFacade`
- `TutorialActorDirector`
- `TutorialCheckpointService`
- `TutorialTransientRegistry`
- `TutorialHUDPresenter`
- `TutorialHighlighter`
- `TutorialInput`
- `TutorialObjectiveMarkerPresenter`
- `TutorialStudentFocusSource`
- `TutorialRiskInfoBubblePresenter`

`TutorialSceneBootstrap`에는 facade, actor director, transient registry, director를 모두 연결한다. 이 Bootstrap이 facade 초기화, DB 전체 학생 생성 및 동기 행동 runtime 초기화, director 시작 순서를 보장한다.

`TutorialDirector`에는 다음을 연결한다.

- `TutorialCourseDefinition`
- facade, actor director, checkpoint service, HUD, highlighter, input, movement marker, objective marker presenter, student focus source, risk info bubble presenter
- 단계 컴포넌트 12개: `TutorialIntroStep`, `TutorialMovementStep`, `TutorialBarricadeStep`, `TutorialRiskResponseStep`, `TutorialPowerRecoveryStep`, `TutorialMicrowaveManagementStep`, `TutorialInnocentStudentStep`, `TutorialChaosDecayStep`, `TutorialStudentWorkStep`, `TutorialProfessorWorkStep`, `TutorialMiniWavePreparationStep`, `TutorialMiniWaveStep`
- 단계 배열에는 각 안정 ID를 정확히 한 번만 넣는다. 표시 번호나 배열 위치로 단계 ID를 대신하지 않는다.
- 기존 `TutorialCourseSummaryStep` 참조가 남아 있다면 배열 요소 자체를 제거하고 빈 슬롯을 남기지 않는다.

`TutorialCheckpointService`에는 facade, actor director, transient registry를 연결한다.

## 4. StageController와 Facade

`TutorialStage`의 `StageController`에는 기존 코드가 요구하는 Stat, UI, Professor, StageSpots, ProfessorTask, ChaosUI, reflection/light 참조를 연결한다. 튜토리얼 config가 정규 `InventorySystem`, `WaveSystem`, `RandomStudentSpawner` 실행을 차단하지만, 기존 공용 컴포넌트가 사용하는 필수 참조는 null로 방치하지 않는다.

`TutorialStageFacade`에는 다음을 연결한다.

- 해당 씬의 `StageController`, `Professor`, `WeaponController`
- 씬의 모든 문·창문 `ExitGate` 목록
- 씬의 모든 `ProfessorTask`
- 3-3에서 사용할 서로 다른 `Microwave` 두 개와 8단계에서 상태 복원이 필요한 모든 `Fire`, `FireSuppressionSystem`, `LabLightSystem`
- P-28에서 정한 각 `Recharger`, 사용 가능 여부, 세션 내부 비용
- P-28 설정을 모두 확인한 뒤에만 `P28 Recharger Configuration Confirmed`를 켠다. 보충 시설이 없기로 결정했더라도 빈 배열의 의미를 확인한 후 체크한다.

씬 시작 시 facade가 모든 출구를 미설치 상태로 한 번 구성한다. 이 setup API를 정규 씬에서 호출하지 않는다.

## 5. 학생 풀과 NavMesh

`TutorialActorDirector`에 다음을 연결한다.

- `StageController`, `Professor`, `StageSpots`, 학생 인스턴스의 부모 Transform
- `StudentDB.GetAllStudentEntries()` 순서와 1:1로 대응하는 고정 대기 슬롯 배열
- 연수 출발 NavMesh 지점과 대기 구역 복귀 NavMesh 지점
- Loyalty와 Standby 자세용 Animator bool 이름. 별도 자세가 없으면 빈 문자열로 두며 기존 `MoveSpeed = 0` Idle을 사용한다. Cheer는 Inspector 설정 없이 아레나와 동일한 `Cheer_S`/`Rally_S`/`Clap_S`/`Punch_S`/`Jab_S` Trigger를 2/2/2/1/1 가중치로 반복한다.
- 3단계 역할 8개, 4-1 행동 spot, 6단계 Work spot
- 3-3 음식 학생을 일렬 배치할 고정 슬롯 배열. StudentDB index 0 학생 프리팹의 `PlateAttacher` 정상/위험 음식 총수 이상이어야 한다.

대기 슬롯은 DB 항목 수 이상이어야 한다. 현재 정책 검증은 3단계 8명과 4-1 미참여 1명을 위해 DB가 최소 9명인지 확인하지만, 코드의 풀 크기는 DB 전체 개수를 사용한다.

모든 대기 슬롯, 출발 지점, 복귀 지점과 행동 spot 주변 1m 안에 유효한 NavMesh가 있어야 한다. NavMeshAgent 이동 가능 여부는 Play Mode에서 직접 확인한다.

### 3단계 고정 역할

정상 4개와 위험 4개를 합쳐 정확히 8개를 연결한다.

- 정상: `Worship`, `Game`, 정상 `Sing`, `Dance`
- 위험: `Escape`, `Hack`, 나쁜 `Sing`, `Smoke`

각 binding의 `BehaveSpot`이 지정 행동을 지원해야 한다. Sing 두 역할은 코드가 노래 품질을 고정한다. Escape와 Hack은 이 scripted 시나리오에서만 실제 탈출·바리케이드 피해·조명 OFF 결과가 억제된다.

### 4-1과 6단계

- 4-1 `Innocent Training Behavior`는 연결한 spot이 지원하는 비위험 행동이어야 한다.
- 6단계 `Student Work Training Spot`은 `Work`를 지원해야 한다.
- `StageSpots.Tutorial Spot Bindings`를 사용할 경우 안정 ID와 spot을 명시적으로 연결한다. 기존 랜덤 spot 목록과 `GetRandomSpotByType()` 구성은 그대로 둔다.

## 6. 이동 마커와 강조

- 1단계 도착 Trigger에 `TutorialMovementMarker`와 Trigger Collider를 배치한다.
- `TutorialMovementMarker`의 Professor와 Marker Anchor를 연결한다. Marker Anchor가 비어 있으면 Trigger 오브젝트의 Transform을 사용한다.
- `TutorialMovementMarker.Static Visual Root`에는 1단계 목표 지점에 남아 있을 별도 정적 메시 또는 데칼 루트를 연결한다. 이 루트에는 Collider를 넣지 않는다.
- 순수 시각용 `TutorialObjectiveMarker` prefab을 만들고 `TutorialMarkerVisual`을 부착한다. prefab에는 Collider와 `TutorialMovementMarker`를 넣지 않는다.
- `TutorialObjectiveMarkerPresenter.Marker Prefab`에 위 prefab을 연결한다. Runtime Root가 비어 있으면 Presenter 오브젝트 하위에 런타임 풀을 만든다.
- Presenter의 Location Profile, Student Profile과 World Target Profile에서 높이와 원본 prefab 대비 스케일 배율을 별도로 설정한다. 시작 권장값은 위치 `(height 1, scale 1)`, 학생 `(height 2.15, scale 0.55)`, 탈출구·시설 `(height 1, scale 0.75)`이며 실제 메시 크기에 맞춰 조정한다.
- Pool Prewarm Count는 `4`, 탈출구 수, 교수 작업대 수 중 가장 큰 동시 표시 수 이상을 권장한다. 부족해도 런타임에 확장된다.
- 기존 `TutorialMovementMarker.Visual Root`와 `Marker Visual`은 공용 Presenter가 연결되지 않은 경우를 위한 fallback이다. 정상 튜토리얼 구성에서는 Presenter의 공유 prefab이 표시된다.
- `TutorialHighlighter`는 facade에 등록된 각 `ExitGate`를 명시적 탐색 루트로 사용하고, 루트 자신과 비활성 오브젝트를 포함한 하위에서 `OutlineFader`를 수집한다. 별도 Gate Binding은 필요하지 않다.
- 문은 정적 파츠의 `OutlineFader`, 창문은 `WindowGate`가 런타임에 생성한 하위 파츠의 `OutlineFader`가 수집된다. 각 Gate 하위에서 하나도 찾지 못하면 2단계 진입 검증이 실패한다.
- `TutorialBarricadeStep.Gate Marker Bindings`에는 facade의 모든 ExitGate와 파괴되지 않는 고정 Marker Anchor를 정확히 한 번씩 연결한다.
- facade `Rechargers`에는 3단계 무기 충전소 `DamageRecharger`와 6단계 자판기 `BoostRecharger`를 활성 binding으로 등록한다. 각 recharger 루트 하위에 `OutlineFader`가 있어야 한다.
- `TutorialProfessorWorkStep.Task Marker Bindings`에는 facade의 모든 ProfessorTask와 고정 Marker Anchor를 정확히 한 번씩 연결한다. 각 ProfessorTask 루트 하위에도 `OutlineFader`가 있어야 한다.
- 점멸 색상은 각 오브젝트의 `Outline.Outline Color`, 속도는 해당 `OutlineFader`의 Fade Duration과 Hold Duration에서 설정한다. Tutorial Step에는 별도 색상 override 필드가 없다.

### 3-2 정전 복구

- `TutorialPowerRecoveryStep.Fuse Box`에는 메인 스테이지 방식으로 구성된 기존 `FuseBox`를 연결한다.
- `TutorialPowerRecoveryStep.Fuse Box Marker UI`에는 메인 스테이지에서 사용하는 기존 `FuseBoxMarkerUI`를 연결한다. Step은 이 컴포넌트의 표시 상태를 직접 제어하지 않는다.
- 전기박스 하위에 빈 오브젝트를 만들고 공용 화살표를 표시할 위치로 배치한 뒤 `TutorialPowerRecoveryStep.Marker Anchor`에 연결한다.
- `FuseBoxMarkerUI` 오브젝트와 컴포넌트는 활성 상태로 유지한다. 숨김은 GameObject 비활성화가 아니라 기존 CanvasGroup 흐름에 맡긴다.
- `FuseBoxMarkerUI.Target`에는 위 `FuseBox` Transform, `World Camera`에는 플레이어 카메라, `Canvas`와 `Bounds Rect`에는 실제 HUD Canvas와 전체 화면 RectTransform을 직접 연결한다. `Direction Arrow Object`도 연결한다.
- TutorialCanvas를 메인 Canvas에 병합했다면 `Canvas`와 `Bounds Rect`를 병합 후 계층 기준으로 다시 연결한다.
- `TutorialStageFacade.Lab Light System`에는 이 연구실의 기존 `LabLightSystem`을 연결한다. 새 Step 추가 이후 필수 참조다.
- `StageController`의 기존 `ChaosUI` 참조와 해당 `ChaosUI.Hack SoundData`, Warning Popup 참조를 메인 스테이지와 동일하게 연결한다. `TutorialStageFacade`에는 별도 Chaos UI 필드가 없으며 3-2에서 기존 해킹 정전 팝업과 정전음을 함께 사용한다.
- 튜토리얼 공용 `TutorialObjectiveMarkerPresenter`가 위 `Marker Anchor`를 월드 대상으로 추적한다. 기존 공용 prefab과 `World Target Profile`의 높이·스케일을 사용하며 별도 전기박스 전용 prefab 또는 OutlineFader를 추가하지 않는다.

### 3-3 전자레인지 관리

- `TutorialStageFacade.Microwaves`에는 사용할 전자레인지 두 개를 중복 없이 연결한다. 이름 검색이나 씬 전체 탐색으로 대체하지 않는다.
- Step 오브젝트에 `TutorialMicrowaveManagementStep`을 추가하고 `TutorialDirector.Steps` 배열에 연결한다.
- 위험 전자레인지를 고정하려면 `Use Fixed Hazard Microwave`를 켜고 `Fixed Hazard Microwave`에 facade에 등록한 두 대 중 하나를 연결한다. 끄면 기존처럼 진입마다 무작위 배정한다.
- 각 전자레인지 하위에 파괴되지 않는 빈 `MarkerAnchor`를 만들고 화살표 위치를 조정한 뒤 `Microwave Marker Bindings`에 전자레인지와 anchor를 한 쌍씩 총 2개 연결한다.
- 3-3 전용 Food Samples 필드는 없다. StudentDB index 0 학생 프리팹의 기존 `PlateAttacher.Fire Cause Foods`와 `Fire Non Cause Foods`를 그대로 사용하므로 두 배열에 null·중복이 없어야 하며 각각 하나 이상 필요하다.
- `TutorialActorDirector.Microwave Food Display Slots`에 위 두 음식 배열의 총수 이상의 고정 Transform을 일렬로 연결한다. 학생은 연수 출발 지점에서 각 슬롯까지 달려가므로 출발점부터 모든 슬롯까지 완전한 NavMesh 경로가 있는지 Play Mode에서 확인한다.
- `TutorialCourseDefinition`에 `MicrowaveManagement`의 3-3 제목·안내·목표·완료 문구를 추가한다. 문구는 코드가 자동 생성하지 않는다.
- 각 전자레인지의 기존 Hum SoundData와 Hazard Cooking Vfx 참조가 설정되어 있어야 한다. 연수 중 위험 VFX와 hum은 유지되지만 폭발 Particle, Fire, explosion SoundData와 Shake는 실행되지 않는다.
- 각 `Microwave.Food Rotation Speed`에서 작동 중 음식의 시계방향 회전 속도를 초당 각도로 조절한다. 기본값은 `90`이며 `0`이면 회전하지 않는다.

## 6-1. 플레이어 입력 잠금과 결과 일시정지

- 튜토리얼 전용 관리 오브젝트에 `TutorialPlayerInputGate`를 부착한다.
- 같은 플레이어 오브젝트의 `Professor`, `FirstPersonController`, `PlayerInteraction`을 명시적으로 연결한다.
- `TutorialDirector.Player Input Gate`에 위 컴포넌트를 연결한다.
- 0단계에서 이동·시점·공격·장비 교체·F 상호작용이 막히지만 Tab과 ESC는 작동하는지 확인한다.
- 미니웨이브 성공·실패 팝업의 Animator나 별도 tween을 추가한다면 Unscaled Time으로 동작하게 설정한다. 결과 버튼은 time scale 0에서도 EventSystem으로 눌릴 수 있어야 한다.

## 7. 장비와 보충 시설

- loadout entry에는 씬의 `WeaponBase` 오브젝트가 아니라 기존 `WeaponItem` asset을 연결한다. 실행 시 `WeaponItem.inStageIndex`를 통해 `Professor.WeaponController`의 `_weaponPresets` 런타임 인스턴스로 해석되므로 두 배열의 인덱스 대응이 유효해야 한다.
- 6단계 부스터가 들어갈 슬롯은 Training Loadout에서 `Is Empty Slot`을 켜 확보한다. 이 entry의 `Weapon Item`, `Fill To Maximum`, `Ammunition` 값은 무시된다. 지급 API는 첫 빈 슬롯을 사용해 최대 탄약으로 채우고 즉시 선택한다.
- Work Training Boost에는 사용할 기존 부스터 `WeaponItem`을 넣고 `Is Empty Slot`은 끄며 `Fill To Maximum`은 켠다. 별도의 최대 탄약 수치는 입력하지 않는다. 전용 `BoostData`는 필요하지 않으며, 작업 효과가 나오지 않으면 다시 투척해야 한다.
- P-28 Mini Wave Loadout의 장비, 슬롯 순서와 각 슬롯의 `Fill To Maximum` 또는 `Ammunition`을 Inspector에서 입력한다. 코드는 임의 장비나 숫자를 채우지 않는다.
- P-28 Recharger 사용 여부와 비용은 facade binding에서 결정한다. 비용은 튜토리얼 세션 자금만 사용하며 정규 인벤토리 자금에는 접근하지 않는다.

## 8. HUD

`TutorialHUDPresenter`에 다음을 연결한다.

- 단계 panel과 title/subtitle/guide/objective/input TMP. 별도 progress TMP는 현재 사용하지 않고 objective 한 줄에 진행도를 함께 표시한다.
- 혼잡도 감소 강조 오브젝트
- 8단계 제한 시간, 탈출, 혼잡도와 변화율, 작업 인원, 프로젝트, 출구 상태 TMP
- 8단계 실패 panel, `8단계 다시 시작` 버튼, `건너뛰기` 버튼
- 성공 결과 팝업과 `재연수`, 메인 메뉴 버튼. 미니웨이브 완료 후 이 팝업에서 사용자 입력을 기다린다.

HUD는 표시만 담당한다. 판정용 UnityEvent를 HUD 오브젝트에 추가하지 않는다. 버튼 콜백은 director가 runtime에 등록하고 파괴 시 자신이 등록한 listener만 해제한다.

### 3단계 위험 행동 말풍선

- `TutorialStudentFocusSource.Student Info`에는 플레이어의 기존 `StudentDetector`가 사용 중인 동일한 `StudentInfo` 컴포넌트를 연결한다.
- 튜토리얼 말풍선용 Detection Range, Detection Radius, Student Layers와 Blocking Layers는 설정하지 않는다. 말풍선 포커스는 기존 `StudentDetector` 판정과 완전히 동일하다.
- `TutorialCanvas` 아래에서 `StepInfoContentRoot`와 분리된 `RiskBehaviorBubble` UI를 만든다.
- 말풍선에는 `RectTransform`, `CanvasGroup`, 9-Sliced 배경 Image, 꼬리 Image, Title TMP와 Description TMP를 구성한다. 모든 Graphic의 Raycast Target을 끈다.
- `TutorialRiskInfoBubblePresenter`에 Canvas, Bubble Root, Canvas Group, 두 TMP와 플레이어 월드 카메라를 직접 연결한다.
- Bubble Root의 부모 RectTransform은 화면 전체를 덮도록 구성한다. World Y Offset은 기본 `0.6`에서 시작해 `0.5~0.7m` 범위로 조정하고, Screen Offset과 Screen Padding은 화면 비율별로 말풍선이 캐릭터나 화면 경계와 겹치지 않는지 확인하며 조정한다.
- Bubble Root는 시작 시 활성 상태여도 초기화 과정에서 숨겨진다. 3단계 포커스 시에만 표시되고 `4/4` 완료 직후 완전히 비활성화된다.

## 9. Play Mode 필수 확인

- 0단계 진입 전에 DB 전체 학생의 `InitializeTutorialBehaviorRuntime` 호출이 모두 반환되는지 확인한다.
- 0~1단계에 미리 설치한 바리케이드가 2단계 진행도에 포함되는지 확인한다.
- 3-1은 8명이 동시에 출발하고 위험 4명만 체력 0이 한 번씩 집계되는지 확인한다. 학생 공격 피해와 행동 기반 혼잡도는 차단되지만, 정상 학생을 플레이어가 쓰러뜨리면 정규 `InnocentDown` 혼잡도와 경고가 발생하고 진행도는 변하지 않아야 한다.
- 3단계 위험 학생을 포커스하면 UpperChest 본 위의 월드 Y 오프셋 위치를 따라 말풍선이 표시되고, 정상 학생에게는 표시되지 않는지 확인한다. Avatar에 UpperChest가 없으면 Chest 본을 자동 사용한다. 위험 학생이 쓰러진 뒤에도 포커스하면 래그돌의 선택된 상체 본을 추적해 다시 표시되며 `4/4` 직후에는 더 이상 표시되지 않아야 한다.
- 3-1 다음에 3-2가 진입하면서 메인 스테이지와 같은 해킹 정전 팝업·정전음이 발생하고 연구실 조명·reflection·ambient light가 기존 정전 동작대로 꺼지는지 확인한다. 기존 `FuseBoxMarkerUI`는 전기박스와 화면 밖 방향을 안내해야 한다.
- 전기박스 F 길게 누르기 진행·취소·복구 속도가 메인 스테이지와 같고, 복구 완료 시 조명과 전기박스 연출·마커가 정상 복원된 뒤 4-1로 한 번만 전환되는지 확인한다.
- 3-3 진입 시 음식 표본 수와 학생 수가 일치하고, 학생들이 서로 다른 지정 음식을 든 채 연수 출발점에서 각 슬롯까지 달려가 일렬 배치되는지 확인한다. 이동 중·도착 후 피해·부스터·AI·집계에서 제외되어야 한다.
- 고정 옵션이 꺼지면 두 전자레인지의 정상/위험 역할이 무작위로 정해지고, 켜면 지정 전자레인지에만 위험 음식이 들어가는지 확인한다. 위험 조리 VFX와 hum은 음식 제거 전까지 유지되지만 폭발·화재·폭발음·흔들림은 발생하지 않아야 한다.
- 공용 화살표가 이번 진입의 위험 음식 전자레인지 MarkerAnchor에만 표시되고, 정상 음식을 꺼낼 때는 유지되며 위험 음식을 꺼내면 즉시 제거되는지 확인한다.
- 정상 음식을 먼저 꺼내면 기존 `NormalFoodRemoved` 혼잡도만 적용되고 단계가 유지되며, 위험 음식을 꺼낼 때만 `(1/1)` 완료 후 남은 VFX·음향·소품이 정리되는지 확인한다.
- 4-1 실제 `InnocentDown` 증가와 HUD 반영, 4-2 자연 감소 및 Tab 즉시 완료를 확인한다.
- 6단계 학생이 복귀 완료된 Standby 상태인지, 부스터 적중 후 작업이 연속 P-23 시간 동안 유지되어야만 완료되는지 확인한다.
- 7단계는 학생 부스터/작업이 막히고 교수 기여만으로 한 번 완료되는지 확인한다.
- 8단계 실패와 반복 재시작에서 동일 학생 인스턴스, 플레이어, 시점, 장비/탄약, 혼잡도, 프로젝트, 탈출, 시간, 출구, 시설, 세션 자금이 최초 진입 상태로 돌아오는지 확인한다.
- 미니웨이브 준비 단계에서 학생이 `Standby`를 유지하고 타이머가 감소하지 않으며, 장비 교체·보충 시설·탈출구 정비 후 Tab을 누르면 그 상태가 체크포인트가 되는지 확인한다.
- 준비 완료 패널 전환 중 입력이 잠기고, 다음 `MiniWave` 진입과 동시에 동일 로스터가 Warp·행동 시작하며 입력이 복원되는지 확인한다.
- 실패 후 재시작은 준비 단계를 다시 표시하지 않고 준비 완료 체크포인트에서 동일 로스터로 즉시 시작하는지 확인한다.
- 8단계 성공 및 건너뛰기 후 성공 결과 팝업이 표시되고 자동으로 메인 메뉴로 이동하지 않는지 확인한다. 메인 메뉴 버튼은 완료 키를 저장하며 재연수는 완료 키를 지우지 않아야 한다.
- 미니웨이브 성공·실패 팝업 중 플레이어 입력과 게임 시간이 멈추고, 재시작·재연수·메인 메뉴 이동 시 `Time.timeScale == 1`로 복원되는지 확인한다.
- 2단계 각 탈출구 마커가 개별 설치 상태에 따라 제거·재표시되는지 확인한다.
- 3단계 무기 충전소, 6단계 부스터 자판기, 7단계 교수 컴퓨터 Outline 점멸이 단계 종료 후 원래 상태로 복원되는지 확인한다.
- 7단계 교수 컴퓨터 마커가 작업 시작 시 사라지고 완료 전 작업 중단 시 다시 나타나는지 확인한다.
- 별도로 정규 스테이지 회귀 체크리스트를 전부 실행한다. 이 문서의 연결만으로 정규 Play Mode 검증을 대체하지 않는다.

## 미결정값

- P-23: 6단계 연속 작업 확인 시간 `n`
- P-28: 8단계 고정 장비, 슬롯 순서, 탄약, 보충 시설과 비용
- P-29: 미니웨이브 `BehaviorWeightSet` asset과 행동별 가중치

위 값은 코드나 이 문서에서 임의로 지정하지 않았다.
