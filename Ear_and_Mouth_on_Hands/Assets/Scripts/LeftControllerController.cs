using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeftControllerController : MonoBehaviour {

    SteamVR_TrackedObject trackedObj;
    public static bool isPushed;
    public static bool isReleased;

    // Use this for initialization
    void Start () {
        trackedObj = GetComponent<SteamVR_TrackedObject>();
    }

    // Update is called once per frame
    void Update () {
        
        var device = SteamVR_Controller.Input((int)trackedObj.index);

        if (
            device.GetPressDown(SteamVR_Controller.ButtonMask.Grip)
        ){
            isPushed = true;
        }else{
            isPushed = false;
        }

        if (
            device.GetPressUp(SteamVR_Controller.ButtonMask.Grip)
           ){
            isReleased = true;
        }else{
            isReleased = false;
        }

    }

}

