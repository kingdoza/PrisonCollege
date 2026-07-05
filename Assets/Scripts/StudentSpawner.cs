using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class StudentSpawner : MonoBehaviour
{
    [SerializeField] private SpawnEntry[] spawnEntries;



    public List<PostStudent> SpawnStudents(BehaviorWeightSet behaviorWeightSet)
    {
        int index = 0;
        List<PostStudent> studentList = new();
        foreach (var entry in spawnEntries)
        {
            Quaternion randomYRotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
            PostStudent student = Instantiate(entry.studentPrefab, entry.spawnTransform.position, randomYRotation).GetComponent<PostStudent>();
            student.name = "Student" + ++index;
            student.BehaviorWeightSet = behaviorWeightSet;
            student.SeatSpot = entry.seatSpot;
            studentList.Add(student);
        }
        return studentList;
    }
}


[System.Serializable]
public struct SpawnEntry
{
    public GameObject studentPrefab;
    public MonitorSpot seatSpot;
    public Transform spawnTransform;
}