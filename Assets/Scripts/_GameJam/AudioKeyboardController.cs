using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioKeyboardController : MonoBehaviour
{

    void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Student")
        {
            Debug.Log("Student sit");
            GetComponent<AudioSource>().Play();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if(other.tag == "Student")
        {
            Debug.Log("Student up");
            GetComponent<AudioSource>().Stop();
        }
    }
}
