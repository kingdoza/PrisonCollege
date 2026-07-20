using System.Collections.Generic;
using UnityEngine;

public class TutorialHighlighter : MonoBehaviour
{
    private readonly Dictionary<OutlineFader, OutlineSnapshot> _snapshots = new();
    private readonly List<Transform> _activeRoots = new();
    private bool _hasActiveHighlights;



    public bool StartBarricadeHighlights(IReadOnlyList<ExitGate> gates)
    {
        if (gates == null || gates.Count == 0)
        {
            Debug.LogError("강조할 탈출구가 없습니다.", this);
            ClearAllHighlights();
            return false;
        }

        List<Transform> gateRoots = new(gates.Count);
        foreach (ExitGate gate in gates)
        {
            if (gate == null)
            {
                Debug.LogError("강조할 ExitGate 참조가 null입니다.", this);
                ClearAllHighlights();
                return false;
            }
            gateRoots.Add(gate.transform);
        }
        return StartHighlights(gateRoots);
    }



    public bool StartHighlights(IReadOnlyList<Transform> targetRoots)
    {
        ClearAllHighlights();
        if (targetRoots == null || targetRoots.Count == 0)
        {
            Debug.LogError("강조할 대상 루트가 없습니다.", this);
            return false;
        }

        HashSet<Transform> uniqueRoots = new();
        foreach (Transform root in targetRoots)
        {
            if (root == null)
            {
                Debug.LogError("강조할 대상 루트 참조가 null입니다.", this);
                ClearAllHighlights();
                return false;
            }
            if (uniqueRoots.Add(root))
                _activeRoots.Add(root);
        }

        _hasActiveHighlights = true;
        return RefreshActiveHighlights();
    }



    public bool RefreshActiveHighlights()
    {
        if (!_hasActiveHighlights) return false;

        HashSet<OutlineFader> currentFaders = new();
        foreach (Transform root in _activeRoots)
        {
            if (root == null)
            {
                Debug.LogError("강조 중인 대상 루트가 파괴됐습니다.", this);
                ClearAllHighlights();
                return false;
            }

            OutlineFader[] faders = root.GetComponentsInChildren<OutlineFader>(true);
            if (faders.Length == 0)
            {
                Debug.LogError($"[{root.name}] 하위에서 OutlineFader를 찾을 수 없습니다.", root);
                ClearAllHighlights();
                return false;
            }
            foreach (OutlineFader fader in faders)
                if (fader != null) currentFaders.Add(fader);
        }

        List<OutlineFader> staleFaders = new();
        foreach (KeyValuePair<OutlineFader, OutlineSnapshot> pair in _snapshots)
        {
            if (pair.Key != null && currentFaders.Contains(pair.Key)) continue;
            StopAndRestore(pair.Key, pair.Value);
            staleFaders.Add(pair.Key);
        }
        foreach (OutlineFader staleFader in staleFaders)
            _snapshots.Remove(staleFader);

        foreach (OutlineFader fader in currentFaders)
        {
            if (_snapshots.ContainsKey(fader)) continue;

            Outline outline = fader.TargetOutline;
            if (outline == null)
            {
                Debug.LogError($"[{fader.name}] OutlineFader와 같은 오브젝트에 Outline이 없습니다.", fader);
                ClearAllHighlights();
                return false;
            }

            OutlineSnapshot snapshot = new()
            {
                outline = outline,
                enabled = outline.enabled,
                color = outline.OutlineColor,
                width = outline.OutlineWidth,
            };
            _snapshots[fader] = snapshot;
            if (!fader.StartFade(outline.OutlineColor))
            {
                Debug.LogError($"[{fader.name}] OutlineFader 점멸을 시작할 수 없습니다.", fader);
                ClearAllHighlights();
                return false;
            }
        }
        return true;
    }



    public void ClearAllHighlights()
    {
        foreach (KeyValuePair<OutlineFader, OutlineSnapshot> pair in _snapshots)
            StopAndRestore(pair.Key, pair.Value);
        _snapshots.Clear();
        _activeRoots.Clear();
        _hasActiveHighlights = false;
    }



    private static void StopAndRestore(OutlineFader fader, OutlineSnapshot snapshot)
    {
        if (fader != null)
            fader.StopFade();

        Outline outline = snapshot.outline;
        if (outline == null) return;
        outline.OutlineColor = snapshot.color;
        outline.OutlineWidth = snapshot.width;
        outline.enabled = snapshot.enabled;
    }



    private void OnDestroy() => ClearAllHighlights();

    private struct OutlineSnapshot
    {
        public Outline outline;
        public bool enabled;
        public Color color;
        public float width;
    }
}
