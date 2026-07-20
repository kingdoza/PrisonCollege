using System.Collections.Generic;
using UnityEngine;

public class TutorialTransientRegistry : MonoBehaviour
{
    private static TutorialTransientRegistry _active;
    private readonly HashSet<GameObject> _objects = new();

    public static TutorialTransientRegistry Active => _active;



    public void ActivateForTutorialScene()
    {
        if (StageController.Instance == null || !StageController.Instance.IsTutorialRuntime)
        {
            Debug.LogError("TutorialTransientRegistry는 튜토리얼 runtime에서만 활성화할 수 있습니다.", this);
            return;
        }
        _active = this;
    }



    public void Register(GameObject transientObject)
    {
        if (transientObject != null)
            _objects.Add(transientObject);
    }



    public void Unregister(GameObject transientObject)
    {
        if (transientObject != null)
            _objects.Remove(transientObject);
    }



    public void ClearAll()
    {
        foreach (GameObject transientObject in _objects)
        {
            if (transientObject != null)
                Destroy(transientObject);
        }
        _objects.Clear();
    }



    private void OnDestroy()
    {
        if (_active == this)
            _active = null;
    }
}
