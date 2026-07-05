using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MultiBoneRemapper : EditorWindow
{
    public GameObject logicRigRoot; // 로직이 붙어있는 기존 뼈 루트 (예: Hips)
    public GameObject newCharacterPrefab; // 새 메쉬들이 있는 모델 프리팹

    [MenuItem("Tools/Multi-SMR Hierarchy Remapper")]
    public static void ShowWindow() => GetWindow<MultiBoneRemapper>("Hierarchy Remapper");

    private void OnGUI()
    {
        logicRigRoot = (GameObject)EditorGUILayout.ObjectField("기존 로직 뼈 루트", logicRigRoot, typeof(GameObject), true);
        newCharacterPrefab = (GameObject)EditorGUILayout.ObjectField("새 캐릭터 모델", newCharacterPrefab, typeof(GameObject), true);

        if (GUILayout.Button("계층 구조 맞춰 재연결 실행") && logicRigRoot != null && newCharacterPrefab != null)
        {
            RemapWithHierarchy();
        }
    }

    private void RemapWithHierarchy()
    {
        // 1. 기존 로직 뼈들을 캐싱
        Dictionary<string, Transform> logicBoneMap = new Dictionary<string, Transform>();
        foreach (var t in logicRigRoot.GetComponentsInChildren<Transform>(true))
        {
            if (!logicBoneMap.ContainsKey(t.name)) logicBoneMap.Add(t.name, t);
        }

        // 2. 새 캐릭터의 전체 구조를 복제 (메쉬 오브젝트들만 가져오기 위해)
        GameObject newModelInstance = Instantiate(newCharacterPrefab, logicRigRoot.transform.parent);
        newModelInstance.name = newCharacterPrefab.name; // 이름 뒤 (Clone) 제거

        // 3. 복제본 내부의 모든 SMR을 돌며 뼈만 기존 로직 뼈로 교체
        SkinnedMeshRenderer[] allSMRs = newModelInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        foreach (var smr in allSMRs)
        {
            Transform[] sourceBones = smr.bones;
            Transform[] remappedBones = new Transform[sourceBones.Length];

            for (int i = 0; i < sourceBones.Length; i++)
            {
                if (logicBoneMap.TryGetValue(sourceBones[i].name, out Transform foundBone))
                {
                    remappedBones[i] = foundBone;
                }
            }

            smr.bones = remappedBones;

            // Root Bone 재연결
            if (smr.rootBone != null && logicBoneMap.TryGetValue(smr.rootBone.name, out Transform root))
            {
                smr.rootBone = root;
            }

            // 불필요해진 새 모델 내부의 뼈대(Transform)들은 무시하고 SMR은 기존 로직 뼈를 바라보게 됨
            Debug.Log($"[연결 완료] {smr.name}");
        }

        // 4. (선택 사항) 새 모델 인스턴스 내부에 들어있던 원본 뼈대들은 로직 뼈와 중복되므로 삭제하거나 정리
        // 하지만 계층 구조 유지를 위해 그대로 두어도 SMR이 'Bones' 배열로 로직 뼈를 가리키므로 작동엔 지장 없습니다.

        Selection.activeGameObject = newModelInstance;
        Debug.Log($"{newCharacterPrefab.name}의 계층 구조를 유지하며 리매핑 완료!");
    }
}