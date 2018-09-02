using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FilterController : MonoBehaviour {

    AudioLowPassFilter radioFilter, bottleFilter;
    GameObject radio, bottle;

	// Use this for initialization
	void Start () {
        radio = GameObject.Find("radio");
        bottle = GameObject.Find("bottle");
        radioFilter = radio.GetComponent<AudioLowPassFilter>();
        bottleFilter = bottle.GetComponent<AudioLowPassFilter>();
	}
	
	// Update is called once per frame
	void Update () {

        //controll the filter of radio
        if (RightControllerController.isRightRadioHidden && LeftControllerController.isLeftRadioHidden)
        {
            radioFilter.cutoffFrequency = 3000.0f;
            Debug.Log("radio filter");
        }
        else
        {
            radioFilter.cutoffFrequency = 22000.0f;
        }
        
        //controll the filter of radio
        if (RightControllerController.isRightBottleHidden && LeftControllerController.isLeftBottleHidden)
        {
            bottleFilter.cutoffFrequency = 3000.0f;
            Debug.Log("bottle filter");
        }
        else
        {
            bottleFilter.cutoffFrequency = 22000.0f;
        }
    }
}
