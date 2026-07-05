using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorSpot : Spot
{
    private float crashRemained = 5;
    private Student prevStudent;
    public UpgradeDoors door;
    public float arrivalTime = 0;

    public override string GetAnimName()
    {
        return door.isUpgraded ? "Punch" : "OpenDoor";
    }


    public override bool GetIsRootTrans()
    {
        return !door.isUpgraded;
    }



    public override bool GetIsEscape()
    {
        return !door.isUpgraded;
    }



    // void Update()
    // {
    //     if (student != prevStudent)
    //     {
    //         prevStudent = student;
    //         crashRemained = 5;
    //     }
    //     else
    //     {
    //         crashRemained -= Time.deltaTime;
    //     }

    //     if (crashRemained <= 0)
    //     {
    //         door.DoCrash();
    //         if (student)
    //         {
    //             student.EscapeDoor(this);
    //         }
    //     }
    // }


    public override void Update()
    {
        base.Update();
        if (isArrived)
        {
            arrivalTime += Time.deltaTime;
        }
        else
        {
            arrivalTime = 0;
        }

        if (arrivalTime >= 5)
        {
            door.DoCrash();
            if (student)
            {
                student.EscapeDoor(this);
            }
        }
    }
}
