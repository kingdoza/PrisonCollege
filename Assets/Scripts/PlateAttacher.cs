using System.Collections.Generic;
using UnityEngine;

public class PlateAttacher : AnimAttacher
{
    [SerializeField][Range(0, 1)] private float _fireCauseProbabiliy;
    [Header("Sockets")]
    [SerializeField] private Transform _plateHandSocket;
    [SerializeField] private Transform _foodSocket;

    [Header("Props")]
    [SerializeField] private GameObject _plate;
    [SerializeField] private GameObject[] _fireCauseFoods;
    [SerializeField] private GameObject[] _fireNonCauseFoods;

    private FoodInfo _currentFood;
    private GameObject _tutorialFoodInstance;
    private GameObject _tutorialFoodSource;
    public FoodInfo CurrentFood => _currentFood;


    public override void HideAll()
    {
        if (_tutorialFoodInstance != null)
        {
            Destroy(_tutorialFoodInstance);
            _tutorialFoodInstance = null;
        }
        _plate.SetActive(false);
        foreach (GameObject foodObj in _fireCauseFoods)
        {
            foodObj.SetActive(false);
        }
        foreach (GameObject foodObj in _fireNonCauseFoods)
        {
            foodObj.SetActive(false);
        }
    }



    public bool ShowTutorialFood(GameObject foodSource)
    {
        if (foodSource == null || _plate == null || _plateHandSocket == null || _foodSocket == null)
            return false;

        _tutorialFoodSource = foodSource;
        ShowTutorialFoodInternal();
        return true;
    }



    private void ShowTutorialFoodInternal()
    {
        HideAll();
        AttachProp(_plate, _plateHandSocket);
        _plate.SetActive(true);
        _tutorialFoodInstance = Instantiate(_tutorialFoodSource, _foodSocket);
        _tutorialFoodInstance.transform.localPosition = Vector3.zero;
        _tutorialFoodInstance.transform.localRotation = Quaternion.identity;
        _tutorialFoodInstance.SetActive(true);
    }



    public void ClearTutorialFood()
    {
        _tutorialFoodSource = null;
        HideAll();
    }



    public bool TryGetFoodCatalogSnapshot(out FoodInfo[] foods)
    {
        foods = null;
        if (_fireCauseFoods == null
            || _fireNonCauseFoods == null
            || _fireCauseFoods.Length == 0
            || _fireNonCauseFoods.Length == 0)
            return false;

        FoodInfo[] snapshot = new FoodInfo[_fireCauseFoods.Length + _fireNonCauseFoods.Length];
        HashSet<GameObject> uniqueFoods = new();
        int index = 0;
        foreach (GameObject foodObject in _fireCauseFoods)
        {
            if (foodObject == null || !uniqueFoods.Add(foodObject)) return false;
            snapshot[index++] = new FoodInfo
            {
                gameObj = foodObject,
                isCauseFire = true,
            };
        }
        foreach (GameObject foodObject in _fireNonCauseFoods)
        {
            if (foodObject == null || !uniqueFoods.Add(foodObject)) return false;
            snapshot[index++] = new FoodInfo
            {
                gameObj = foodObject,
                isCauseFire = false,
            };
        }

        foods = snapshot;
        return true;
    }


    private FoodInfo ChooseFood()
    {
        float randValue = UnityEngine.Random.Range(0f, 1f);
        bool isCauseFire = randValue < _fireCauseProbabiliy;
        GameObject foodObj = (isCauseFire)
                ? _fireCauseFoods.GetRandom()
                : _fireNonCauseFoods.GetRandom();
        FoodInfo food = new();
        food.isCauseFire = isCauseFire;
        food.gameObj = foodObj;
        return food;
    }


    public void LiftPlate()
    {
        if (_tutorialFoodSource != null)
        {
            if (_tutorialFoodInstance == null)
                ShowTutorialFoodInternal();
            return;
        }

        HideAll();
        AttachProp(_plate, _plateHandSocket);
        _plate.SetActive(true);
        _currentFood = ChooseFood();
        AttachProp(_currentFood.gameObj, _foodSocket);
        _currentFood.gameObj.SetActive(true);
    }
}




public enum FoodType
{

}
