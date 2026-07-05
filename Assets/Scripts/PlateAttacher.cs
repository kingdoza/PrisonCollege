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
    public FoodInfo CurrentFood => _currentFood;


    public override void HideAll()
    {
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