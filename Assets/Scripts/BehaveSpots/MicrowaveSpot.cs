using UnityEngine;

public class MicrowaveSpot : SingleStudentSpot
{
    [SerializeField] private Microwave _microwave;
    public override bool IsUsable =>  base.IsUsable && !_microwave.IsOperating;



    public void OperateMicrowave()
    {
        _microwave.Operate();
    }



    public void PutFoodInMicrowave(FoodInfo foodInfo)
    {
        _microwave.PutFood(foodInfo);
    }
}
