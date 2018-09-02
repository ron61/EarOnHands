using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BottleAudioPlayController : MonoBehaviour {

    AudioSource audioSource;
    Rigidbody rigid;
    float speed;
    float acc;
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

        if(acc > playLimit && !audioSource.isPlaying){
            audioSource.Play();
        }

        if(audioSource.isPlaying){
            //Debug.Log("playing now");
        }

        Debug.Log(acc);

	}

    void CalcAcc () {
        
        speed = rigid.velocity.magnitude;
        acc = Mathf.Abs(latestSpeed - speed);
        latestSpeed = rigid.velocity.magnitude;

    }

}
