using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BoneTransferer : EditorWindow
{
    private GameObject sourceStudent;
    private GameObject targetStudent;

    [MenuItem("Tools/교수님 전용/래그돌 완전 이식(부재 시 자동생성)")]
    public static void ShowWindow() => GetWindow<BoneTransferer>("래그돌 이식");

    private void OnGUI()
    {
        sourceStudent = (GameObject)EditorGUILayout.ObjectField("원본 (래그돌 완료)", sourceStudent, typeof(GameObject), true);
        targetStudent = (GameObject)EditorGUILayout.ObjectField("대상 (새 학생)", targetStudent, typeof(GameObject), true);

        if (GUILayout.Button("래그돌 데이터 통째로 이식하기") && sourceStudent && targetStudent)
        {
            TransferRagdoll();
        }
    }

    private void TransferRagdoll()
    {
        // 1. 대상 학생의 경로 미리 캐싱
        Dictionary<string, Transform> targetPaths = RefreshTargetPaths();

        // 2. 원본 학생 순회 (여기서 생성 로직 포함)
        foreach (var sourceBone in sourceStudent.GetComponentsInChildren<Transform>())
        {
            if (sourceBone == sourceStudent.transform) continue;

            string sourcePath = GetRelativePath(sourceStudent.transform, sourceBone);
            Transform targetBone = GetOrCreatePath(sourcePath);

            if (targetBone != null)
            {
                ClearExistingPhysics(targetBone.gameObject);
                CopyPhysics(sourceBone, targetBone);
            }
        }

        // 3. 조인트 재연결 (새로 생성된 오브젝트들 포함)
        targetPaths = RefreshTargetPaths(); // 생성된 오브젝트 포함해서 갱신
        ReconnectJoints(targetPaths);

        Debug.Log("대상에 없는 오브젝트까지 자동 생성하여 이식을 완료했습니다.");
    }

    // 경로가 없으면 부모를 따라가며 새로 생성하는 핵심 함수
    private Transform GetOrCreatePath(string path)
    {
        string[] nodes = path.Split('/');
        Transform currentParent = targetStudent.transform;

        foreach (string node in nodes)
        {
            Transform found = currentParent.Find(node);
            if (found == null)
            {
                // 없으면 새로 생성
                GameObject newNode = new GameObject(node);
                newNode.transform.SetParent(currentParent);
                // 원본과 동일한 로컬 좌표/회전/스케일 설정 (매우 중요)
                Transform sourceNode = sourceStudent.transform.Find(GetPathUpTo(path, node));
                if (sourceNode != null)
                {
                    newNode.transform.localPosition = sourceNode.localPosition;
                    newNode.transform.localRotation = sourceNode.localRotation;
                    newNode.transform.localScale = sourceNode.localScale;
                }
                found = newNode.transform;
            }
            currentParent = found;
        }
        return currentParent;
    }

    private string GetPathUpTo(string fullPath, string nodeName)
    {
        string[] nodes = fullPath.Split('/');
        string result = "";
        foreach (var n in nodes)
        {
            result += (result == "" ? "" : "/") + n;
            if (n == nodeName) break;
        }
        return result;
    }

    private void CopyPhysics(Transform source, Transform target)
    {
        // 콜라이더 복사
        foreach (var col in source.GetComponents<Collider>())
        {
            UnityEditorInternal.ComponentUtility.CopyComponent(col);
            UnityEditorInternal.ComponentUtility.PasteComponentAsNew(target.gameObject);
        }
        // 리지드바디 복사
        var rb = source.GetComponent<Rigidbody>();
        if (rb != null)
        {
            UnityEditorInternal.ComponentUtility.CopyComponent(rb);
            UnityEditorInternal.ComponentUtility.PasteComponentAsNew(target.gameObject);
        }
        // 조인트 복사
        var joint = source.GetComponent<CharacterJoint>();
        if (joint != null)
        {
            UnityEditorInternal.ComponentUtility.CopyComponent(joint);
            UnityEditorInternal.ComponentUtility.PasteComponentAsNew(target.gameObject);
        }
    }

    private void ReconnectJoints(Dictionary<string, Transform> targetPaths)
    {
        foreach (var targetBone in targetStudent.GetComponentsInChildren<CharacterJoint>())
        {
            string path = GetRelativePath(targetStudent.transform, targetBone.transform);
            Transform sourceBone = sourceStudent.transform.Find(path);
            if (sourceBone == null) continue;

            CharacterJoint sJoint = sourceBone.GetComponent<CharacterJoint>();
            if (sJoint != null && sJoint.connectedBody != null)
            {
                string connectedPath = GetRelativePath(sourceStudent.transform, sJoint.connectedBody.transform);
                if (targetPaths.TryGetValue(connectedPath, out Transform newConnected))
                {
                    targetBone.connectedBody = newConnected.GetComponent<Rigidbody>();
                }
            }
        }
    }

    private Dictionary<string, Transform> RefreshTargetPaths()
    {
        Dictionary<string, Transform> paths = new Dictionary<string, Transform>();
        foreach (var t in targetStudent.GetComponentsInChildren<Transform>())
        {
            string path = GetRelativePath(targetStudent.transform, t);
            if (!paths.ContainsKey(path)) paths.Add(path, t);
        }
        return paths;
    }

    private string GetRelativePath(Transform root, Transform target)
    {
        if (root == target) return "";
        string path = target.name;
        Transform parent = target.parent;
        while (parent != null && parent != root)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }

    private void ClearExistingPhysics(GameObject obj)
    {
        foreach (var c in obj.GetComponents<CharacterJoint>()) DestroyImmediate(c);
        foreach (var r in obj.GetComponents<Rigidbody>()) DestroyImmediate(r);
        foreach (var l in obj.GetComponents<Collider>()) DestroyImmediate(l);
    }
}