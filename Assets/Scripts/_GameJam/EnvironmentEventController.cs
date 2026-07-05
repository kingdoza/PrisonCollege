using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class EnvironmentEventController : MonoBehaviour
{

    public GameObject RainSystem;
    public GameObject FloodSystem;
    public  GameObject RedPointLight;
    public GameObject ElectricShield;
    public  GameObject AreaLight;
    private bool isFlood;

    //for debugg
    void Start()
    {
        // startFlood();
        // startRedLight();   
        // Debug.Log("Debugg");
    }

    GameObject  Temp;

    public void startFlood()
    {
        if (isFlood)
            return;
        isFlood = true;
        Temp = Instantiate(RainSystem);
        Temp.SetActive(true);
        Temp.transform.position = RainSystem.transform.position;
        // RainSystem.SetActive(true);
        FloodSystem.SetActive(true);
        FloodSystem.GetComponent<FloodController>().startFlood();
        StartCoroutine(endFloodRoutine());
    }

    IEnumerator endFloodRoutine()
    {
        yield return new WaitForSeconds(10);
        // RainSystem.SetActive(false);
        Destroy(Temp);
        yield return new WaitForSeconds(5);
        FloodSystem.SetActive(false);
        isFlood = false;
    }

    public void startRedLight()
    {
        // Debug.Log("Start");
        AreaLight.SetActive(false);
        RedPointLight.SetActive(true);
        ElectricShield.layer = 6;
    }

    public void EndRedLight()
    {
        AreaLight.SetActive(true);
        RedPointLight.SetActive(false);
        ElectricShield.layer = 0;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //startFlood();
        }
    }

}
