using UnityEngine;
using System.Linq;

public class Shackles : MonoBehaviour
{
    [SerializeField] private Transform _chain;
    [SerializeField] private Transform _weight;
    [SerializeField] private Transform _chainParent;


    private void Start()
    {
        bool hasToActivate = AttributeSystem.Instance.IsStudShackle;
        if (hasToActivate == false)
        {
            Destroy(gameObject);
            return;
        }
        //DisaableChainTriggers();
        _chain.SetParent(null);
        _weight.SetParent(null);
    }



    private void DisaableChainTriggers()
    {
        Collider[] chainColliders = _chainParent.GetComponentsInChildren<Collider>();
        foreach (Collider collider in chainColliders.Reverse())
        {
            collider.isTrigger = false;
        }
    }



    private void OnDisable()
    {
        if (_chain != null) _chain.gameObject.SetActive(false);
        if (_weight != null) _weight.gameObject.SetActive(false);
    }
}
