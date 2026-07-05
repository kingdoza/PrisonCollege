using UnityEngine;

public class ExplosionShacker : MonoBehaviour
{
    [SerializeField] private float _maxStrength;
    [SerializeField] private float _maxRadius;
    private LayerMask _playerLayer;



    private void Awake()
    {
        _playerLayer = LayerMask.GetMask(Global.PLAYER_LAYER_NAME);
    }



    public void PlayShake(float strengthRate = 1)
    {
        Debug.Log("ExplosionShacker.PlayShake");
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _maxRadius, _playerLayer);

        foreach (var hitCollider in hitColliders)
        {
            Debug.Log("hitCollider : " + hitCollider.name);
            Professor professor = hitCollider.GetComponentInParent<Professor>();
            if (professor == null) continue;

            float distance = Vector3.Distance(transform.position, professor.transform.position);
            float falloffRatio = Mathf.Clamp01(1 - (distance / _maxRadius));
            float finalStrength = (falloffRatio * falloffRatio) * _maxStrength * strengthRate;
            CameraShaker.Instance.DoExplosionShake(finalStrength);
        }
    }
}
