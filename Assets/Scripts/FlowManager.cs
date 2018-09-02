using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlowManager : MonoBehaviour {

    int k;
    GameObject narator, radio, bottle;
    AudioSource audioSource;
    public AudioClip clip1, clip2, clip3, clip4, clip5, clip6;
    float time;
    Vector3 radioPos, bottlePos, farPos;

	// Use this for initialization
	void Start () {

        k = 0;
        time = 0.0f;
        narator = GameObject.Find("Narator");
        bottle = GameObject.Find("bottle");
        radio = GameObject.Find("radio");
        audioSource = narator.GetComponent<AudioSource>();
        radioPos = new Vector3(-0.115f, 1.5f, -0.432f);
        bottlePos = new Vector3(-0.102f, 1.5f, -0.845f);
        farPos = new Vector3(100, 2, 100);

    }
	
	// Update is called once per frame
	void Update () {

        time += Time.deltaTime;
       
        if (k == 5 && RightControllerController.isTriggered && time > 20.0f)
        {
            audioSource.PlayOneShot(clip6);
        }

        if (k == 4 && RightControllerController.isTriggered && !audioSource.isPlaying)
        {
            bottle.transform.position = bottlePos;
            radio.transform.position = radioPos;
            audioSource.PlayOneShot(clip5);
            time = 0.0f;
            k++;
        }

        if (k == 3 && RightControllerController.isTriggered && !audioSource.isPlaying)
        {
            bottle.transform.position = farPos;
            radio.transform.position = radioPos;
            audioSource.PlayOneShot(clip4);

            k++;
        }

        if (k == 2 && RightControllerController.isTriggered && !audioSource.isPlaying)
        {
            bottle.transform.position = farPos;
            radio.transform.position = radioPos;
            audioSource.PlayOneShot(clip3);
            radio.GetComponent<AudioSource>().Play();

            k++;
        }

        if (k == 1 && RightControllerController.isTriggered && !audioSource.isPlaying)
        {
            bottle.transform.position = bottlePos;
            radio.transform.position = farPos;
            audioSource.PlayOneShot(clip2);
            k++;
        }

        if (k == 0 && RightControllerController.isTriggered /*&& !audioSource.isPlaying*/)
        {
            bottle.transform.position = farPos;
            radio.transform.position = farPos;
            audioSource.PlayOneShot(clip1);
            k++;
        }
    }
}
