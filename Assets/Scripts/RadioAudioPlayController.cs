using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadioAudioPlayController : MonoBehaviour {
    
    AudioSource audioSource;


	// Use this for initialization
	void Start () {
        audioSource = this.GetComponent<AudioSource>();
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
