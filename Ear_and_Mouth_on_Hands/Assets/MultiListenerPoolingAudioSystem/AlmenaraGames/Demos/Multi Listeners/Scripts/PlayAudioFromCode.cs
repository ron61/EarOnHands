using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlmenaraGames.Demos{
public class PlayAudioFromCode : MonoBehaviour {


	void Awake () {

		//Plays an occludable Audio Object by its identifier and makes that follows the position of this GameObject
		MultiAudioManager.PlayAudioObjectByIdentifier ("bgm test", transform,true);

	}

}

}