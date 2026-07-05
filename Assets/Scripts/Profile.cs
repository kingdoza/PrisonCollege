using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Profile : MonoBehaviour
{
    [SerializeField] private Image _profileImg;
    [SerializeField] private TextMeshProUGUI _nameTmp;
    [SerializeField] private TextMeshProUGUI _courseTmp;



    private void Awake()
    {
        
    }



    private void Start()
    {
        int siblingIdx = transform.GetSiblingIndex();
        StudentEntry studentEntry = StudentDB.Instance.GetStudentEntryAt(siblingIdx);
        _profileImg.sprite = studentEntry.profile;
        _nameTmp.text = $"{studentEntry.koreanName}<size=70%> ({studentEntry.englishName})</size>";
        _courseTmp.text = studentEntry.course;
    }
}
