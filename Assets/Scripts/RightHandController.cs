using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RightHandController : MonoBehaviour {

    Animator animator;

	// Use this for initialization
	void Start () {
        this.animator = GetComponent<Animator>();
	}
	
	// Update is called once per frame
	void Update () {
    		
        if(RightControllerController.isPushed){
            this.animator.SetTrigger("FistTrigger");
        }

        if (RightControllerController.isReleased){
            this.animator.SetTrigger("IdleTrigger");
        }

 
	}
}
