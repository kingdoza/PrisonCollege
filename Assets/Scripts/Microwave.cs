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
    [SerializeField] private LoopingVfxController _hazardCookingVfx;
    [SerializeField, Min(0f)] private float _foodRotationSpeed = 90f;
    private ExplosionShacker _explosionShacker;
    private Click _interaction;
    private Duration _operateDuration;
    private Fire _fire;

    private bool _isOperating = false;
    private bool _tutorialExplosionSuppressed;
    private FoodInfo _currentFoodInside = null;

    public bool IsOperating => _isOperating;
    public bool HasFood => _currentFoodInside != null;
    public bool HasHazardFood => _currentFoodInside != null && _currentFoodInside.isCauseFire;
    public GameObject CurrentFoodObject => _currentFoodInside?.gameObj;
    public event Action<Microwave, bool> FoodRemovedEvent;
    public event Action<Microwave> ExplodedEvent;
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
        _interaction.ActionName = "음식 빼기";
        _interaction.FillAmount = 1f;
        RefreshHazardCookingVfx();
    }



    private void Update()
    {
        if (_isOperating == false) return;
        RotateFood(Time.deltaTime);
        // 3-3 연수에서는 두 전자레인지가 음식 제거 전까지 계속 작동해야 한다.
        // Duration을 진행시키면 MaxReachEvent의 Quit 또는 위험 음식의 Explode가 발생하므로
        // 명시적으로 주입된 튜토리얼 억제 상태에서만 조리 시간을 고정한다.
        if (_tutorialExplosionSuppressed) return;
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
        RefreshHazardCookingVfx();
    }



    public void Operate()
    {
        if (_currentFoodInside == null) return;
        _isOperating = true;
        _emitter = SoundUtils.PlayOwnedScene3DSFX(_humSD, transform.position, false, 1, true);
        _operateDuration.Initialize(true);
        _cookingLight.enabled = true;
        RefreshHazardCookingVfx();
    }



    private void RemoveFood()
    {
        bool wasHazard = _currentFoodInside != null && _currentFoodInside.isCauseFire;
        bool hadFood = _currentFoodInside != null;
        if (_currentFoodInside != null && !_currentFoodInside.isCauseFire)
        {
            StageController.Instance.NormalFoodRemoved();
        }
        Quit();
        if (hadFood)
            FoodRemovedEvent?.Invoke(this, wasHazard);
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
        RefreshHazardCookingVfx();
    }



    private void Explode()
    {
        _explosionParticle.gameObject.SetActive(true);
        _explosionParticle.Play();
        _explosionShacker.PlayShake();
        SoundUtils.PlayScene3DSFX(_explosionSD, transform.position);
        _fire.Ignite();
        Quit();
        ExplodedEvent?.Invoke(this);
    }



    public TutorialMicrowaveState CaptureTutorialState()
    {
        return new TutorialMicrowaveState
        {
            microwave = this,
            hasFood = HasFood,
            isHazardFood = HasHazardFood,
            isOperating = IsOperating,
            foodObject = CurrentFoodObject,
        };
    }



    public void RestoreTutorialState(TutorialMicrowaveState state)
    {
        if (StageController.Instance == null || !StageController.Instance.IsTutorialRuntime)
        {
            Debug.LogError($"[{name}] Microwave 튜토리얼 복원 API는 튜토리얼 runtime에서만 사용할 수 있습니다.", this);
            return;
        }

        Quit();
        if (!state.hasFood || state.foodObject == null) return;
        PutFood(new FoodInfo { gameObj = state.foodObject, isCauseFire = state.isHazardFood });
        if (state.isOperating) Operate();
    }



    private void RotateFood(float deltaTime)
    {
        if (_currentFoodInside?.gameObj == null || _foodRotationSpeed <= 0f)
            return;

        _currentFoodInside.gameObj.transform.Rotate(
            Vector3.up,
            _foodRotationSpeed * deltaTime,
            Space.World);
    }



    public bool SetTutorialExplosionSuppressed(bool isSuppressed)
    {
        if (StageController.Instance == null || !StageController.Instance.IsTutorialRuntime)
        {
            Debug.LogError($"[{name}] 전자레인지 폭발 억제는 튜토리얼 runtime에서만 사용할 수 있습니다.", this);
            return false;
        }

        _tutorialExplosionSuppressed = isSuppressed;
        return true;
    }



    private void RefreshHazardCookingVfx()
    {
        if (_isOperating && _currentFoodInside != null && _currentFoodInside.isCauseFire)
        {
            _hazardCookingVfx?.Play();
        }
        else
        {
            _hazardCookingVfx?.Stop();
        }
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
