using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanVolumeController : MonoBehaviour{
    
    public AudioSource audioSource;
    public GameObject Lhand;
    public GameObject Rhand;
    private float distanceL, distanceR, pan, volume, shorter_distance;
    public float range;

    // Use this for initialization
    void Start(){

    }

    // Update is called once per frame
    void Update(){

        distanceL = (this.transform.position - Lhand.transform.position).sqrMagnitude;
        distanceR = (this.transform.position - Rhand.transform.position).sqrMagnitude;
        pan = (distanceL - distanceR) / (distanceL * 1.01f + distanceR * 1.01f);
        audioSource.panStereo = pan;

        shorter_distance = System.Math.Min(distanceL, distanceR);
        volume = 1 - Mathf.Log10((10 - 1) * shorter_distance / range + 1);
        audioSource.volume = System.Math.Max(volume, 0);

    }
}