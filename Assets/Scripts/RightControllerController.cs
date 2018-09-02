using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RightControllerController : MonoBehaviour {

    SteamVR_TrackedObject trackedObj;
    public static bool isPushed;
    public static bool isReleased;
    public static bool isTriggered;
    public static bool isRightRadioHidden;
    public static bool isRightBottleHidden;
    GameObject bottle;
    GameObject radio;
    float radio_distance, radio_obst_dist;
    float bottle_distance, bottle_obst_dist;

    // Use this for initialization
    void Start () {
        trackedObj = GetComponent<SteamVR_TrackedObject>();
        bottle = GameObject.Find("bottle");
        radio = GameObject.Find("radio");
    }

    // Update is called once per frame
    void Update () {

        RaycastHit hit;

        //check if the grip button and trigger is pushed or not
        var device = SteamVR_Controller.Input((int)trackedObj.index);

        if (device.GetPressDown(SteamVR_Controller.ButtonMask.Grip))
        {
            isPushed = true;
        }
        else
        {
            isPushed = false;
        }

        if (device.GetPressUp(SteamVR_Controller.ButtonMask.Grip))
        {
            isReleased = true;
        }
        else
        {
            isReleased = false;
        }

        if (device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
        {
            isTriggered = true;
        }
        else
        {
            isTriggered = false;
        }

        //filter if there are obstacles between hand and audio source
        if (Physics.Raycast(trackedObj.transform.position, radio.transform.position - trackedObj.transform.position, out hit, Mathf.Infinity))
        {
            radio_obst_dist = Vector3.Distance(hit.transform.position, trackedObj.transform.position);
            radio_distance = Vector3.Distance(trackedObj.transform.position, radio.transform.position);
            

            if (radio_distance > radio_obst_dist + 0.01f)
            {
                //Debug.Log("right radio filter");
                isRightRadioHidden = true;
            }
            else
            {
                isRightRadioHidden = false;
            }
        }

        if (Physics.Raycast(trackedObj.transform.position, bottle.transform.position - trackedObj.transform.position, out hit, Mathf.Infinity))
        {
            bottle_obst_dist = Vector3.Distance(hit.transform.position, trackedObj.transform.position);
            bottle_distance = Vector3.Distance(trackedObj.transform.position, bottle.transform.position);

            if (bottle_distance > bottle_obst_dist + 0.01f)
            {
                //Debug.Log("right bottle filter");
                isRightBottleHidden = true;
            }
            else
            {
                isRightBottleHidden = false;
            }
        }

    }

}

