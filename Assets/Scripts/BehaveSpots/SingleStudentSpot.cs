using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleStudentSpot : BehaveSpot
{
    [SerializeField] protected PostStudent _occupant;
    //public PostStudent Occupant => _occupant;
    public override bool IsUsable => base.IsUsable && _occupant == null;



    public override void Use(PostStudent userStudent) 
    {
        base.Use(userStudent);
        _occupant = userStudent;
    }



    public override void Release(PostStudent userStudent) 
    {
        base.Release(userStudent);
        _occupant = null;
    }
}
