using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindowSpot : Spot
{
    private float crashRemained = 5;
    private Student prevStudent;
    public UpgradeDoors window;
    public float arrivalTime = 0;

    public override string GetAnimName()
    {
        return window.isUpgraded ? "Punch" : "JumpWindow";
    }


    public override bool GetIsRootTrans()
    {
        return !window.isUpgraded;
    }



    public override bool GetIsEscape()
    {
        return !window.isUpgraded;
    }


    // void Update()
    // {
    //     if (student != prevStudent)
    //     {
    //         prevStudent = student;
    //         crashRemained = 5;
    //     }
    //     else if (student)
    //     {
    //         crashRemained -= Time.deltaTime;
    //     }

    //     if (crashRemained <= 0)
    //     {
    //         window.DoCrash();
    //         if (student)
    //         {
    //             student.EscapeWindow(this);
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
            window.DoCrash();
            if (student)
            {
                student.EscapeWindow(this);
            }
        }
    }
}
