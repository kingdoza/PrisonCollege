using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StudentDB : PersistentSingleton<StudentDB>
{
    [SerializeField] private StudentEntry[] _studentEntries;


    public StudentEntry GetStudentEntryAt(int idx)
    {
        return _studentEntries[idx];
    }



    public StudentEntry[] GetRandomStudentEntries(int count, out StudentEntry[] remained)
    {
        // 1. 예외 처리: 요청 숫자가 전체보다 많으면 전체를 반환
        if (count >= _studentEntries.Length)
        {
            remained = new StudentEntry[0]; // 남은 인원 없음
            return (StudentEntry[])_studentEntries.Clone();
        }

        // 2. 원본 보존을 위해 리스트로 복사 후 무작위 셔플
        List<StudentEntry> list = _studentEntries.ToList();

        // 피셔-예이츠 셔플(Fisher-Yates Shuffle) 알고리즘
        for (int i = list.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            StudentEntry temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }

        // 3. 앞에서부터 count만큼 가져오기
        StudentEntry[] selected = list.GetRange(0, count).ToArray();

        // 4. 나머지 인원 할당
        remained = list.GetRange(count, list.Count - count).ToArray();

        return selected;
    }
}



[System.Serializable]
public struct StudentEntry
{
    public int id;
    public string koreanName;
    public string englishName;
    public GameObject prefab;
    public Sprite profile;
    public string course;
}