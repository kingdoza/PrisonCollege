using UnityEngine;

[CreateAssetMenu(fileName = "StageRuntimeConfig", menuName = "Stage/Runtime Config")]
public class StageRuntimeConfig : ScriptableObject
{
    [Tooltip("기본값은 반드시 Normal입니다. TutorialStage 전용 asset에서만 Tutorial로 바꿉니다.")]
    [SerializeField] private StageRuntimeMode _mode = StageRuntimeMode.Normal;

    [Header("Normal-safe defaults")]
    [SerializeField] private bool _usePreparation = true;
    [SerializeField] private bool _autoSpawnStudents = true;
    [SerializeField] private bool _useInventoryLoadout = true;
    [SerializeField] private bool _useWavePresentation = true;
    [SerializeField] private StageFinishPolicy _finishPolicy = StageFinishPolicy.NormalStageFlow;

    [Header("Tutorial data (Tutorial mode only)")]
    [Tooltip("튜토리얼 메뉴에 표시할 스테이지 이름입니다. 예: 신임교수 연수")]
    [SerializeField] private string _tutorialStageTitle;
    [Tooltip("튜토리얼 씬 시작 시 적용할 고정 Skybox Material입니다. 정규 WaveSystem의 현재 낮/밤 상태는 변경하지 않습니다.")]
    [SerializeField] private Material _tutorialSkybox;
    [SerializeField] private BehaviorWeightSet _tutorialBehaviorWeightSet;
    [Tooltip("0~7단계 장비입니다. WeaponItem asset을 연결하며, 빈 슬롯은 isEmptySlot으로 명시합니다.")]
    [SerializeField] private TutorialLoadoutEntry[] _trainingLoadout = System.Array.Empty<TutorialLoadoutEntry>();
    [Tooltip("P-21: 6단계에서 빈 슬롯에 지급할 연수용 부스터 WeaponItem입니다. fillToMaximum을 켭니다.")]
    [SerializeField] private TutorialLoadoutEntry _workTrainingBoost;
    [Tooltip("P-28 장비입니다. WeaponItem asset, 슬롯 순서, 탄약 방식을 Inspector에서 설정합니다.")]
    [SerializeField] private TutorialLoadoutEntry[] _miniWaveLoadout = System.Array.Empty<TutorialLoadoutEntry>();
    [SerializeField] private float _tutorialChaosFactor = 1f;
    [SerializeField] private float _tutorialProjectFactor = 1f;

    public StageRuntimeMode Mode => _mode;
    public bool IsTutorial => _mode == StageRuntimeMode.Tutorial;
    public bool UsePreparation => _usePreparation;
    public bool AutoSpawnStudents => _autoSpawnStudents;
    public bool UseInventoryLoadout => _useInventoryLoadout;
    public bool UseWavePresentation => _useWavePresentation;
    public StageFinishPolicy FinishPolicy => _finishPolicy;
    public string TutorialStageTitle => _tutorialStageTitle;
    public Material TutorialSkybox => _tutorialSkybox;
    public BehaviorWeightSet TutorialBehaviorWeightSet => _tutorialBehaviorWeightSet;
    public TutorialLoadoutEntry[] TrainingLoadout => _trainingLoadout;
    public TutorialLoadoutEntry WorkTrainingBoost => _workTrainingBoost;
    public TutorialLoadoutEntry[] MiniWaveLoadout => _miniWaveLoadout;
    public float TutorialChaosFactor => _tutorialChaosFactor;
    public float TutorialProjectFactor => _tutorialProjectFactor;
}
