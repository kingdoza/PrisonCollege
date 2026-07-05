using System.Linq;
using UnityEngine;

public abstract class ItemDecorator : MonoBehaviour
{
    [SerializeField] private GameObject _decPrefab;
    private Transform[] _sockets;
    private bool _hasToDecorate;



    protected virtual void Start()
    {
        _hasToDecorate = GetItemActivation();
        if (_hasToDecorate == false) return;
        _sockets = GetComponentsInChildren<Transform>().Skip(1).ToArray();

        foreach (Transform socket in _sockets)
        {
            Instantiate(_decPrefab, socket);
        }
    }



    protected abstract bool GetItemActivation();
}
