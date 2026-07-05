using UnityEngine;
using UnityEngine.Events;

public class CoopPartSpot : SingleStudentSpot
{
    public UnityEvent ReleaseEvent;
    public UnityEvent<PostStudent> ArrivedEvent;



    public override void Release(PostStudent student)
    {
        base.Release(student);
        ReleaseEvent?.Invoke();
    }



    public override void Arrived(PostStudent userStudent)
    {
        base.Arrived(userStudent);
        ArrivedEvent?.Invoke(_occupant);
    }
}
