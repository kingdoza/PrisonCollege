using UnityEngine;

public class Barricade : MonoBehaviour
{
    [SerializeField] private SoundData _breakSD;
    public SoundData BreakSD => _breakSD;
}
