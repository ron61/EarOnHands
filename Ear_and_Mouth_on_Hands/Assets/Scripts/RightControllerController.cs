using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RightControllerController : MonoBehaviour {

    SteamVR_TrackedObject trackedObj;
    public static bool isPushed;
    public static bool isReleased;
    public static bool isTriggered;

    // Use this for initialization
    void Start () {

        trackedObj = GetComponent<SteamVR_TrackedObject>();

    }

    // Update is called once per frame
    public void Update () {

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

    }

}

