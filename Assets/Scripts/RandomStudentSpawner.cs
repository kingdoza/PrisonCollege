using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class RandomStudentSpawner : MonoBehaviour
{
    [SerializeField] private MonitorSpot[] _seatSpots;



    public List<PostStudent> SpawnStudents(BehaviorWeightSet behaviorWeightSet)
    {
        int spawnCount = _seatSpots.Length;
        PostStudent[] students = new PostStudent[spawnCount];
        StudentEntry[] spawnStudentEntries = StudentDB.Instance.GetRandomStudentEntries(spawnCount, out StudentEntry[] rema);

        for (int i = 0; i < spawnCount; ++i)
        {
            StudentEntry studentEntry = spawnStudentEntries[i];
            Quaternion randomYRotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360f), 0);
            Vector3 spawnPosition = GetSpotMovableRandomPoint(_seatSpots[i].transform.position);
            PostStudent student = Instantiate(studentEntry.prefab, spawnPosition, randomYRotation).GetComponent<PostStudent>();
            student.name = "Student" + (i + 1);
            student.Name = studentEntry.koreanName;
            student.BehaviorWeightSet = behaviorWeightSet;
            student.SeatSpot = _seatSpots[i];
            students[i] = student;
        }

        return students.ToList();
    }



    public Vector3 GetSpotMovableRandomPoint(Vector3 spotPosition)
    {
        // 1. 현재 구워진 NavMesh의 모든 정점/삼각형 정보를 가져옴
        NavMeshTriangulation navData = NavMesh.CalculateTriangulation();

        for (int i = 0; i < 30; i++) // 최대 30번 시도
        {
            // 2. 전체 NavMesh 삼각형 중 무작위로 하나를 선택
            int t = UnityEngine.Random.Range(0, navData.indices.Length / 3);

            // 3. 선택된 삼각형 내부의 한 점을 구함 (가장 확실한 '내부' 좌표)
            Vector3 v1 = navData.vertices[navData.indices[t * 3]];
            Vector3 v2 = navData.vertices[navData.indices[t * 3 + 1]];
            Vector3 v3 = navData.vertices[navData.indices[t * 3 + 2]];

            // 삼각형 내 랜덤 좌표 생성 공식
            float r1 = UnityEngine.Random.value;
            float r2 = UnityEngine.Random.value;
            if (r1 + r2 > 1) { r1 = 1 - r1; r2 = 1 - r2; }
            Vector3 randomPoint = v1 + r1 * (v2 - v1) + r2 * (v3 - v1);

            // 4. 해당 지점에서 spotPosition까지 경로가 유효한지 체크
            NavMeshPath path = new NavMeshPath();
            if (NavMesh.CalculatePath(randomPoint, spotPosition, NavMesh.AllAreas, path))
            {
                if (path.status == NavMeshPathStatus.PathComplete)
                {
                    return randomPoint; // 길이 완벽하게 연결된 지점 발견!
                }
            }
        }

        // 실패 시 안전장치
        return spotPosition;
    }
}