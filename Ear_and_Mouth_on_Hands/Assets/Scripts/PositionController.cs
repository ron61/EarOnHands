using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionController : MonoBehaviour
{

    GameObject radio, bottle;
    Vector3 radioPos, bottlePos;

    // Use this for initialization
    void Start()
    {
        radio = GameObject.Find("radio");
        bottle = GameObject.Find("bottle");
        radioPos = radio.GetComponent<Transform>().position;
        bottlePos = bottle.GetComponent<Transform>().position;
    }

    // Update is called once per frame
    void Update()
    {
        if (RightControllerController.isTriggered)
        {
            radioPos = new Vector3(-0.02f, 1.29f, 0.24f);
            bottlePos = new Vector3(-0.33f, 1.0f, 0.02f);

            radio.GetComponent<Transform>().position = radioPos;
            bottle.GetComponent<Transform>().position = bottlePos;

        }

       
    }
}
