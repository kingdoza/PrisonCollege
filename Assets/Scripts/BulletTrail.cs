using UnityEngine;

public class BulletTrail : MonoBehaviour
{
    [SerializeField] private float _velocity;
    private Vector3 _targetDestination;
    private bool _isInitialized = false;




    private void Awake()
    {
        gameObject.SetActive(false);
    }




    public void Shot(Vector3 destination)
    {
        _targetDestination = destination;
        transform.LookAt(destination);
        gameObject.SetActive(true);
        _isInitialized = true;
    }



    private void Update()
    {
        if (!_isInitialized) return;
        transform.position = Vector3.MoveTowards(transform.position, _targetDestination, _velocity * Time.deltaTime);
        if (Vector3.Distance(transform.position, _targetDestination) < 0.01f)
        {
            Destroy(gameObject);
        }
    }
}
