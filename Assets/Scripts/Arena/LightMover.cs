using UnityEngine;

public class LightMover : MonoBehaviour
{
    [SerializeField] private Light _mainLight;
    [SerializeField] private Light[] _subLights;
    [SerializeField] private float rotationSpeed = 5f;
    private Transform _focusTarget;



    private void Start()
    {
        _mainLight.innerSpotAngle = 40;
        _mainLight.spotAngle = 80;
        _mainLight.intensity = 40;
        _mainLight.gameObject.SetActive(true);
        foreach (Light light in _subLights)
        {
            light.gameObject.SetActive(false);
        }
    }



    private void Update()
    {
        if (_focusTarget == null) return;
        LookLightTarget(_mainLight);
        foreach (Light light in _subLights)
        {
            if (light.gameObject.activeSelf == false) continue;
            if (light.type == LightType.Spot)
                LookLightTarget(light);
            else if (light.type == LightType.Point)
            {
                light.transform.position = new Vector3(_focusTarget.position.x, light.transform.position.y, _focusTarget.position.z);
            }
        }
    }



    public void SetTarget(Transform target)
    {
        _focusTarget = target;
    }



    private void LookLightTarget(Light light)
    {
        Vector3 direction = (_focusTarget.position + Vector3.up * 0.5f) - light.transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        light.transform.rotation = Quaternion.Slerp(light.transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }



    public void OnFightStarted()
    {
        _mainLight.spotAngle = 150;
        _mainLight.intensity = 20;
        foreach (Light light in _subLights)
        {
            light.gameObject.SetActive(true);
        }
    }
}
