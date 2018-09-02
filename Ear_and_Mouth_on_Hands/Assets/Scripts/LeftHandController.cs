using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeftHandController : MonoBehaviour{

    Animator animator;

    // Use this for initialization
    void Start () {
        this.animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update () {

        if (LeftControllerController.isPushed)
        {
            this.animator.SetTrigger("FistTrigger");
            Debug.Log("pushed");
        }

        if (LeftControllerController.isReleased)
        {
            this.animator.SetTrigger("IdleTrigger");
            Debug.Log("released");
        }

    }
}
