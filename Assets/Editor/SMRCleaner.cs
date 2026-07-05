using UnityEngine;
using UnityEditor;

public class HierarchyOptimizer : EditorWindow
{
    // 인스펙터처럼 대상을 할당받기 위한 변수
    public GameObject targetObject;

    [MenuItem("Tools/Hierarchy Optimizer (Targeted)")]
    public static void ShowWindow() => GetWindow<HierarchyOptimizer>("Optimizer");

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("대상 오브젝트를 드래그하고 버튼을 누르세요.", MessageType.Info);

        // 대상을 인자로 받기 위한 필드
        targetObject = (GameObject)EditorGUILayout.ObjectField("Target Object", targetObject, typeof(GameObject), true);

        if (GUILayout.Button("최적화 실행"))
        {
            if (targetObject != null)
            {
                ExecuteOptimization(targetObject);
            }
            else
            {
                Debug.LogError("Target Object가 비어있습니다!");
            }
        }
    }

    /// <summary>
    /// 외부 스크립트에서도 호출 가능하도록 타겟을 인자로 받는 메서드
    /// </summary>
    public static void ExecuteOptimization(GameObject target)
    {
        if (target == null) return;

        // 1. LOD 오브젝트 삭제 (이름 끝 lod1~4)
        Transform[] allChildren = target.GetComponentsInChildren<Transform>(true);
        int deleteCount = 0;

        for (int i = allChildren.Length - 1; i >= 0; i--)
        {
            Transform child = allChildren[i];
            if (child == null || child == target.transform) continue;

            string name = child.name.ToLower();
            if (name.EndsWith("lod1") || name.EndsWith("lod2") || name.EndsWith("lod3") || name.EndsWith("lod4"))
            {
                Undo.DestroyObjectImmediate(child.gameObject);
                deleteCount++;
            }
        }

        // 2. 모든 하위 SMR의 Update When Offscreen 활성화
        SkinnedMeshRenderer[] smrs = target.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var smr in smrs)
        {
            Undo.RecordObject(smr, "Update When Offscreen 활성화");
            smr.updateWhenOffscreen = true;
        }

        Debug.Log($"[완료] {target.name} 하위 {deleteCount}개 삭제 및 {smrs.Length}개 SMR 설정 변경.");
    }
}