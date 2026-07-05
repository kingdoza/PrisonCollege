using UnityEngine;

public class StageLayout : MonoBehaviour
{
    [SerializeField] private GameObject _stageSlotObjPrefab;
    [SerializeField] private SimplePanel _stageSelectPanel;
    private StageSlot[] _stageSlots;
    private StageSlot _selectedStage;



    private void Awake()
    {
        //_stageSlots = GetComponentsInChildren<StageSlot>(true);
        //foreach (var slot in _stageSlots)
        //{
        //    slot.MouseClickEvent.AddListener(OnSlotMouseClicked);
        //}
        _stageSelectPanel.DeactivateEvent.AddListener(UnselectStage);
    }



    private void Start()
    {
        MakeStageSlots();
    }



    private void MakeStageSlots()
    {
        StageInfo[] stageInfos = GameManager.Instance.StageEntries;
        _stageSlots = new StageSlot[stageInfos.Length];
        for (int i = 0; i < stageInfos.Length; i++)
        {
            GameObject stageSlotObj = Instantiate(_stageSlotObjPrefab, transform);
            StageSlot stageSlot = stageSlotObj.GetComponent<StageSlot>();
            stageSlot.Init(stageInfos[i]);
            stageSlot.MouseClickEvent.AddListener(OnSlotMouseClicked);
            _stageSlots[i] = stageSlot;
        }
    }



    private void UnselectStage()
    {
        _selectedStage?.Unfocus();
        _selectedStage = null;
    }



    private void OnSlotMouseClicked(StageSlot targetSlot)
    {
        if (targetSlot == _selectedStage)
        {
            _selectedStage.Unfocus();
            _selectedStage = null;
        }
        else
        {
            _selectedStage?.Unfocus();
            _selectedStage = targetSlot;
            _selectedStage.Focus();
        }
    }
}
