using System;
using UnityEngine;

public class Microwave : MonoBehaviour
{
    [SerializeField] private ParticleSystem _explosionParticle;
    [SerializeField] [Range(0f, 1f)] private float _fireInvokeThereshold;
    [SerializeField] private Transform _foodSocket;
    [SerializeField] private Light _cookingLight;
    [SerializeField] private SoundData _humSD;
    [SerializeField] private SoundData _explosionSD;
    private ExplosionShacker _explosionShacker;
    private Click _interaction;
    private Duration _operateDuration;
    private Fire _fire;

    private bool _isOperating = false;
    private FoodInfo _currentFoodInside = null;

    public bool IsOperating => _isOperating;
    private SoundEmitter _emitter;



    private void Awake()
    {
        _cookingLight.enabled = false;
        _interaction = GetComponent<Click>();
        _explosionParticle.gameObject.SetActive(false);
        _operateDuration = GetComponent<Duration>();
        _fire = GetComponent<Fire>();
        _operateDuration.Initialize(true);
        _operateDuration.MaxReachEvent.AddListener(Quit);
        _explosionShacker = GetComponent<ExplosionShacker>();

        _interaction.ClickEvent.AddListener(RemoveFood);
        _interaction.InteractState = false;
        _interaction.ActionName = "À½½Ä »©±â";
        _interaction.FillAmount = 1f;
    }



    private void Update()
    {
        if (_isOperating == false) return;
        _operateDuration.Increase(Time.deltaTime);
        if (_operateDuration.Ratio >= _fireInvokeThereshold && _currentFoodInside != null && _currentFoodInside.isCauseFire)
        {
            Explode();
        }
    }



    public void PutFood(FoodInfo foodInfo)
    {
        Destroy(_currentFoodInside?.gameObj);
        _currentFoodInside = new();
        _currentFoodInside.isCauseFire = foodInfo.isCauseFire;
        Quaternion initialRotation = Quaternion.Euler(-90f, 0f, 0f);
        _currentFoodInside.gameObj = Instantiate(foodInfo.gameObj, _foodSocket.position, initialRotation, _foodSocket);
        //AttachProp(_currentFoodInside.gameObj, _foodSocket);
        _currentFoodInside.gameObj.SetActive(true);
        _interaction.InteractState = true;
    }



    public void Operate()
    {
        if (_currentFoodInside == null) return;
        _isOperating = true;
        _emitter = SoundUtils.PlayOwnedScene3DSFX(_humSD, transform.position, false, 1, true);
        _operateDuration.Initialize(true);
        _cookingLight.enabled = true;
    }



    private void RemoveFood()
    {
        if (_currentFoodInside != null && !_currentFoodInside.isCauseFire)
        {
            StageController.Instance.NormalFoodRemoved();
        }
        Quit();
    }



    public void Quit()
    {
        _isOperating = false;
        _cookingLight.enabled = false;
        _currentFoodInside?.gameObj.SetActive(false);
        _currentFoodInside = null;
        _interaction.InteractState = false;
        _emitter?.StopAndReturn();
        _emitter = null;
    }



    private void Explode()
    {
        _explosionParticle.gameObject.SetActive(true);
        _explosionParticle.Play();
        _explosionShacker.PlayShake();
        SoundUtils.PlayScene3DSFX(_explosionSD, transform.position);
        _fire.Ignite();
        Quit();
    }



    protected virtual void AttachProp(GameObject prop, Transform targetSocket)
    {
        prop.transform.SetParent(targetSocket);
        prop.transform.localPosition = Vector3.zero;
        prop.transform.localRotation = Quaternion.identity;
    }
}



[Serializable]
public class FoodInfo
{
    public bool isCauseFire;
    public GameObject gameObj;
}