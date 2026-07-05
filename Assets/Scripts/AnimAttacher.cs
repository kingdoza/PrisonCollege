using UnityEngine;

public abstract class AnimAttacher : MonoBehaviour
{
    public abstract void HideAll();


    protected virtual void AttachProp(GameObject prop, Transform targetSocket)
    {
        prop.transform.SetParent(targetSocket);
        prop.transform.localPosition = Vector3.zero;
        prop.transform.localRotation = Quaternion.identity;
    }
}
