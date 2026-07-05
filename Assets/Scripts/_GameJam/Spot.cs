using System;
using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;

public class Spot : MonoBehaviour
{
    [SerializeField] private string animName;
    [SerializeField] private bool isRootTrans;
    [SerializeField] protected bool hasTurn = true;
    public bool isArrived = false;

    //public AudioClip audioClip;
    public Student student;
    // public Student Student { set
    //     {
    //         if (student == value)
    //             return;
    //         student = value;
    //         if (student)
    //         {
    //             StartCoroutine(Occupy());
    //         }
    //         else
    //         {
    //             StopAllCoroutines();
    //         }
    //     }
    //     get => student;
    // }

    //[SerializeField] private MonoBehaviour usableMono;
    //public IUsable usable;


    private void Awake()
    {
        //audioClip = GetComponent<AudioClip>();
        // if (usableMono != null)
        // {
        //     usable = usableMono as IUsable;
        //     usable.UseChangeEvent.AddListener(OnUseChange);
        // }
    }


    // public void PlayClip()
    // {
    //     AudioSource.PlayClipAtPoint(audioClip, transform.position + Vector3.up * 1.5f);
    // }



    private void OnUseChange()
    {
        student.MoveAndAction(this, State.Walk, "Idle");
    }



    public virtual string GetAnimName()
    {
        return animName;
    }


    public virtual bool GetIsRootTrans()
    {
        return isRootTrans;
    }


    public virtual bool GetIsEscape()
    {
        return false;
    }


    public virtual bool HasToTurn()
    {
        return hasTurn;
    }


    public virtual void Update()
    {
        if (student == null)
        {
            isArrived = false;
        }
    }



    // private IEnumerator Occupy()
    // {
    //     if (usable == null)
    //         yield break;
    //     while (true)
    //     {
    //         yield return new WaitForSeconds(1);
    //         usable.Use();
    //     }
    // }
}
