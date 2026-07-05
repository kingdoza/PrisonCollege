using TMPro;
using UnityEngine;

public class LabIntro : MonoBehaviour
{
    [SerializeField] private GameObject _membersPanel;
    [SerializeField] private TextMeshProUGUI _membersBtnTmp;
    [SerializeField] private GameObject _researchPanel;
    [SerializeField] private TextMeshProUGUI _researchBtnTmp;
    [SerializeField] private TextMeshProUGUI _bannerTmp;
    [SerializeField] private TMP_FontAsset _boldFontAsset;
    [SerializeField] private Color _boldColor;


    private Color _originBtnTmpColor;
    private TMP_FontAsset _originalBtnTmpFontAsset;



    private void Awake()
    {
        _originBtnTmpColor = _membersBtnTmp.color;
        _originalBtnTmpFontAsset = _membersBtnTmp.font;
    }



    private void Start()
    {
        Research_Btn();
    }



    public void Members_Btn()
    {
        _membersPanel.SetActive(true);
        _researchPanel.SetActive(false);

        _membersBtnTmp.color = _boldColor;
        _membersBtnTmp.font = _boldFontAsset;
        _membersBtnTmp.SetAllDirty();

        _researchBtnTmp.color = _originBtnTmpColor;
        _researchBtnTmp.font = _boldFontAsset;
        _researchBtnTmp.SetAllDirty();

        _bannerTmp.text = "Members\r\n<size=80%>Students</size>";
    }



    public void Research_Btn()
    {
        _membersPanel.SetActive(false);
        _researchPanel.SetActive(true);

        _researchBtnTmp.color = _boldColor;
        _researchBtnTmp.font = _boldFontAsset;
        _researchBtnTmp.SetAllDirty();

        _membersBtnTmp.color = _originBtnTmpColor;
        _membersBtnTmp.font = _boldFontAsset;
        _membersBtnTmp.SetAllDirty();

        _bannerTmp.text = "Archive\r\n<size=80%>Video Asset</size>";
    }
}
