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
        radioPos = new Vector3(0.447f, 1.5f, -1.37f);
        bottlePos = new Vector3(0.876f, 1.5f, -1.349f);
    }

    // Update is called once per frame
    void Update()
    {
        ResetPos();
    }

    public void ResetPos()
    {
        if (RightControllerController.isTriggered)
        {
            radio.GetComponent<Transform>().position = radioPos;
            bottle.GetComponent<Transform>().position = bottlePos;

        }
    }
}
