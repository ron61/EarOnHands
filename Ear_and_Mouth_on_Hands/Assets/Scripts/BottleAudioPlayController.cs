using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BottleAudioPlayController : MonoBehaviour {

    AudioSource audioSource;
    Rigidbody rigid;
    float speed;
    public float acc;
    float latestSpeed;
    public float playLimit;

	// Use this for initialization
	void Start () {
        
        audioSource = this.GetComponent<AudioSource>();
        rigid = this.GetComponent<Rigidbody>();

	}

	
	// Update is called once per frame
	void Update () {

        CalcAcc();

        if(acc > playLimit && audioSource.time > 0.4f){
            audioSource.Play();
        } else if (acc > playLimit && audioSource.time == 0.0f) {
            audioSource.Play();
        }

    }

    void CalcAcc () {
        
        speed = rigid.velocity.magnitude;
        acc = Mathf.Abs(latestSpeed - speed);
        latestSpeed = rigid.velocity.magnitude;

    }

}
