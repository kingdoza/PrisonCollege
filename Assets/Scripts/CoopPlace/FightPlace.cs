using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FightPlace : CoopBehaviorPlace
{
    protected override BehaviorType RequiredBehavior => BehaviorType.Fight;




    private GameObject GetOtherParticipantActor(PostStudent student)
    {
        foreach (ParticipantInfo participant in _participants)
        {
            if (participant.actor != student.gameObject)
                return participant.actor;
        }
        return null;
    }



    protected override void ExecuteStudent(PostStudent student)
    {
        GameObject fightTarget = GetOtherParticipantActor(student);
        student.Blackboard.ExecuteCoop(fightTarget);
        //student.Blackboard.DisableSpot();
    }
}
