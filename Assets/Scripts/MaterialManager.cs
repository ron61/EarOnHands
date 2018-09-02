using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialManager : MonoBehaviour {

    //body, rgrip, lgrip, trackpad

    GameObject l_body, l_rgrip, l_lgrip, l_trackpad;
    GameObject r_body, r_rgrip, r_lgrip, r_trackpad;
    public Material material;
    float time = 0.0f;


    // Use this for initialization
    void Start () {

    }

    // Update is called once per frame
    void Update () {
        time += Time.deltaTime;

        if(time > 2.0f && l_body == null) {
            l_body = GameObject.Find("Controller (left)/Model/body");
            l_rgrip = GameObject.Find("Controller (left)/Model/rgrip");
            l_lgrip = GameObject.Find("Controller (left)/Model/lgrip");
            l_trackpad = GameObject.Find("Controller (left)/Model/trackpad");
            r_body = GameObject.Find("Controller (right)/Model/body");
            r_rgrip = GameObject.Find("Controller (right)/Model/rgrip");
            r_lgrip = GameObject.Find("Controller (right)/Model/lgrip");
            r_trackpad = GameObject.Find("Controller (right)/Model/trackpad");

            l_body.GetComponent<Renderer>().material = material;
            l_rgrip.GetComponent<Renderer>().material = material;
            l_lgrip.GetComponent<Renderer>().material = material;
            l_trackpad.GetComponent<Renderer>().material = material;
            r_body.GetComponent<Renderer>().material = material;
            r_lgrip.GetComponent<Renderer>().material = material;
            r_rgrip.GetComponent<Renderer>().material = material;
            r_trackpad.GetComponent<Renderer>().material = material;
        }
        
    }
}
