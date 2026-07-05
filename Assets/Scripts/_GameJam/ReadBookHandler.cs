using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReadBookHandler : MonoBehaviour,IInteractable
{
    // private const int INTERACTABLE = 6;
    // private const int NON_INTERACTABLE = 0;
    // private int readCount;

    // [SerializeField]GameSystem gameSystem;

    // void Start()
    // {
    //     readCount = 0;
    //     gameObject.layer = INTERACTABLE;
    // }

    public void OnInteract()
    {
        // readCount++;
        // gameSystem.UpdateScore();
        // // if(readCount>=3)
        // // {
        // //     // Debug.Log("You read this all!");
        // //     gameObject.layer = NON_INTERACTABLE;
        // // }

    }

    public int GetSpecific()
    {
        return -1;
    }
}
