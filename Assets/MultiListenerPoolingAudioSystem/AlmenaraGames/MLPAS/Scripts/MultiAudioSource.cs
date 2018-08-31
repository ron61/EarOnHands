using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using AlmenaraGames;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AlmenaraGames{
	[HelpURL("https://drive.google.com/uc?&id=1wSFIUL0gqVL2Gan0qgB_EZdYGHwa9F_p#page=11")]
	[AddComponentMenu("Almenara Games/MLPAS/Multi Audio Source",-1)]
public class MultiAudioSource : MonoBehaviour {

	[SerializeField] private AudioObject audioObject;

	/// <summary>
	/// Gets or sets the <see cref="AudioObject"/>.
	/// </summary>
	public AudioObject AudioObject{get{ return audioObject; } 
		set{
			
				audioObject = value;

		}
	}

	Transform audioSourceTransform;
	Transform audioSourceReverbTransform;

	MultiAudioListener nearestListener;

	Vector3 nearestListenerPosition;
	Vector3 secondNearestListenerPosition;
	Vector3 thirdNearestListenerPosition;
	Vector3 fourthNearestListenerPosition;

	Vector3 nearestListenerForward;
	Vector3 secondNearestListenerForward;
	Vector3 thirdNearestListenerForward;
	Vector3 fourthNearestListenerForward;

	bool nearestListenerNULL=true;
	bool secondNearestListenerNULL=true;
	bool thirdNearestListenerNULL=true;
	bool fourthNearestListenerNULL=true;

	float nearestListenerDistance;
	float secondNearestListenerDistance;
	float thirdNearestListenerDistance;
	float fourthNearestListenerDistance;

	float nearestListenerBlend;
	/// <summary>
	/// The blend amount of the nearest listener.
	/// </summary>
	public float NearestListenerBlend { get { return nearestListenerBlend; } }
	float secondNearestListenerBlend;
	float thirdNearestListenerBlend;
	float fourthNearestListenerBlend;

	float nearestListenerBlendDistance;
	float secondNearestListenerBlendDistance;
	float thirdNearestListenerBlendDistance;
	float fourthNearestListenerBlendDistance;

	Transform thisTransform;
	Vector3 thisPosition;
	Vector3 averagePosition;
	/// <summary>
	/// The average position of all the listeners.
	/// </summary>
	public Vector3 AveragePosition { get { return averagePosition; } }
	Vector3 smoothedAveragePosition;
	float distanceBetweenListeners;

	[Space(10)]
	[SerializeField] private MultiAudioManager.UpdateModes playUpdateMode=MultiAudioManager.UpdateModes.UnscaledTime;
	/// <summary>
	/// If set to UnscaledTime the audio will always be played at normal speed even with the TimeScale set to 0. Otherwise the audio will multiply its speed by the current TimeScale.
	/// </summary>
	public MultiAudioManager.UpdateModes PlayUpdateMode {get{ return playUpdateMode; }set {changingParameters=true;playUpdateMode = value;}}

	[Space(10)]

	private int channel=-1;
	/// <summary>
	/// Gets the channel where the <see cref="MultiAudioSource"/>is playing.
	/// </summary>
	/// <value>The channel.</value>
	public int Channel { get { return channel; } }

	[Space(10)]
	/// <summary>
	/// The target to follow.
	/// </summary>
	public Transform TargetToFollow;

	[Space(10)]
	/// <summary>
	/// Mute the <see cref="MultiAudioSource"/>.
	/// </summary>
	public bool Mute;

	[Space(10)]
	/// <summary>
	/// Use the override values.
	/// </summary>
	public bool OverrideValues = false;

	[Space(10)]
	[SerializeField] private bool overrideRandomVolumeMultiplier=false;
	[SerializeField] private bool randomVolumeMultiplier = true;
	/// <summary>
	/// Use random volume multiplier.
	/// </summary>
	public bool RandomVolumeMultiplierOverride {get{return randomVolumeMultiplier;} set{OverrideValues=true;changingParameters=true;overrideRandomVolumeMultiplier=true;randomVolumeMultiplier=value;}}
	
	[SerializeField] private bool overrideRandomPitchMultiplier=false;
	[SerializeField] private bool randomPitchMultiplier = true;
	/// <summary>
	/// Use random pitch multiplier.
	/// </summary>
	public bool RandomPitchMultiplierOverride {get{return randomPitchMultiplier;} set{OverrideValues=true;changingParameters=true;overrideRandomPitchMultiplier=true;randomPitchMultiplier=value;}}

	[Space(10)]

	[SerializeField] private bool overrideAudioClip=false;
	[SerializeField] private AudioClip audioClipOverride;
	/// <summary>
	/// Gets or sets the audio clip for override the <see cref="AudioObject"/>default clips.
	/// </summary>
	public AudioClip AudioClipOverride {get{ return audioClipOverride; }set {OverrideValues=true;changingParameters = true;overrideAudioClip=true;audioClipOverride = value;}}

	[Space(10)]

	[SerializeField] private bool overrideVolume=false;
	[Range(0f,2f)]
	[SerializeField] private float volumeOverride=1f;
	/// <summary>
	/// Gets or sets the volume for override the <see cref="AudioObject"/>default volume.
	/// </summary>
		public float VolumeOverride {get{ return volumeOverride; }set {OverrideValues=true;changingParameters = true;overrideVolume = true;volumeOverride =  Mathf.Clamp(value,0,1);}}

	[Space(10)]

	[SerializeField] private bool overridePitch=false;
	[Range(-3f,3f)]
	[SerializeField] private float pitchOverride=1f;
	/// <summary>
	/// Gets or sets the pitch for override the <see cref="AudioObject"/>default pitch.
	/// </summary>
		public float PitchOverride {get{ return pitchOverride; }set {OverrideValues=true;changingParameters = true;overridePitch = true;pitchOverride = Mathf.Clamp(value,-3,3);}}

	[Space(10)]

	[SerializeField] private bool overrideSpatialMode=false;
	[SerializeField] private bool spatialMode2DOverride=false;
	/// <summary>
	/// Gets or sets the spatial mode for override the <see cref="AudioObject"/>default spatial mode.
	/// </summary>
		public bool SpatialMode2DOverride {get{ return spatialMode2DOverride; }set {OverrideValues=true;changingParameters = true;overrideSpatialMode = true;spatialMode2DOverride =  value;}}

	[Space(10)]

	[SerializeField] private bool overrideStereoPan=false;
	[Range(-1f,1f)]
	[SerializeField] private float stereoPanOverride=0f;
	/// <summary>
	/// Gets or sets the 2D stereo pan for override the <see cref="AudioObject"/>default 2D stereo pan.
	/// </summary>
		public float StereoPanOverride {get{ return stereoPanOverride; }set {OverrideValues=true;changingParameters = true;overrideStereoPan = true;stereoPanOverride =  Mathf.Clamp(value,-1,1);}}

	[Space(10)]

	[SerializeField] private bool overrideSpread=false;
	[Range(0f,1f)]
	[SerializeField] private float spreadOverride=0f;
	/// <summary>
	/// Gets or sets the spread for override the <see cref="AudioObject"/>default spread.
	/// </summary>
		public float SpreadOverride {get{ return spreadOverride; }set {OverrideValues=true;changingParameters = true;overrideSpread = true;spreadOverride =  Mathf.Clamp(value,0,360);}}

	[Space(10)]

	[SerializeField] private bool overrideDopplerLevel=false;
	[Range(0f,5f)]
	[SerializeField] private float dopplerLevelOverride=0.25f;
	/// <summary>
	/// Gets or sets the doppler level for override the <see cref="AudioObject"/>default doppler level.
	/// </summary>
		public float DopplerLevelOverride {get{ return dopplerLevelOverride; }set {OverrideValues=true;changingParameters = true;overrideDopplerLevel = true;dopplerLevelOverride =  Mathf.Clamp(value,0,1);}}

	[Space(10)]

	[SerializeField] private bool overrideReverbZone=false;
	[Range(0f,1.1f)]
	[SerializeField] private float reverbZoneMixOverride = 1;
	/// <summary>
	/// Gets or sets the reverb zone mix for override the <see cref="AudioObject"/>default reverb zone mix.
	/// </summary>
		public float ReverbZoneMixOverride {get{ return reverbZoneMixOverride; }set {OverrideValues=true;changingParameters = true;overrideReverbZone = true;reverbZoneMixOverride =  Mathf.Clamp(value,0,1);}}

	[Space(10)]

	[SerializeField] private bool overrideDistance=false;
	[SerializeField] private float minDistanceOverride=1f;
	/// <summary>
	/// Gets or sets the min hearable distance for override the <see cref="AudioObject"/>default min hearable distance.
	/// </summary>
		public float MinDistanceOverride {get{ return minDistanceOverride; }set {OverrideValues=true;changingParameters = true;overrideDistance = true;minDistanceOverride =  Mathf.Clamp(value,0,Mathf.Infinity);}}
	[SerializeField] private float maxDistanceOverride=20f;
	/// <summary>
	/// Gets or sets the max hearable distance for override the <see cref="AudioObject"/>default max hearable distance.
	/// </summary>
		public float MaxDistanceOverride {get{ return maxDistanceOverride; }set {OverrideValues=true;changingParameters = true;overrideDistance = true;maxDistanceOverride =  Mathf.Clamp(value,0,Mathf.Infinity);}}

	[Space(10)]

	[SerializeField] private bool overrideMixerGroup=false;
	[SerializeField] private AudioMixerGroup mixerGroupOverride;
	/// <summary>
	/// Gets or sets the mixer group for override the <see cref="AudioObject"/>default mixer group.
	/// </summary>
		public AudioMixerGroup MixerGroupOverride {get{ return mixerGroupOverride; }set {OverrideValues=true;changingParameters = true;mixerGroupOverride = value;}}

	[Space(10)]

		[Tooltip("Occludes the sound if there is an Collider between the source and the listener with one of the <b>Multi Listener Pooling Audio System Config</b> occludeCheck layers")]
	[SerializeField] private bool occludeSound=false;
	/// <summary>
	/// Occludes the sound if there is an Collider between the source and the listener.
	/// </summary>
		public bool OccludeSound {get{ return occludeSound; }set {OverrideValues=true;changingParameters = true;occludeSound = value;}}

	[Space(10)]
	[SerializeField] private float delay=0f;
	public bool Delayed{ get { return delayed; } }
	/// <summary>
	/// Gets or sets the delay before the <see cref="MultiAudioSource"/>starts playing the <see cref="AudioObject"/>or Audio Clip.
	/// </summary>
	public float Delay {get{ return delay; }set {delayed = value>0;delay = value;}}
	
	[Space(10)]
	[SerializeField] private MultiAudioManager.UpdateModes delayUpdateMode=MultiAudioManager.UpdateModes.ScaledTime;
	/// <summary>
	/// Gets or sets the delay update mode. If set to ScaledTime, the delay counter will take the TimeScale into account, otherwise it will ignore it.
	/// </summary>
	/// <value>The delay mode.</value>
	public MultiAudioManager.UpdateModes DelayUpdateMode {get{ return delayUpdateMode; }set {changingParameters=true;delayUpdateMode = value;}}
	
	[Space(10)]
	[SerializeField] private bool overrideVolumeRolloff=false;
	[SerializeField] private bool volumeRolloffOverride = true;
	[SerializeField] private AnimationCurve volumeRolloffCurveOverride=AnimationCurve.EaseInOut(0,0,1,1);

	/// <summary>
	/// Gets or sets the <see cref="MultiAudioSource"/>volume rolloff for override the <see cref="AudioObject"/>default volume rolloff.
	/// </summary>
		public bool VolumeRolloffOverride {get{ return volumeRolloffOverride; }set {OverrideValues=true;changingParameters = true;volumeRolloffOverride = value;}}
	/// <summary>
	/// Gets or sets the volume rolloff curve for override the <see cref="AudioObject"/>default volume rolloff curve.
	/// </summary>
		public AnimationCurve VolumeRolloffCurveOverride {get{ return volumeRolloffCurveOverride; }set {OverrideValues=true;changingParameters = true;volumeRolloffCurveOverride = value;}}

	[Space(10)]
	[SerializeField] private bool overrideSpatialize=false;
	[SerializeField] private bool spatializeOverride;

	/// <summary>
	/// Gets or sets the spatialize value for override the <see cref="AudioObject"/>default spatialize value.
	/// </summary>
		public bool SpatializeOverride {get{ return spatializeOverride; }set {OverrideValues=true;changingParameters = true;spatializeOverride = value;}}


	[Space(10)]

	[SerializeField] private bool playOnStart = false;

	[Space(10)]

	[SerializeField] private bool ignoreListenerPause=false;
	/// <summary>
	/// Gets or sets a value indicating whether this <see cref="MultiAudioSource"/>ignores the listener pause.
	/// </summary>
	public bool IgnoreListenerPause{ get { return ignoreListenerPause; } set{changingParameters = true;ignoreListenerPause = value;} }

	[Space(10)]

	[SerializeField] private bool persistsBetweenScenes=false;
	/// <summary>
	/// Gets or sets a value indicating whether this <see cref="MultiAudioSource"/>persists between scenes.
	/// </summary>
	public bool PersistsBetweenScenes{ get { return persistsBetweenScenes; } set{persistsBetweenScenes = value;} }

	[Space(10)]

	[SerializeField] private bool paused=false;
	/// <summary>
	/// Gets or sets a value indicating whether this <see cref="MultiAudioSource"/>is paused.
	/// </summary>
	public bool LocalPause{ get { return paused; } set{paused = value;} }

	[Space(10)]

	private int sessionIdx;
	public int SessionIndex{get{return sessionIdx;} set{sessionIdx = value;}}
	private float volumeMultiplier=0f;
	private float volumeRandomStartMultiplier=1f;
	private float pitchRandomStartMultiplier=1f;
	private float volume;
	private float pitch;
	private float dopplerLevel;
	private float spread;
	private float smoothOcclude;
	private bool spatialMode2D;
	private float stereoPan;
	private float maxDistance;
	private float minDistance;
	/// <summary>
	/// Gets the current max hearable distance of this <see cref="MultiAudioSource"/>.
	/// </summary>
	public float MaxDistance{get{return maxDistance;}}
	/// <summary>
	/// Gets the current min hearable distance of this <see cref="MultiAudioSource"/>.
	/// </summary>
	public float MinDistance{get{return minDistance;}}
	private bool sameClip = false;
	private bool changingParameters=false;
	private bool delayed=false;
	private bool culled=false;
	private bool spatialize;
	private bool isBGM;
	private float fadeOutMultiplier=0f;
	private float fadeOutTime;
	private bool fadeOutDisableObject=false;
	private bool fadeOut=false;
	private float fadeInMultiplier=0f;
	private float fadeInTime;
	private bool fadeIn=false;
	private float stopDelayTime;
	private bool stopDelay;
	private bool stopDelayDisableObject;
	private float stopDelayFadeOutTime;
	private bool stopDelayFadeOut;
	private int loopIndex=0;
	private AudioClip audioClipForOverride;

	private float occludeMultiplier = 0;
	private float occludeFilterValue = 0;

	private float waitForReverb=0f;
	private float updateTime = 0f;
	private bool onFocus = true;
	private bool play=false;
	private bool playing=false;
	private bool globalPaused = false;
	private bool loop=false;
	private float customUnscaledDeltaTime;
	private float pitchTimeScale=1f;
	/// <summary>
	/// Gets a value indicating whether this <see cref="MultiAudioSource"/>is looping.
	/// </summary>
	public bool Loop{get{return loop;}}

		private AudioSource audioSource;
		private AudioLowPassFilter occludeFilter;
	private AudioReverbZone listenerReverb;

	private bool insidePool = false;
	[HideInInspector] public bool InsidePool{get{return insidePool;} set{ insidePool = !cantChangePooleable ? value : insidePool; }}
	private bool cantChangePooleable=false;

	// Use this for initialization
	void Awake () {
	
		thisTransform = transform;

		//Initialize the real audio source
		GameObject audioSourceGo = new GameObject ("Audio Source", typeof(AudioSource));
		audioSourceGo.hideFlags = HideFlags.HideInHierarchy;
		audioSource = audioSourceGo.GetComponent<AudioSource> ();
		audioSourceTransform = audioSourceGo.transform;
		audioSourceTransform.parent = thisTransform;

		if (occludeFilter == null)
			occludeFilter = audioSourceGo.AddComponent<AudioLowPassFilter> ();

		occludeFilter.enabled = false;

		audioSource.playOnAwake = false;
		audioSource.mute = false;
		audioSource.bypassEffects = false;
		audioSource.bypassListenerEffects = false;
		audioSource.bypassReverbZones = false;
		audioSource.reverbZoneMix = 1;
		audioSource.dopplerLevel = 0.25f;
		audioSource.spread = 0;
		audioSource.spatialBlend = 1;
		audioSource.loop = false;
		audioSource.volume = 0;


		listenerReverb = audioSourceGo.AddComponent<AudioReverbZone> ();

		listenerReverb.enabled = false;

		listenerReverb.hideFlags = HideFlags.HideInInspector;

			pitchTimeScale = 1;


	}

	void Start()
	{

		if (InsidePool && transform.parent.gameObject == MultiPoolAudioSystem.audioManager.gameObject) {

			if (!play) {
				gameObject.SetActive (false);

				return;
			}

		}

		if (!insidePool) {
			MultiAudioManager.Instance.AudioSources.Add (this);
		}

		cantChangePooleable = true;

		if (delay > 0)
			delayed = true;

		if (playOnStart && !delayed) {

			Play (channel);

		}

	}

	// Update is called once per frame
	void Update () {

		if (audioObject==null || audioSource==null) {

			if (delayed) {

				if (audioObject == null) {

					if (InsidePool)
						Debug.LogWarning ("<i>Audio Object</i> to play is missing or invalid", MultiPoolAudioSystem.audioManager.gameObject);
					else {
						Debug.LogWarning ("Audio Source: " + "<b>"+audioSource.gameObject.name+"</b>" + " don't have a valid <i>Audio Object</i> to play",audioSource.gameObject);
					}

					Stop ();

					return;

				}

				if (audioObject.RandomClip == null) {

					Debug.LogWarning ("Audio Object: " + "<b>"+audioObject.name+"</b>" + " don't have a valid <i>Audio Clip</i> to play",audioObject);

					Stop ();

					return;

				}

			}

			return;

		}
		
					if (TargetToFollow!=null && TargetToFollow.gameObject.activeInHierarchy && (play || playing)) {

				transform.position = TargetToFollow.position;

			}
		
			if (Time.timeScale > 0) {
				customUnscaledDeltaTime = Time.deltaTime / Time.timeScale;
			}
			else {
				customUnscaledDeltaTime = Time.unscaledDeltaTime;
			}

			pitchTimeScale = playUpdateMode==MultiAudioManager.UpdateModes.ScaledTime?Time.timeScale:1f;

		if (playOnStart && delayed && delay==0) {

			Play (channel);

		}


			if (!ignoreListenerPause && MultiAudioManager.Paused && onFocus || !onFocus)
			globalPaused = true;


			if (Application.runInBackground || Time.realtimeSinceStartup!=updateTime)
				onFocus = true;

			if (onFocus && !MultiAudioManager.Paused && !globalPaused && !play && playing && audioSource != null && (true && !loop && !audioSource.isPlaying || pitch<0 && loop && audioSource.time <= 0.01f || pitch>=0 && loop && audioSource.time >= audioSource.clip.length - 0.01f || true && loop && !audioSource.isPlaying)) {

				if (loop) {
					if (audioClipForOverride == null) {
						Play (channel, TargetToFollow);
					} else {
						PlayOverride (audioClipForOverride, channel, TargetToFollow);
					}
				} else {
					if (waitForReverb<=0f)
					Stop ();
				}

			}


			if (fadeOut && fadeOutMultiplier<=0) {

				fadeOut = false;
				Stop (fadeOutDisableObject);

			}

			if (fadeIn && fadeInMultiplier>=1) {

				fadeInMultiplier = 1;
				fadeIn = false;

			}

			if (stopDelay && stopDelayTime <= 0) {

				stopDelay = false;
				stopDelayTime = 0f;

				if (stopDelayFadeOut) {
					FadeOut (stopDelayFadeOutTime, stopDelayDisableObject);
				} else {
					Stop (stopDelayDisableObject);
				}

			}
				
			if (play || playOnStart) {
				delay = Mathf.Clamp (delay - (delayUpdateMode==MultiAudioManager.UpdateModes.UnscaledTime?customUnscaledDeltaTime:Time.deltaTime), 0, Mathf.Infinity);
			}

			if (!globalPaused && fadeOut && playing && !LocalPause)
			{
				fadeOutMultiplier = Mathf.Clamp (fadeOutMultiplier - (customUnscaledDeltaTime/fadeOutTime), 0, Mathf.Infinity);
			}

			if (!globalPaused && fadeIn && playing && !LocalPause)
			{
				fadeInMultiplier = Mathf.Clamp (fadeInMultiplier + (customUnscaledDeltaTime/fadeInTime), 0, Mathf.Infinity);
			}

			if (!globalPaused && !LocalPause && playing)
				waitForReverb= Mathf.Clamp (waitForReverb - customUnscaledDeltaTime, 0, Mathf.Infinity);

			if (stopDelay && playing) {

				stopDelayTime = stopDelayTime - (delayUpdateMode==MultiAudioManager.UpdateModes.UnscaledTime?customUnscaledDeltaTime:Time.deltaTime);

			}

		audioSource.ignoreListenerPause = ignoreListenerPause;

		thisPosition = thisTransform.position;

			if (secondNearestListenerNULL) {
				secondNearestListenerBlend = 0;
				secondNearestListenerBlendDistance = 0;
			}
			if (thirdNearestListenerNULL) {
				thirdNearestListenerBlend = 0;
				thirdNearestListenerBlendDistance = 0;
			}
			if (fourthNearestListenerNULL) {
				fourthNearestListenerBlend = 0;
				fourthNearestListenerBlendDistance = 0;
			}


		SetParameters (true);

	}

	void LatePlay()
	{

			if (MultiAudioManager.noListeners)
				Debug.LogWarning ("There are no <b>Multi Audio Listeners</b> in the scene. Please ensure there is always at least one audio listener in the scene.", InsidePool?MultiAudioManager.Instance.gameObject:gameObject);

			if (!nearestListenerNULL && nearestListener.ReverbPreset != AudioReverbPreset.Off) {
				waitForReverb = audioSource.clip.length + listenerReverb.decayTime + 0.25f;
				audioSource.bypassReverbZones = false;
			} else {
				audioSource.bypassReverbZones = true;
				listenerReverb.maxDistance = 0;
				listenerReverb.minDistance = 0;
			}

		delay = 0;
		delayed = false;
		play = false;

		if (!sameClip) {
				audioSource.time = 0;
			if (pitch < 0) {
				audioSource.timeSamples = audioSource.clip.samples - 1;
			}
			audioSource.Play ();
		} else {

			if (pitch >= 0)
				audioSource.time = 0;
			else {
				audioSource.timeSamples = audioSource.clip.samples - 1;
			}

			if (!audioSource.isPlaying) {
				audioSource.Play ();
			}

		}

		playing = true;
	}

	void LateUpdate()
	{

				GetNearestListeners ();


				if (!nearestListenerNULL && secondNearestListenerNULL && thirdNearestListenerNULL && fourthNearestListenerNULL) {
					averagePosition = audioSourceTransform.InverseTransformDirection (new Vector3 (nearestListenerPosition.x - thisPosition.x, nearestListenerPosition.y - thisPosition.y, nearestListenerPosition.z - thisPosition.z));
				} else if (!nearestListenerNULL && !secondNearestListenerNULL && thirdNearestListenerNULL && fourthNearestListenerNULL) {

					Vector3 firstLerp = (Vector3.Lerp (Vector3.zero, audioSourceTransform.InverseTransformDirection (nearestListenerPosition - thisPosition), nearestListenerBlendDistance * nearestListenerBlend));
					Vector3 secondLerp = (Vector3.Lerp (Vector3.zero, audioSourceTransform.InverseTransformDirection (secondNearestListenerPosition - thisPosition), secondNearestListenerBlendDistance * secondNearestListenerBlend));

					averagePosition = (firstLerp + secondLerp);
				} else if (!nearestListenerNULL && !secondNearestListenerNULL && !thirdNearestListenerNULL && fourthNearestListenerNULL) {

					Vector3 firstLerp = (Vector3.Lerp (Vector3.zero, audioSourceTransform.InverseTransformDirection (nearestListenerPosition - thisPosition), nearestListenerBlendDistance * nearestListenerBlend));
					Vector3 secondLerp = (Vector3.Lerp (Vector3.zero, audioSourceTransform.InverseTransformDirection (secondNearestListenerPosition - thisPosition), secondNearestListenerBlendDistance * secondNearestListenerBlend));
					Vector3 thirdLerp = (Vector3.Lerp (Vector3.zero, audioSourceTransform.InverseTransformDirection (thirdNearestListenerPosition - thisPosition), thirdNearestListenerBlendDistance * thirdNearestListenerBlend));

					averagePosition = (firstLerp + secondLerp + thirdLerp);
				} else if (!nearestListenerNULL && !secondNearestListenerNULL && !thirdNearestListenerNULL && !fourthNearestListenerNULL) {

					Vector3 firstLerp = (Vector3.Lerp (Vector3.zero, audioSourceTransform.InverseTransformDirection (nearestListenerPosition - thisPosition), nearestListenerBlendDistance * nearestListenerBlend));
					Vector3 secondLerp = (Vector3.Lerp (Vector3.zero, audioSourceTransform.InverseTransformDirection (secondNearestListenerPosition - thisPosition), secondNearestListenerBlendDistance * secondNearestListenerBlend));
					Vector3 thirdLerp = (Vector3.Lerp (Vector3.zero, audioSourceTransform.InverseTransformDirection (thirdNearestListenerPosition - thisPosition), thirdNearestListenerBlendDistance * thirdNearestListenerBlend));
					Vector3 fourthLerp = (Vector3.Lerp (Vector3.zero, audioSourceTransform.InverseTransformDirection (fourthNearestListenerPosition - thisPosition), fourthNearestListenerBlendDistance * fourthNearestListenerBlend));

					averagePosition = (firstLerp + secondLerp + thirdLerp + fourthLerp);
				}

		if (occludeSound && !spatialMode2D) {

				if (Physics.Linecast (thisPosition, nearestListenerPosition, MultiPoolAudioSystem.audioManager.OccludeCheck, QueryTriggerInteraction.Ignore) ) {

					occludeMultiplier = MultiPoolAudioSystem.audioManager.OccludeMultiplier * (spatialMode2D?0:1) * (6 / 5f);

				occludeFilterValue = Mathf.Lerp(22000,2000,occludeMultiplier);

					smoothOcclude = Mathf.Lerp (smoothOcclude, occludeMultiplier, customUnscaledDeltaTime * 15f);

					if (!occludeFilter.isActiveAndEnabled)
						occludeFilter.enabled = true;

					occludeFilter.cutoffFrequency = Mathf.Lerp (occludeFilter.cutoffFrequency, occludeFilterValue, customUnscaledDeltaTime * 15f);

					if (play) {
						smoothOcclude = occludeMultiplier;
					}

				} else {

					smoothOcclude = Mathf.Lerp (smoothOcclude, 0, customUnscaledDeltaTime * 15f);

					occludeFilter.cutoffFrequency = Mathf.Lerp (occludeFilter.cutoffFrequency, 22000, customUnscaledDeltaTime * 15f);

					if (occludeFilter.cutoffFrequency > 21999) {
						occludeFilter.cutoffFrequency = 22000;
						occludeFilter.enabled = false;
					}

					if (play) {
						smoothOcclude = 0;
					}

				}

			} else {
				smoothOcclude = 0;

				if (occludeFilter.isActiveAndEnabled)
					occludeFilter.enabled = false;
			}

			if (OverrideValues && overrideVolumeRolloff && volumeRolloffOverride || (!OverrideValues || !overrideVolumeRolloff) && (playing||play) && audioObject.volumeRolloff) {
			volumeMultiplier = Mathf.Lerp (0, 1, ( OverrideValues && overrideVolumeRolloff?volumeRolloffCurveOverride.Evaluate (nearestListenerBlend):audioObject.volumeRolloffCurve.Evaluate (nearestListenerBlend) ) );

				audioSource.volume = volume * (spatialMode2D?1:volumeMultiplier) * (fadeOut?Mathf.Clamp01(fadeOutMultiplier):1) * (fadeIn?Mathf.Clamp01(fadeInMultiplier):1);

			} else {

				audioSource.volume = volume * (fadeOut?Mathf.Clamp01(fadeOutMultiplier):1) * (fadeIn?Mathf.Clamp01(fadeInMultiplier):1);

			}

			if (occludeSound && !spatialMode2D) {

			audioSource.volume *= 1 - (smoothOcclude);

			}

			smoothedAveragePosition = Vector3.Lerp(smoothedAveragePosition,averagePosition,customUnscaledDeltaTime*25f);


		if (nearestListenerBlend > 0 || spatialMode2D) {

			if (!spatialMode2D) {
				if (!float.IsNaN (MultiPoolAudioSystem.audioManager.AudioListenerPosition.x) && !float.IsNaN (MultiPoolAudioSystem.audioManager.AudioListenerPosition.y) && !float.IsNaN (MultiPoolAudioSystem.audioManager.AudioListenerPosition.z) &&
				    !float.IsNaN (smoothedAveragePosition.x) && !float.IsNaN (smoothedAveragePosition.y) && !float.IsNaN (smoothedAveragePosition.z)) {
					audioSourceTransform.position = new Vector3 (MultiPoolAudioSystem.audioManager.AudioListenerPosition.x - smoothedAveragePosition.x, MultiPoolAudioSystem.audioManager.AudioListenerPosition.y - smoothedAveragePosition.y, MultiPoolAudioSystem.audioManager.AudioListenerPosition.z - smoothedAveragePosition.z);
				}

				if (!nearestListenerNULL && secondNearestListenerNULL && thirdNearestListenerNULL && fourthNearestListenerNULL) {
					if (nearestListenerForward != Vector3.zero)
						audioSourceTransform.forward = (nearestListenerForward);
					else
						nearestListenerForward = Vector3.forward;
				} else if (!nearestListenerNULL && !secondNearestListenerNULL && thirdNearestListenerNULL && fourthNearestListenerNULL) {
					Vector3 forwardToTest = (Vector3.Lerp (Vector3.zero, nearestListenerForward, nearestListenerBlendDistance * nearestListenerBlend) + Vector3.Lerp (Vector3.zero, secondNearestListenerForward, secondNearestListenerBlendDistance * secondNearestListenerBlend)) / 2;

					if (forwardToTest != Vector3.zero)
						audioSourceTransform.forward = forwardToTest;
					else
						nearestListenerForward = Vector3.forward;
				} else if (!nearestListenerNULL && !secondNearestListenerNULL && !thirdNearestListenerNULL && fourthNearestListenerNULL) {
					Vector3 forwardToTest = (Vector3.Lerp (Vector3.zero, nearestListenerForward, nearestListenerBlendDistance * nearestListenerBlend) + Vector3.Lerp (Vector3.zero, secondNearestListenerForward, secondNearestListenerBlendDistance * secondNearestListenerBlend) + Vector3.Lerp (Vector3.zero, thirdNearestListenerForward, thirdNearestListenerBlendDistance * thirdNearestListenerBlend)) / 3;
					if (forwardToTest != Vector3.zero)
						audioSourceTransform.forward = forwardToTest;
					else
						nearestListenerForward = Vector3.forward;
				} else if (!nearestListenerNULL && !secondNearestListenerNULL && !thirdNearestListenerNULL && !fourthNearestListenerNULL) {
					Vector3 forwardToTest = (Vector3.Lerp (Vector3.zero, nearestListenerForward, nearestListenerBlendDistance * nearestListenerBlend) + Vector3.Lerp (Vector3.zero, secondNearestListenerForward, secondNearestListenerBlendDistance * secondNearestListenerBlend) + Vector3.Lerp (Vector3.zero, thirdNearestListenerForward, thirdNearestListenerBlendDistance * thirdNearestListenerBlend) + Vector3.Lerp (Vector3.zero, fourthNearestListenerForward, fourthNearestListenerBlendDistance * fourthNearestListenerBlend)) / 4;
					if (forwardToTest != Vector3.zero)
						audioSourceTransform.forward = forwardToTest;
					else
						nearestListenerForward = Vector3.forward;
				}
			} else {

				audioSourceTransform.position = MultiPoolAudioSystem.audioManager.AudioListenerPosition;

			}

				audioSource.maxDistance = 1000000;
				audioSource.minDistance = 1000000 - 1;

				bool enableReverb = (waitForReverb>0 || loop ) && nearestListener.ReverbPreset!=AudioReverbPreset.Off;

				if (enableReverb) {

					audioSource.bypassReverbZones = false;
					listenerReverb.enabled = true;
					listenerReverb.reverbPreset = nearestListener.ReverbPreset;
					listenerReverb.maxDistance = Mathf.Lerp(listenerReverb.maxDistance,audioSource.maxDistance,customUnscaledDeltaTime*15f);
					listenerReverb.minDistance = Mathf.Lerp(listenerReverb.minDistance,audioSource.maxDistance-10,customUnscaledDeltaTime*15f);

				} else {
					
					listenerReverb.reverbPreset = AudioReverbPreset.Off;

					listenerReverb.maxDistance = Mathf.Lerp(listenerReverb.maxDistance,0,customUnscaledDeltaTime*15f);
					listenerReverb.minDistance = Mathf.Lerp(listenerReverb.minDistance,0,customUnscaledDeltaTime*15f);

				}

				if (culled) {
					culled = false;
				}
			} else {

				if (!culled) {
					audioSourceTransform.localPosition = Vector3.zero;
					culled = true;
				}

				audioSource.maxDistance = maxDistance;
				audioSource.minDistance = minDistance;

			}
			
			if (!culled) {
				if (!LocalPause) {
					audioSource.pitch = Mathf.Clamp ((!nearestListenerNULL ? pitch : 1)*pitchTimeScale, -3, 3);
				} else {
					audioSource.pitch = 0;
				}
				audioSource.panStereo = stereoPan;
				audioSource.spatialBlend = spatialMode2D || nearestListenerDistance<0.01f? 0 : 1;
				audioSource.dopplerLevel = dopplerLevel;
				audioSource.spread = spread;

				if (occludeSound) {
					audioSource.spread += 50 * smoothOcclude;
				}
				audioSource.spatialize = spatialize;
				#if UNITY_5_5_OR_NEWER
				audioSource.spatializePostEffects = false;
				#endif
				audioSource.mute = Mute;
			} else {

				if (LocalPause)
				audioSource.pitch = 0;

			}


		if (play && !changingParameters && (!delayed && true || delayed && delay == 0)) {


			LatePlay ();

		}

			if (globalPaused && !MultiAudioManager.Paused && onFocus || globalPaused && ignoreListenerPause && onFocus)
			globalPaused = false;

		if (changingParameters) {

			SetParameters ();

			changingParameters = false;
			return;

		}

		updateTime = Time.realtimeSinceStartup;

	}

	void SetParameters(bool onUpdate=false)
	{
		if (!onUpdate) {
			volumeRandomStartMultiplier = Random.Range (audioObject.minVolumeMultiplier, audioObject.maxVolumeMultiplier);
			pitchRandomStartMultiplier = Random.Range (audioObject.minPitchMultiplier, audioObject.maxPitchMultiplier);
		}


			if (OverrideValues && overrideReverbZone) {
				audioSource.reverbZoneMix = reverbZoneMixOverride;
			} else {
				audioSource.reverbZoneMix = audioObject.reverbZoneMix;
			}

		if (!OverrideValues || !overrideSpread) {
			spread = audioObject.spread;
		} else {
			spread = spreadOverride;
		}
		if (!OverrideValues || !overrideVolume) {
				volume = Mathf.Clamp01 (audioObject.volume * (OverrideValues && overrideRandomVolumeMultiplier && !randomVolumeMultiplier?1:volumeRandomStartMultiplier) );
		} else {
				volume = Mathf.Clamp01 (volumeOverride * (OverrideValues && overrideRandomVolumeMultiplier && !randomVolumeMultiplier?1:volumeRandomStartMultiplier) );
		}
		if (!OverrideValues || !overridePitch) {
				pitch = Mathf.Clamp (audioObject.pitch * (OverrideValues && overrideRandomPitchMultiplier && !randomPitchMultiplier?1:pitchRandomStartMultiplier) , -3f, 3f);
		} else {
				pitch = Mathf.Clamp (pitchOverride * (OverrideValues && overrideRandomPitchMultiplier && !randomPitchMultiplier?1:pitchRandomStartMultiplier), -3f, 3f);
		}

		if (!OverrideValues || !overrideDopplerLevel) {
			dopplerLevel = audioObject.dopplerLevel;
		} else {
			dopplerLevel = dopplerLevelOverride;
		}

		if (!OverrideValues || !overrideSpatialMode) {
			spatialMode2D = audioObject.spatialMode2D;
		} else {
			spatialMode2D = spatialMode2DOverride;
		}

		if (!OverrideValues || !overrideSpatialize) {
			spatialize = audioObject.spatialize;
		} else {
			spatialize = spatializeOverride;
		}

		if (!OverrideValues || !overrideStereoPan) {
			stereoPan = Mathf.Clamp (audioObject.stereoPan, -1, 1);
		} else {
			stereoPan = Mathf.Clamp (stereoPanOverride, -1, 1);
		}

		if (OverrideValues && overrideMixerGroup && mixerGroupOverride!=null)
			audioSource.outputAudioMixerGroup = mixerGroupOverride;
		else {

			if (audioObject.mixerGroup != null) {
				audioSource.outputAudioMixerGroup = audioObject.mixerGroup;
			} else {
				if (audioObject.isBGM && audioSource.outputAudioMixerGroup!=MultiPoolAudioSystem.audioManager.BgmMixerGroup || !audioObject.isBGM && audioSource.outputAudioMixerGroup!=MultiPoolAudioSystem.audioManager.SfxMixerGroup)
					audioSource.outputAudioMixerGroup = audioObject.isBGM?MultiPoolAudioSystem.audioManager.BgmMixerGroup:MultiPoolAudioSystem.audioManager.SfxMixerGroup;
			}

		}

		if (!OverrideValues || !overrideDistance) {
			maxDistance = audioObject.maxDistance;
			minDistance = audioObject.minDistance;
		} else {
			maxDistance = maxDistanceOverride;
			minDistance = minDistanceOverride;
		}

	}

		void RealPlay(float _delay=0,int _channel=-1,Transform _targetToFollow=null,float _fadeInTime=0f)
	{

		if (audioObject == null) {

			if (InsidePool)
				Debug.LogWarning ("<i>Audio Object</i> to play is missing or invalid", MultiPoolAudioSystem.audioManager.gameObject);
			else {
				Debug.LogWarning ("Audio Source: " + "<b>"+audioSource.gameObject.name+"</b>" + " don't have a valid <i>Audio Object</i> to play",audioSource.gameObject);
			}

			Stop ();

			return;

		}

			if (InsidePool) {
				persistsBetweenScenes = false;
				ignoreListenerPause = false;
				Mute = false;
				TargetToFollow = null;
				overrideAudioClip = false;
				overrideMixerGroup = false;
				overrideDistance = false;
				overrideDopplerLevel = false;
				overridePitch = false;
				overrideRandomPitchMultiplier = false;
				overrideRandomVolumeMultiplier = false;
				overrideReverbZone = false;
				overrideSpatialize = false;
				overrideSpatialMode = false;
				overrideSpread = false;
				overrideStereoPan = false;
				OverrideValues = false;
				overrideVolume = false;
				overrideVolumeRolloff = false;
			}
		delay = _delay;
		delayed = delay > 0;

		AudioClip _clip = audioObject.RandomClip;

		if (_clip!=null && audioObject.loop && audioObject.loopClipsSequentially) {

			if (loopIndex >= audioObject.clips.Length)
				loopIndex = 0;

			_clip = audioObject.clips [loopIndex];
			loopIndex += 1;

		}



		if (overrideAudioClip && audioClipOverride != null)
			_clip = audioClipOverride;

		if (_clip == null) {

			Debug.LogWarning ("Audio Object: " + "<b>"+audioObject.name+"</b>" + " don't have a valid <i>Audio Clip</i> to play",audioObject);

			Stop ();

			return;

		}

		sameClip = audioSource.clip == _clip;

		channel = _channel;

		if (!playOnStart) {
			TargetToFollow = _targetToFollow;
		}

		audioSource.clip = _clip;

		SetParameters ();


		if (TargetToFollow!=null) {

			transform.position = TargetToFollow.position;

		}

		loop = audioObject.loop;

			if (_fadeInTime > 0) {

				fadeOut = false;
				fadeIn = true;
				fadeInMultiplier = 0f;
				fadeInTime = Mathf.Clamp (_fadeInTime, 0.1f, Mathf.Infinity);

			}


		play = true;

	}

	void RealPlayOverride(AudioClip _audioClipOverride ,float _delay=0,int _channel=-1,Transform _targetToFollow=null,float _fadeInTime=0f)
	{

		if (audioObject == null) {

			if (InsidePool)
				Debug.LogWarning ("<i>Audio Object</i> to play is missing or invalid", MultiPoolAudioSystem.audioManager.gameObject);
			else {
				Debug.LogWarning ("Audio Source: " + "<b>"+audioSource.gameObject.name+"</b>" + " don't have a valid <i>Audio Object</i> to play",audioSource.gameObject);
			}

			Stop ();

			return;

		}

			if (InsidePool) {
				persistsBetweenScenes = false;
				ignoreListenerPause = false;
				Mute = false;
				TargetToFollow = null;
				overrideAudioClip = false;
				overrideMixerGroup = false;
				overrideDistance = false;
				overrideDopplerLevel = false;
				overridePitch = false;
				overrideRandomPitchMultiplier = false;
				overrideRandomVolumeMultiplier = false;
				overrideReverbZone = false;
				overrideSpatialize = false;
				overrideSpatialMode = false;
				overrideSpread = false;
				overrideStereoPan = false;
				OverrideValues = false;
				overrideVolume = false;
				overrideVolumeRolloff = false;
			}
		delay = _delay;
		delayed = delay > 0;

		AudioClip _clip = _audioClipOverride;
		audioClipForOverride = _clip;

		if (_clip == null) {

			Debug.LogWarning ("Audio Object: " + "<b>"+audioObject.name+"</b>" + " don't have a valid <i>Audio Clip</i> to play",audioObject);

			Stop ();

			return;

		}

		sameClip = audioSource.clip == _clip;

		channel = _channel;

		if (!playOnStart) {
			TargetToFollow = _targetToFollow;
		}

		audioSource.clip = _clip;


		SetParameters ();


		if (TargetToFollow!=null) {

			transform.position = TargetToFollow.position;

		}

		loop = audioObject.loop;

		if (_fadeInTime > 0) {

			fadeOut = false;
			fadeIn = true;
			fadeInMultiplier = 0f;
			fadeInTime = Mathf.Clamp (_fadeInTime, 0.1f, Mathf.Infinity);

		}

		play = true;

	}

	#region No Delay Methods
	/// <summary>
	/// Plays the <see cref="MultiAudioSource"/>.
	/// </summary>
	public void Play()
	{

		RealPlay (0f,-1, null);

	}
	/// <summary>
	/// Plays the <see cref="MultiAudioSource"/>at the specified Channel.
	/// </summary>
	/// <param name="_channel">Channel.</param>
	public void Play(int _channel)
	{

		RealPlay (0f,_channel, null);

	}
	/// <summary>
	/// Plays the <see cref="MultiAudioSource"/>and makes that follow a target.
	/// </summary>
	/// <param name="_targetToFollow">Target to follow.</param>
	public void Play(Transform _targetToFollow)
	{

		RealPlay (0f,-1, _targetToFollow);

	}
	/// <summary>
	/// Plays the <see cref="MultiAudioSource"/>at the specified Channel and makes that follow a target.
	/// </summary>
	/// <param name="_channel">Channel.</param>
	/// <param name="_targetToFollow">Target to follow.</param>
	public void Play(int _channel,Transform _targetToFollow)
	{

		RealPlay (0f,_channel, _targetToFollow);

	}
	#endregion

	#region Delay Methods
	/// <summary>
	/// Plays the <see cref="MultiAudioSource"/>with a delay specified in seconds.
	/// </summary>
	/// <param name="_delay">Delay.</param>
	public void Play(float _delay)
	{

		RealPlay (_delay,-1, null);

	}
	/// <summary>
	/// Plays the <see cref="MultiAudioSource"/>with a delay specified in seconds at the specified Channel.
	/// </summary>
	/// <param name="_channel">Channel.</param>
	/// <param name="_delay">Delay.</param>
	public void Play(int _channel,float _delay)
	{

		RealPlay (_delay,_channel, null);

	}
	/// <summary>
	/// Plays the <see cref="MultiAudioSource"/>with a delay specified in seconds and makes that follow a target.
	/// </summary>
	/// <param name="_targetToFollow">Target to follow.</param>
	/// <param name="_delay">Delay.</param>
		public void Play(Transform _targetToFollow,float _delay)
	{

		RealPlay (_delay,-1, _targetToFollow);

	}
	/// <summary>
	/// Play the <see cref="MultiAudioSource"/>with a delay specified in seconds at the specified Channel and makes that follow a target.
	/// </summary>
	/// <param name="_channel">Channel.</param>
	/// <param name="_targetToFollow">Target to follow.</param>
	/// <param name="_delay">Delay.</param>
		public void Play(int _channel,Transform _targetToFollow,float _delay)
	{

		RealPlay (_delay,_channel, _targetToFollow);

	}

	// No Delay Override Methods
	/// <summary>
	/// Plays the <see cref="MultiAudioSource"/>using its <see cref="AudioObject"/>but with another Audio Clip.
	/// </summary>
	/// <param name="audioClipOverride">Audio clip override.</param>
	public void PlayOverride(AudioClip audioClipOverride)
	{

		RealPlayOverride (audioClipOverride,0f,-1, null);

	}
	/// <summary>
	/// Plays the <see cref="MultiAudioSource"/>using its <see cref="AudioObject"/>but with another Audio Clip at the specified Channel.
	/// </summary>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="_channel">Channel.</param>
	public void PlayOverride(AudioClip audioClipOverride, int _channel)
	{

		RealPlayOverride (audioClipOverride,0f,_channel, null);

	}
	/// <summary>
	/// Plays the <see cref="MultiAudioSource"/>using its <see cref="AudioObject"/>but with another Audio Clip and makes that follow a target.
	/// </summary>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="_targetToFollow">Target to follow.</param>
	public void PlayOverride(AudioClip audioClipOverride, Transform _targetToFollow)
	{

		RealPlayOverride (audioClipOverride,0f,-1, _targetToFollow);

	}
	/// <summary>
	/// Plays the <see cref="MultiAudioSource"/>using its <see cref="AudioObject"/>but with another Audio Clip at the specified Channel and makes that follow a target.
	/// </summary>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="_channel">Channel.</param>
	/// <param name="_targetToFollow">Target to follow.</param>
	public void PlayOverride(AudioClip audioClipOverride, int _channel,Transform _targetToFollow)
	{

		RealPlayOverride (audioClipOverride,0f,_channel, _targetToFollow);

	}

	//Delay Override Methods
	/// <summary>
	/// Plays the <see cref="MultiAudioSource"/>using its <see cref="AudioObject"/>but with another Audio Clip with a delay specified in seconds.
	/// </summary>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="_delay">Delay.</param>
	public void PlayOverride(AudioClip audioClipOverride, float _delay)
	{

		RealPlayOverride (audioClipOverride,_delay,-1, null);

	}
	/// <summary>
	/// Plays the <see cref="MultiAudioSource"/>using its <see cref="AudioObject"/>but with another Audio Clip with a delay specified in seconds at the specified Channel.
	/// </summary>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="_channel">Channel.</param>
	/// <param name="_delay">Delay.</param>
	public void PlayOverride(AudioClip audioClipOverride, int _channel,float _delay)
	{

		RealPlayOverride (audioClipOverride,_delay,_channel, null);

	}
	/// <summary>
	/// Plays the <see cref="MultiAudioSource"/>using its <see cref="AudioObject"/>but with another Audio Clip with a delay specified in seconds and makes that follow a target.
	/// </summary>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="_targetToFollow">Target to follow.</param>
	/// <param name="_delay">Delay.</param>
	public void PlayOverride(AudioClip audioClipOverride, Transform _targetToFollow,float _delay)
	{

		RealPlayOverride (audioClipOverride,_delay,-1, _targetToFollow);

	}

	/// <summary>
	/// Plays the <see cref="MultiAudioSource"/>using its <see cref="AudioObject"/>but with another Audio Clip with a delay specified in seconds at the specified Channel and makes that follow a target.
	/// </summary>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="_channel">Channel.</param>
	/// <param name="_targetToFollow">Target to follow.</param>
	/// <param name="_delay">Delay.</param>
	public void PlayOverride(AudioClip audioClipOverride, int _channel,Transform _targetToFollow,float _delay)
	{

		RealPlayOverride (audioClipOverride,_delay,_channel, _targetToFollow);

	}
	#endregion

	#region No Delay Fade In Methods
		/// <summary>
		/// Plays the <see cref="MultiAudioSource"/>.
		/// </summary>
		/// <param name="fadeInTime">Fade In Time.</param>
		public void PlayFadeIn(float fadeInTime)
		{

			RealPlay (0f,-1, null,fadeInTime);

		}
		/// <summary>
		/// Plays the <see cref="MultiAudioSource"/>at the specified Channel.
		/// </summary>
		/// <param name="fadeInTime">Fade In Time.</param>
		/// <param name="_channel">Channel.</param>
		public void PlayFadeIn(float fadeInTime,int _channel)
		{

			RealPlay (0f,_channel, null,fadeInTime);

		}
		/// <summary>
		/// Plays the <see cref="MultiAudioSource"/>and makes that follow a target.
		/// </summary>
		/// <param name="fadeInTime">Fade In Time.</param>
		/// <param name="_targetToFollow">Target to follow.</param>
		public void PlayFadeIn(float fadeInTime,Transform _targetToFollow)
		{

			RealPlay (0f,-1, _targetToFollow,fadeInTime);

		}
		/// <summary>
		/// Plays the <see cref="MultiAudioSource"/>at the specified Channel and makes that follow a target.
		/// </summary>
		/// <param name="fadeInTime">Fade In Time.</param>
		/// <param name="_channel">Channel.</param>
		/// <param name="_targetToFollow">Target to follow.</param>
		public void PlayFadeIn(float fadeInTime,int _channel,Transform _targetToFollow)
		{

			RealPlay (0f,_channel, _targetToFollow,fadeInTime);

		}

		//Delay Fade In Methods
		/// <summary>
		/// Plays the <see cref="MultiAudioSource"/>with a delay specified in seconds.
		/// </summary>
		/// <param name="fadeInTime">Fade In Time.</param>
		/// <param name="_delay">Delay.</param>
		public void PlayFadeIn(float fadeInTime,float _delay)
		{

			RealPlay (_delay,-1, null,fadeInTime);

		}
		/// <summary>
		/// Plays the <see cref="MultiAudioSource"/>with a delay specified in seconds at the specified Channel.
		/// </summary>
		/// <param name="fadeInTime">Fade In Time.</param>
		/// <param name="_channel">Channel.</param>
		/// <param name="_delay">Delay.</param>
		public void PlayFadeIn(float fadeInTime,int _channel,float _delay)
		{

			RealPlay (_delay,_channel, null,fadeInTime);

		}
		/// <summary>
		/// Plays the <see cref="MultiAudioSource"/>with a delay specified in seconds and makes that follow a target.
		/// </summary>
		/// <param name="fadeInTime">Fade In Time.</param>
		/// <param name="_targetToFollow">Target to follow.</param>
		/// <param name="_delay">Delay.</param>
		public void PlayFadeIn(float fadeInTime,Transform _targetToFollow,float _delay)
		{

			RealPlay (_delay,-1, _targetToFollow,fadeInTime);

		}
		/// <summary>
		/// Play the <see cref="MultiAudioSource"/>with a delay specified in seconds at the specified Channel and makes that follow a target.
		/// </summary>
		/// <param name="fadeInTime">Fade In Time.</param>
		/// <param name="_channel">Channel.</param>
		/// <param name="_targetToFollow">Target to follow.</param>
		/// <param name="_delay">Delay.</param>
		public void PlayFadeIn(float fadeInTime,int _channel,Transform _targetToFollow,float _delay)
		{

			RealPlay (_delay,_channel, _targetToFollow,fadeInTime);

		}

	#endregion

	#region No Delay Override Methods
		/// <summary>
		/// Plays the <see cref="MultiAudioSource"/>using its <see cref="AudioObject"/>but with another Audio Clip.
		/// </summary>
		/// <param name="fadeInTime">Fade In Time.</param>
		/// <param name="audioClipOverride">Audio clip override.</param>
		public void PlayFadeInOverride(float fadeInTime,AudioClip audioClipOverride)
		{

			RealPlayOverride (audioClipOverride,0f,-1, null,fadeInTime);

		}
		/// <summary>
		/// Plays the <see cref="MultiAudioSource"/>using its <see cref="AudioObject"/>but with another Audio Clip at the specified Channel.
		/// </summary>
		/// <param name="fadeInTime">Fade In Time.</param>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="_channel">Channel.</param>
		public void PlayFadeInOverride(float fadeInTime,AudioClip audioClipOverride, int _channel)
		{

			RealPlayOverride (audioClipOverride,0f,_channel, null,fadeInTime);

		}
		/// <summary>
		/// Plays the <see cref="MultiAudioSource"/>using its <see cref="AudioObject"/>but with another Audio Clip and makes that follow a target.
		/// </summary>
		/// <param name="fadeInTime">Fade In Time.</param>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="_targetToFollow">Target to follow.</param>
		public void PlayFadeInOverride(float fadeInTime,AudioClip audioClipOverride, Transform _targetToFollow)
		{

			RealPlayOverride (audioClipOverride,0f,-1, _targetToFollow,fadeInTime);

		}
		/// <summary>
		/// Plays the <see cref="MultiAudioSource"/>using its <see cref="AudioObject"/>but with another Audio Clip at the specified Channel and makes that follow a target.
		/// </summary>
		/// <param name="fadeInTime">Fade In Time.</param>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="_channel">Channel.</param>
		/// <param name="_targetToFollow">Target to follow.</param>
		public void PlayFadeInOverride(float fadeInTime,AudioClip audioClipOverride, int _channel,Transform _targetToFollow)
		{

			RealPlayOverride (audioClipOverride,0f,_channel, _targetToFollow,fadeInTime);

		}

		//Delay Override Methods
		/// <summary>
		/// Plays the <see cref="MultiAudioSource"/>using its <see cref="AudioObject"/>but with another Audio Clip with a delay specified in seconds.
		/// </summary>
		/// <param name="fadeInTime">Fade In Time.</param>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="_delay">Delay.</param>
		public void PlayFadeInOverride(float fadeInTime,AudioClip audioClipOverride, float _delay)
		{

			RealPlayOverride (audioClipOverride,_delay,-1, null,fadeInTime);

		}
		/// <summary>
		/// Plays the <see cref="MultiAudioSource"/>using its <see cref="AudioObject"/>but with another Audio Clip with a delay specified in seconds at the specified Channel.
		/// </summary>
		/// <param name="fadeInTime">Fade In Time.</param>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="_channel">Channel.</param>
		/// <param name="_delay">Delay.</param>
		public void PlayFadeInOverride(float fadeInTime,AudioClip audioClipOverride, int _channel,float _delay)
		{

			RealPlayOverride (audioClipOverride,_delay,_channel, null,fadeInTime);

		}
		/// <summary>
		/// Plays the <see cref="MultiAudioSource"/>using its <see cref="AudioObject"/>but with another Audio Clip with a delay specified in seconds and makes that follow a target.
		/// </summary>
		/// <param name="fadeInTime">Fade In Time.</param>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="_targetToFollow">Target to follow.</param>
		/// <param name="_delay">Delay.</param>
		public void PlayFadeInOverride(float fadeInTime,AudioClip audioClipOverride, Transform _targetToFollow,float _delay)
		{

			RealPlayOverride (audioClipOverride,_delay,-1, _targetToFollow,fadeInTime);

		}
		/// <summary>
		/// Plays the <see cref="MultiAudioSource"/>using its <see cref="AudioObject"/>but with another Audio Clip with a delay specified in seconds at the specified Channel and makes that follow a target.
		/// </summary>
		/// <param name="fadeInTime">Fade In Time.</param>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="_channel">Channel.</param>
		/// <param name="_targetToFollow">Target to follow.</param>
		/// <param name="_delay">Delay.</param>
		public void PlayFadeInOverride(float fadeInTime,AudioClip audioClipOverride, int _channel,Transform _targetToFollow,float _delay)
		{

			RealPlayOverride (audioClipOverride,_delay,_channel, _targetToFollow,fadeInTime);

		}

	#endregion
	
	/// <summary>
	/// Stops playing the <see cref="MultiAudioSource"/>.
	/// </summary>
	/// <param name="disableObject">If set to <c>true</c> disable object.</param>
	public void Stop(bool disableObject=true)
	{

		audioSource.Stop ();
			if (InsidePool) {
				persistsBetweenScenes = false;
				ignoreListenerPause = false;
				Mute = false;
				TargetToFollow = null;
				overrideAudioClip = false;
				overrideMixerGroup = false;
				overrideDistance = false;
				overrideDopplerLevel = false;
				overridePitch = false;
				overrideRandomPitchMultiplier = false;
				overrideRandomVolumeMultiplier = false;
				overrideReverbZone = false;
				overrideSpatialize = false;
				overrideSpatialMode = false;
				overrideSpread = false;
				overrideStereoPan = false;
				OverrideValues = false;
				overrideVolume = false;
				overrideVolumeRolloff = false;
			}
		loop = false;
		play = false;
		playing = false;
		delay = 0;
		delayed = false;
		stopDelay = false;
		fadeIn = false;
		fadeOut = false;
		onFocus = true;
		if (audioClipForOverride!=null)
		audioClipForOverride = null;
		loopIndex = 0;

		if (InsidePool) {
			overrideVolume = false;
			overridePitch = false;
			overrideSpread = false;
			overrideReverbZone = false;
			mixerGroupOverride = null;
			overrideDistance = false;
			occludeSound = false;

			gameObject.SetActive (false);

		} else {

			if (disableObject) {
				gameObject.SetActive (false);
			}

		}

	}

	/// <summary>
	/// Stops playing the <see cref="MultiAudioSource"/>using a fade out.
	/// </summary>
	/// <param name="_fadeOutTime">Fade out time.</param>
	/// <param name="disableObject">If set to <c>true</c> disable object.</param>
	public void FadeOut(float _fadeOutTime=1f,bool disableObject=true)
	{
			channel = -1;
		if (!fadeOut) {
			fadeOutMultiplier = 1;
			fadeOutTime = Mathf.Clamp(_fadeOutTime,0.1f,Mathf.Infinity);
			fadeOut = true;
			fadeOutDisableObject = disableObject;
		}

	}

	/// <summary>
	/// Stops playing the <see cref="MultiAudioSource"/>with a delay specified in seconds.
	/// </summary>
	/// <param name="_delay">Delay time for Stop.</param>
	/// <param name="disableObject">If set to <c>true</c> disable object.</param>
	public void StopDelayed(float _delay,bool disableObject=true)
	{

		if (_delay>0) {
			stopDelayTime = Mathf.Clamp(_delay,0.1f,Mathf.Infinity);
			stopDelayFadeOut = false;
			stopDelay = true;
			stopDelayDisableObject = disableObject;
		}

	}

	/// <summary>
	/// Stops playing the <see cref="MultiAudioSource"/>with a delay specified in seconds using a fade out.
	/// </summary>
	/// <param name="_delay">Delay time for Stop.</param>
	/// <param name="_fadeOutTime">Fade out time.</param>
	/// <param name="disableObject">If set to <c>true</c> disable object.</param>
	public void FadeOutDelayed(float _delay,float _fadeOutTime=1f,bool disableObject=true)
	{

		if (_delay>0) {
			stopDelayTime = Mathf.Clamp(_delay,0.1f,Mathf.Infinity);
			stopDelay = true;
			stopDelayDisableObject = disableObject;
			stopDelayFadeOut = true;
			stopDelayFadeOutTime = _fadeOutTime;
		}

	}

	/// <summary>
	/// Interrupts the delayed stop.
	/// </summary>
	public void InterruptDelayedStop()
	{

		stopDelay = false;

	}

	private bool isApplicationQuitting = false;
	void OnApplicationQuit () {
		isApplicationQuitting = true;
	}

	void OnDestroy()
	{

		if (isApplicationQuitting)
			return;

		if (MultiPoolAudioSystem.audioManager!=null && MultiPoolAudioSystem.audioManager.AudioSources!=null)
		MultiPoolAudioSystem.audioManager.AudioSources.Remove (this);

	}

	// Get the Nearest Enabled Multi Audio Listener
	void GetNearestListeners()
	{

		Vector3 closestTargetForward = Vector3.forward;
		Vector3 closestPosition = Vector3.zero;
		float closestDistanceSqr = Mathf.Infinity;
		Vector3 tempTransform = Vector3.forward;
		Vector3 tempPosition = Vector3.zero;

		nearestListenerNULL = true;
		secondNearestListenerNULL = true;
		thirdNearestListenerNULL = true;
		fourthNearestListenerNULL = true;

		nearestListenerPosition = Vector3.zero;
		secondNearestListenerPosition = Vector3.zero;
		thirdNearestListenerPosition = Vector3.zero;
		fourthNearestListenerPosition = Vector3.zero;

		nearestListenerForward = Vector3.forward;
		secondNearestListenerForward = Vector3.forward;
		thirdNearestListenerForward = Vector3.forward;
		fourthNearestListenerForward = Vector3.forward;

		int firstFinded = -1;
		int secondFinded = -1;
		int thirdFinded = -1;

		int maxIndex = MultiPoolAudioSystem.audioManager.listenersForwards.Count;

		for (int i = 0; i < maxIndex; i++) {

			tempTransform = MultiPoolAudioSystem.audioManager.listenersForwards [i];
			tempPosition = MultiPoolAudioSystem.audioManager.listenersPositions [i];


			Vector3 directionToTarget = new Vector3 (tempPosition.x - thisPosition.x, tempPosition.y - thisPosition.y, tempPosition.z - thisPosition.z);
			float dSqrToTarget = directionToTarget.sqrMagnitude;

			if (dSqrToTarget < closestDistanceSqr) {

				firstFinded = i;

				closestTargetForward = tempTransform;

				closestPosition = tempPosition;

				nearestListenerNULL = false;

				nearestListener = MultiPoolAudioSystem.audioManager.listenersComponents [i];

				nearestListenerPosition = closestPosition;

				nearestListenerForward = closestTargetForward;

				closestDistanceSqr = dSqrToTarget;


			}

		}
			
		if (!nearestListenerNULL) {
			Vector3 directionToNearestListener = new Vector3 (nearestListenerPosition.x - thisPosition.x, nearestListenerPosition.y - thisPosition.y, nearestListenerPosition.z - thisPosition.z);
			float distance = directionToNearestListener.magnitude;

			nearestListenerDistance = distance;
			nearestListenerBlend = Mathf.Clamp01 (1 - (nearestListenerDistance - minDistance) / (maxDistance - minDistance));

			if (spatialMode2D)
				nearestListenerBlend = 1;
		}

		bool notCulled = !nearestListenerNULL && nearestListenerBlend>0;

		if (notCulled)
		{

			closestDistanceSqr = Mathf.Infinity;

			for (int i = 0; i < maxIndex; i++) {

				if (i != firstFinded) {

					tempTransform = MultiPoolAudioSystem.audioManager.listenersForwards [i];
					tempPosition = MultiPoolAudioSystem.audioManager.listenersPositions [i];

					Vector3 directionToTarget = new Vector3 (tempPosition.x - thisPosition.x, tempPosition.y - thisPosition.y, tempPosition.z - thisPosition.z);
					float dSqrToTarget = directionToTarget.sqrMagnitude;

					if (dSqrToTarget < closestDistanceSqr) {

						secondFinded = i;

						closestTargetForward = tempTransform;

						closestPosition = tempPosition;

						secondNearestListenerNULL = false;

						secondNearestListenerPosition = closestPosition;

						secondNearestListenerForward = closestTargetForward;

						closestDistanceSqr = dSqrToTarget;

					}
				}

			}

			closestDistanceSqr = Mathf.Infinity;

			for (int i = 0; i < maxIndex; i++) {

				if (i != firstFinded && i != secondFinded) {

					tempTransform = MultiPoolAudioSystem.audioManager.listenersForwards [i];
					tempPosition = MultiPoolAudioSystem.audioManager.listenersPositions [i];

					Vector3 directionToTarget = new Vector3 (tempPosition.x - thisPosition.x, tempPosition.y - thisPosition.y, tempPosition.z - thisPosition.z);
					float dSqrToTarget = directionToTarget.sqrMagnitude;

					if (dSqrToTarget < closestDistanceSqr) {

						thirdFinded = i;

						closestTargetForward = tempTransform;

						closestPosition = tempPosition;

						thirdNearestListenerNULL = false;

						thirdNearestListenerPosition = closestPosition;

						thirdNearestListenerForward = closestTargetForward;

						closestDistanceSqr = dSqrToTarget;

					}
				}

			}

			closestDistanceSqr = Mathf.Infinity;

			for (int i = 0; i < maxIndex; i++) {

				if (i != firstFinded && i != secondFinded && i != thirdFinded) {

					tempTransform = MultiPoolAudioSystem.audioManager.listenersForwards [i];
					tempPosition = MultiPoolAudioSystem.audioManager.listenersPositions [i];

					Vector3 directionToTarget = new Vector3 (tempPosition.x - thisPosition.x, tempPosition.y - thisPosition.y, tempPosition.z - thisPosition.z);
					float dSqrToTarget = directionToTarget.sqrMagnitude;

					if (dSqrToTarget < closestDistanceSqr) {

						closestTargetForward = tempTransform;
						closestPosition = tempPosition;

						fourthNearestListenerNULL = false;

						fourthNearestListenerPosition = closestPosition;

						fourthNearestListenerForward = closestTargetForward;

						closestDistanceSqr = dSqrToTarget;

					}
				}

			}


			if (!thirdNearestListenerNULL && fourthNearestListenerNULL) {

				Vector3 directionToBetweenNearestAndSecondListeners = new Vector3 (nearestListenerPosition.x - secondNearestListenerPosition.x, nearestListenerPosition.y - secondNearestListenerPosition.y, nearestListenerPosition.z - secondNearestListenerPosition.z);
				float distanceBetweenNearestAndSecondListeners = directionToBetweenNearestAndSecondListeners.sqrMagnitude;

				Vector3 directionToBetweenSecondAndThirdListeners = new Vector3 (secondNearestListenerPosition.x - thirdNearestListenerPosition.x, secondNearestListenerPosition.y - thirdNearestListenerPosition.y, secondNearestListenerPosition.z - thirdNearestListenerPosition.z);
				float distanceBetweenSecondAndThirdListeners = directionToBetweenSecondAndThirdListeners.sqrMagnitude;

				Vector3 directionToBetweenNearestAndThirdListeners = new Vector3 (nearestListenerPosition.x - thirdNearestListenerPosition.x, nearestListenerPosition.y - thirdNearestListenerPosition.y, nearestListenerPosition.z - thirdNearestListenerPosition.z);
				float distanceBetweeNearestAndThirdListeners = directionToBetweenNearestAndThirdListeners.sqrMagnitude;

				distanceBetweenListeners = Mathf.Sqrt ((distanceBetweenNearestAndSecondListeners + distanceBetweenSecondAndThirdListeners + distanceBetweeNearestAndThirdListeners) / 3);


			} else if (!fourthNearestListenerNULL) {

				Vector3 directionToBetweenNearestAndSecondListeners = new Vector3 (nearestListenerPosition.x - secondNearestListenerPosition.x, nearestListenerPosition.y - secondNearestListenerPosition.y, nearestListenerPosition.z - secondNearestListenerPosition.z);
				float distanceBetweenNearestAndSecondListeners = directionToBetweenNearestAndSecondListeners.sqrMagnitude;

				Vector3 directionToBetweenNearestAndThirdListeners = new Vector3 (nearestListenerPosition.x - thirdNearestListenerPosition.x, nearestListenerPosition.y - thirdNearestListenerPosition.y, nearestListenerPosition.z - thirdNearestListenerPosition.z);
				float distanceBetweenNearestAndThirdListeners = directionToBetweenNearestAndThirdListeners.sqrMagnitude;

				Vector3 directionToBetweenNearestAndFourthListeners = new Vector3 (nearestListenerPosition.x - fourthNearestListenerPosition.x, nearestListenerPosition.y - fourthNearestListenerPosition.y, nearestListenerPosition.z - fourthNearestListenerPosition.z);
				float distanceBetweenNearestAndFourthListeners = directionToBetweenNearestAndFourthListeners.sqrMagnitude;

				Vector3 directionToBetweenSecondAndThirdListeners = new Vector3 (secondNearestListenerPosition.x - thirdNearestListenerPosition.x, secondNearestListenerPosition.y - thirdNearestListenerPosition.y, secondNearestListenerPosition.z - thirdNearestListenerPosition.z);
				float distanceBetweenSecondAndThirdListeners = directionToBetweenSecondAndThirdListeners.sqrMagnitude;

				Vector3 directionToBetweenSecondAndFourthListeners = new Vector3 (secondNearestListenerPosition.x - fourthNearestListenerPosition.x, secondNearestListenerPosition.y - fourthNearestListenerPosition.y, secondNearestListenerPosition.z - fourthNearestListenerPosition.z);
				float distanceBetweenSecondAndFourthListeners = directionToBetweenSecondAndFourthListeners.sqrMagnitude;

				Vector3 directionToBetweenThirdAndFourthListeners = new Vector3 (thirdNearestListenerPosition.x - fourthNearestListenerPosition.x, thirdNearestListenerPosition.y - fourthNearestListenerPosition.y, thirdNearestListenerPosition.z - fourthNearestListenerPosition.z);
				float distanceBetweenThirdAndFourthListeners = directionToBetweenThirdAndFourthListeners.sqrMagnitude;

				distanceBetweenListeners = Mathf.Sqrt ((distanceBetweenNearestAndSecondListeners + distanceBetweenNearestAndThirdListeners + distanceBetweenNearestAndFourthListeners +
					distanceBetweenSecondAndThirdListeners + distanceBetweenSecondAndFourthListeners + distanceBetweenThirdAndFourthListeners) / 6);

			} else if (thirdNearestListenerNULL && fourthNearestListenerNULL && !secondNearestListenerNULL) {

				Vector3 directionToBetweenListeners = new Vector3 (nearestListenerPosition.x - secondNearestListenerPosition.x, nearestListenerPosition.y - secondNearestListenerPosition.y, nearestListenerPosition.z - secondNearestListenerPosition.z);
				distanceBetweenListeners = directionToBetweenListeners.magnitude;

			}

			if (!nearestListenerNULL) {
				Vector3 directionToNearestListener = new Vector3 (nearestListenerPosition.x - thisPosition.x, nearestListenerPosition.y - thisPosition.y, nearestListenerPosition.z - thisPosition.z);
				float distance = directionToNearestListener.magnitude;

				nearestListenerDistance = distance;
				nearestListenerBlendDistance = Mathf.Clamp01 (Mathf.Abs(1 - ((distance) / distanceBetweenListeners)));
				nearestListenerBlend =  Mathf.Clamp01 (1 - (nearestListenerDistance - minDistance) / (maxDistance - minDistance));

				if (spatialMode2D) {
					nearestListenerBlend = 1;
					nearestListenerBlendDistance = 1;
				}
			}

			if (!secondNearestListenerNULL) {
				Vector3 directionToSecondNearestListener = new Vector3 (secondNearestListenerPosition.x - thisPosition.x, secondNearestListenerPosition.y - thisPosition.y, secondNearestListenerPosition.z - thisPosition.z);
				float distanceSecond = directionToSecondNearestListener.magnitude;

				secondNearestListenerDistance = distanceSecond;
				secondNearestListenerBlendDistance = Mathf.Clamp01 (Mathf.Abs(1 - ((distanceSecond) / distanceBetweenListeners)));
				secondNearestListenerBlend = Mathf.Clamp01 (1 - ((secondNearestListenerDistance) - minDistance) / (maxDistance - minDistance));

				if (spatialMode2D) {
					secondNearestListenerBlend = 0;
					secondNearestListenerBlendDistance = 0;
				}
			}

			if (!thirdNearestListenerNULL) {
				Vector3 directionToThirdNearestListener = new Vector3 (thirdNearestListenerPosition.x - thisPosition.x, thirdNearestListenerPosition.y - thisPosition.y, thirdNearestListenerPosition.z - thisPosition.z);
				float distanceThird = directionToThirdNearestListener.magnitude;

				thirdNearestListenerDistance = distanceThird;
				thirdNearestListenerBlendDistance = Mathf.Clamp01 (1 - ((distanceThird) / distanceBetweenListeners));
				thirdNearestListenerBlend = Mathf.Clamp01 (1 - (thirdNearestListenerDistance - minDistance) / (maxDistance - minDistance));

				if (spatialMode2D) {
					thirdNearestListenerBlend = 0;
					thirdNearestListenerBlendDistance = 0;
				}
			}

			if (!fourthNearestListenerNULL) {
				Vector3 directionToFourthNearestListener = new Vector3 (fourthNearestListenerPosition.x - thisPosition.x, fourthNearestListenerPosition.y - thisPosition.y, fourthNearestListenerPosition.z - thisPosition.z);
				float distanceFourth = directionToFourthNearestListener.magnitude;

				fourthNearestListenerDistance = distanceFourth;
				fourthNearestListenerBlendDistance = Mathf.Clamp01 (Mathf.Abs(1 - ((distanceFourth) / distanceBetweenListeners)));
				fourthNearestListenerBlend = Mathf.Clamp01 (1 - (fourthNearestListenerDistance - minDistance) / (maxDistance - minDistance));

				if (spatialMode2D) {
					fourthNearestListenerBlend = 0;
					fourthNearestListenerBlendDistance = 0;
				}
			}

		}

	}

	void OnDrawGizmosSelected()
	{

		Gizmos.DrawIcon (transform.position, "AudioSourceIco");

	}

	void OnApplicationFocus(bool hasFocus)
	{
		onFocus = hasFocus;
	}

	void OnDrawGizmos()
	{

		if (Application.isPlaying) {
			if (!nearestListenerNULL) {
				Gizmos.color=Color.Lerp(new Color (1, 0, 0f, 0f),new Color (smoothOcclude>0.01f && occludeSound?1:0, 1, 0f,1f),Mathf.Clamp01(nearestListenerBlend));
				if (spatialMode2D) {
					Gizmos.color = new Color (0, 0.25f, 1f, 1f);
				}
				if (Mute) {
					Gizmos.color = new Color (1f, 0f, 0f, 1f);
				}
				Gizmos.DrawLine (transform.position, nearestListenerPosition);
			}
			if (!secondNearestListenerNULL) {
				Gizmos.color=Color.Lerp(new Color (1, 0, 0f, 0f),new Color (smoothOcclude>0.01f && occludeSound?1:0, 1, 0f,1f),Mathf.Clamp01(secondNearestListenerBlend));
				if (spatialMode2D) {
					Gizmos.color = new Color (0, 0.25f, 1f, 1f);
				}
				if (Mute) {
					Gizmos.color = new Color (1f, 0f, 0f, 1f);
				}
				Gizmos.DrawLine (transform.position, secondNearestListenerPosition);
			}
			if (!thirdNearestListenerNULL) {
				Gizmos.color=Color.Lerp(new Color (1, 0, 0f, 0f),new Color (smoothOcclude>0.01f && occludeSound?1:0, 1, 0f,1f),Mathf.Clamp01(thirdNearestListenerBlend));
				if (spatialMode2D) {
					Gizmos.color = new Color (0, 0.25f, 1f, 1f);
				}
				if (Mute) {
					Gizmos.color = new Color (1f, 0f, 0f, 1f);
				}
				Gizmos.DrawLine (transform.position, thirdNearestListenerPosition);
			}
			if (!fourthNearestListenerNULL) {
				Gizmos.color=Color.Lerp(new Color (1, 0, 0f, 0f),new Color (smoothOcclude>0.01f && occludeSound?1:0, 1, 0f,1f),Mathf.Clamp01(fourthNearestListenerBlend));
				if (spatialMode2D) {
					Gizmos.color = new Color (0, 0.25f, 1f, 1f);
				}
				if (Mute) {
					Gizmos.color = new Color (1f, 0f, 0f, 1f);
				}
				Gizmos.DrawLine (transform.position, fourthNearestListenerPosition);
			}
		}


	}

	#if UNITY_EDITOR

	[CustomEditor(typeof(MultiAudioSource))]
	public class MultiAudioSourceEditor: Editor 
	{

		SerializedObject audioSourceObj;
		SerializedProperty audioObjectProp;
		SerializedProperty playUpdateModeProp;
		SerializedProperty overrideValuesProp;
		SerializedProperty targetToFollowProp;
		SerializedProperty muteProp;
		SerializedProperty overrideAudioClipProp;
		SerializedProperty audioClipOverrideProp;
		SerializedProperty overrideRandomVolumeMultiplierProp;
		SerializedProperty randomVolumeMultiplierProp;
		SerializedProperty overrideVolumeProp;
		SerializedProperty volumeOverrideProp;
		SerializedProperty overrideRandomPitchMultiplierProp;
		SerializedProperty randomPitchMultiplierProp;
		SerializedProperty overridePitchProp;
		SerializedProperty pitchOverrideProp;
		SerializedProperty overrideSpatialModeProp;
		SerializedProperty spatialMode2DOverrideProp;
		SerializedProperty overrideStereoPanProp;
		SerializedProperty stereoPanOverrideProp;
		SerializedProperty overrideSpreadProp;
		SerializedProperty spreadOverrideProp;
		SerializedProperty overrideDopplerLevelProp;
		SerializedProperty dopplerLevelOverrideProp;
		SerializedProperty overrideReverbZoneProp;
		SerializedProperty reverbZoneMixOverrideProp;
		SerializedProperty overrideDistanceProp;
		SerializedProperty minDistanceOverrideProp;
		SerializedProperty maxDistanceOverrideProp;
		SerializedProperty overrideMixerGroupProp;
		SerializedProperty mixerGroupOverrideProp;
		SerializedProperty occludeSoundProp;
		SerializedProperty delayProp;
		SerializedProperty delayUpdateModeProp;
		SerializedProperty overrideVolumeRolloffProp;
		SerializedProperty volumeRolloffOverrideProp;
		SerializedProperty volumeRolloffCurveOverrideProp;
		SerializedProperty overrideSpatializeProp;
		SerializedProperty spatializeOverrideProp;
		SerializedProperty playOnStartProp;
		SerializedProperty ignoreListenerPauseProp;
		SerializedProperty localPauseProp;
			MultiAudioManager.UpdateModes playUpdate;
			MultiAudioManager.UpdateModes delayUpdate;

		void OnEnable()
		{

			audioSourceObj = new SerializedObject (target);
			audioObjectProp = audioSourceObj.FindProperty ("audioObject");
			playUpdateModeProp = audioSourceObj.FindProperty ("playUpdateMode");
			overrideValuesProp = audioSourceObj.FindProperty ("OverrideValues");
			targetToFollowProp = audioSourceObj.FindProperty ("TargetToFollow");
			muteProp = audioSourceObj.FindProperty ("Mute");
			overrideAudioClipProp = audioSourceObj.FindProperty ("overrideAudioClip");
			audioClipOverrideProp = audioSourceObj.FindProperty ("audioClipOverride");

			overrideRandomVolumeMultiplierProp = audioSourceObj.FindProperty ("overrideRandomVolumeMultiplier");
			randomVolumeMultiplierProp = audioSourceObj.FindProperty ("randomVolumeMultiplier");
			overrideVolumeProp = audioSourceObj.FindProperty ("overrideVolume");
			volumeOverrideProp = audioSourceObj.FindProperty ("volumeOverride");
			overrideRandomPitchMultiplierProp = audioSourceObj.FindProperty ("overrideRandomPitchMultiplier");
			randomPitchMultiplierProp = audioSourceObj.FindProperty ("randomPitchMultiplier");
			overridePitchProp = audioSourceObj.FindProperty ("overridePitch");
			pitchOverrideProp = audioSourceObj.FindProperty ("pitchOverride");
			overrideSpatialModeProp = audioSourceObj.FindProperty ("overrideSpatialMode");
			spatialMode2DOverrideProp = audioSourceObj.FindProperty ("spatialMode2DOverride");
			overrideStereoPanProp = audioSourceObj.FindProperty ("overrideStereoPan");
			stereoPanOverrideProp = audioSourceObj.FindProperty ("stereoPanOverride");
			overrideSpreadProp = audioSourceObj.FindProperty ("overrideSpread");
			spreadOverrideProp = audioSourceObj.FindProperty ("spreadOverride");
			overrideDopplerLevelProp = audioSourceObj.FindProperty ("overrideDopplerLevel");
			dopplerLevelOverrideProp = audioSourceObj.FindProperty ("dopplerLevelOverride");
			overrideReverbZoneProp = audioSourceObj.FindProperty ("overrideReverbZone");
			reverbZoneMixOverrideProp = audioSourceObj.FindProperty ("reverbZoneMixOverride");

			overrideMixerGroupProp = audioSourceObj.FindProperty ("overrideMixerGroup");
			mixerGroupOverrideProp = audioSourceObj.FindProperty ("mixerGroupOverride");
			occludeSoundProp = audioSourceObj.FindProperty ("occludeSound");
			delayProp = audioSourceObj.FindProperty ("delay");
			delayUpdateModeProp = audioSourceObj.FindProperty ("delayUpdateMode");
			overrideVolumeRolloffProp = audioSourceObj.FindProperty ("overrideVolumeRolloff");
			volumeRolloffOverrideProp = audioSourceObj.FindProperty ("volumeRolloffOverride");
			volumeRolloffCurveOverrideProp = audioSourceObj.FindProperty ("volumeRolloffCurveOverride");
			overrideSpatializeProp = audioSourceObj.FindProperty ("overrideSpatialize");
			spatializeOverrideProp = audioSourceObj.FindProperty ("spatializeOverride");
			playOnStartProp = audioSourceObj.FindProperty ("playOnStart");
			ignoreListenerPauseProp = audioSourceObj.FindProperty ("ignoreListenerPause");

			overrideDistanceProp = audioSourceObj.FindProperty ("overrideDistance");
			minDistanceOverrideProp = audioSourceObj.FindProperty ("minDistanceOverride");
			maxDistanceOverrideProp = audioSourceObj.FindProperty ("maxDistanceOverride");

			localPauseProp = audioSourceObj.FindProperty ("paused");

		}

		public override void OnInspectorGUI()
		{
			audioSourceObj.Update();

				audioObjectProp.objectReferenceValue=EditorGUILayout.ObjectField ("Audio Object",audioObjectProp.objectReferenceValue, typeof(AudioObject),false);

			if (audioObjectProp.objectReferenceValue != null) {
				
				overrideValuesProp.boolValue = EditorGUILayout.BeginToggleGroup ("Override Parameters",overrideValuesProp.boolValue);

				if (overrideValuesProp.boolValue) {


					overrideAudioClipProp.boolValue=EditorGUILayout.ToggleLeft ("Override Audio Clip", overrideAudioClipProp.boolValue);

					if (overrideAudioClipProp.boolValue) {

							audioClipOverrideProp.objectReferenceValue=EditorGUILayout.ObjectField (audioClipOverrideProp.objectReferenceValue, typeof(AudioClip),false);

					}

					overrideRandomVolumeMultiplierProp.boolValue=EditorGUILayout.ToggleLeft ("Override Random Start Volume Multiplier", overrideRandomVolumeMultiplierProp.boolValue);

					if (overrideRandomVolumeMultiplierProp.boolValue) {

						randomVolumeMultiplierProp.boolValue=EditorGUILayout.Toggle ("Enable Random Start Volume",randomVolumeMultiplierProp.boolValue);

					}

					overrideVolumeProp.boolValue=EditorGUILayout.ToggleLeft ("Override Volume", overrideVolumeProp.boolValue);

					if (overrideVolumeProp.boolValue) {
					
						volumeOverrideProp.floatValue=EditorGUILayout.Slider (volumeOverrideProp.floatValue, 0, 1);

					}

					overrideRandomPitchMultiplierProp.boolValue=EditorGUILayout.ToggleLeft ("Override Random Start Pitch Multiplier", overrideRandomPitchMultiplierProp.boolValue);

					if (overrideRandomPitchMultiplierProp.boolValue) {

							randomPitchMultiplierProp.boolValue=EditorGUILayout.Toggle ("Enable Random Start Pitch",randomPitchMultiplierProp.boolValue);

					}

					overridePitchProp.boolValue=EditorGUILayout.ToggleLeft ("Override Pitch", overridePitchProp.boolValue);

					if (overridePitchProp.boolValue) {

						pitchOverrideProp.floatValue=EditorGUILayout.Slider (pitchOverrideProp.floatValue, -3, 3);
						EditorGUILayout.LabelField ("A negative pitch value will going to make the Audio Object plays backwards", EditorStyles.miniLabel);

					}


					overrideSpreadProp.boolValue=EditorGUILayout.ToggleLeft ("Override Spread", overrideSpreadProp.boolValue);

					if (overrideSpreadProp.boolValue) {

						spreadOverrideProp.floatValue=EditorGUILayout.Slider (spreadOverrideProp.floatValue, 0, 360);

					}

					overrideSpatialModeProp.boolValue=EditorGUILayout.ToggleLeft ("Override Spatial Mode", overrideSpatialModeProp.boolValue);

					if (overrideSpatialModeProp.boolValue) {

						spatialMode2DOverrideProp.boolValue=EditorGUILayout.Toggle ("2D Spatial Mode",spatialMode2DOverrideProp.boolValue);

					}

					overrideStereoPanProp.boolValue=EditorGUILayout.ToggleLeft ("Override 2D Stereo Pan", overrideStereoPanProp.boolValue);

					if (overrideStereoPanProp.boolValue) {

						stereoPanOverrideProp.floatValue=EditorGUILayout.Slider (stereoPanOverrideProp.floatValue, -1, 1);

					}

					overrideDistanceProp.boolValue=EditorGUILayout.ToggleLeft ("Override Distance", overrideDistanceProp.boolValue);

					if (overrideDistanceProp.boolValue) {

						minDistanceOverrideProp.floatValue=Mathf.Clamp(EditorGUILayout.FloatField ("Min Distance",minDistanceOverrideProp.floatValue),0,Mathf.Infinity);
						maxDistanceOverrideProp.floatValue=Mathf.Clamp(EditorGUILayout.FloatField ("Max Distance",maxDistanceOverrideProp.floatValue),minDistanceOverrideProp.floatValue,Mathf.Infinity);

					}

					overrideReverbZoneProp.boolValue=EditorGUILayout.ToggleLeft ("Override Reverb Zone Mix", overrideReverbZoneProp.boolValue);

					if (overrideReverbZoneProp.boolValue) {

						reverbZoneMixOverrideProp.floatValue=EditorGUILayout.Slider (reverbZoneMixOverrideProp.floatValue, 0, 1.1f);

					}

					overrideDopplerLevelProp.boolValue=EditorGUILayout.ToggleLeft ("Override Doppler Level", overrideDopplerLevelProp.boolValue);

					if (overrideDopplerLevelProp.boolValue) {

						dopplerLevelOverrideProp.floatValue=EditorGUILayout.Slider (dopplerLevelOverrideProp.floatValue, 0, 5f);

					}

					overrideVolumeRolloffProp.boolValue= EditorGUILayout.ToggleLeft ("Override Volume Rolloff", overrideVolumeRolloffProp.boolValue);
					if (overrideVolumeRolloffProp.boolValue)
					{
						volumeRolloffOverrideProp.boolValue = EditorGUILayout.BeginToggleGroup ("Volume Rolloff", volumeRolloffOverrideProp.boolValue);

						
						volumeRolloffCurveOverrideProp.animationCurveValue = EditorGUILayout.CurveField ("Rolloff Curve", volumeRolloffCurveOverrideProp.animationCurveValue);

						if (GUILayout.Button ("Use Logarithmic Rolloff Curve",EditorStyles.miniButton)) {

							volumeRolloffCurveOverrideProp.animationCurveValue = new AnimationCurve(new Keyframe[]{new Keyframe(0,0,0,0),new Keyframe(0.2f,0.015f,0.09f,0.09f),new Keyframe(0.6f,0.1f,0.3916f,0.3916f),new Keyframe(0.8f,0.25f,1.33f,1.33f),new Keyframe(0.9f,0.5f,5f,5f),new Keyframe(0.95f,1f,14.26f,14.26f) });

						}
						
						EditorGUILayout.EndToggleGroup ();
					}
						


					overrideMixerGroupProp.boolValue=EditorGUILayout.ToggleLeft ("Override Mixer Group", overrideMixerGroupProp.boolValue);

					if (overrideMixerGroupProp.boolValue) {

							mixerGroupOverrideProp.objectReferenceValue=EditorGUILayout.ObjectField (mixerGroupOverrideProp.objectReferenceValue, typeof(AudioMixerGroup),false);

					}

					overrideSpatializeProp.boolValue=EditorGUILayout.ToggleLeft ("Override Spatialize", overrideSpatializeProp.boolValue);

					if (overrideSpatializeProp.boolValue) {

							spatializeOverrideProp.boolValue=EditorGUILayout.Toggle ("Spatialize",spatializeOverrideProp.boolValue);

					}

				}

				EditorGUILayout.EndToggleGroup ();

			}
			EditorGUILayout.Space ();
				playUpdateModeProp.enumValueIndex = (int)(MultiAudioManager.UpdateModes)EditorGUILayout.EnumPopup ("Update Mode",(audioSourceObj.targetObject as MultiAudioSource).playUpdateMode);
			EditorGUILayout.Space ();
			playOnStartProp.boolValue = EditorGUILayout.Toggle ("Play On Start", playOnStartProp.boolValue);
			delayProp.floatValue = Mathf.Clamp(EditorGUILayout.FloatField ("Delay Before Start",delayProp.floatValue),0,Mathf.Infinity);
			delayUpdateModeProp.enumValueIndex = (int)(MultiAudioManager.UpdateModes)EditorGUILayout.EnumPopup ("Delay Update Mode",(audioSourceObj.targetObject as MultiAudioSource).delayUpdateMode);
			EditorGUILayout.Space ();
			ignoreListenerPauseProp.boolValue = EditorGUILayout.Toggle ("Ignore Listener Pause", ignoreListenerPauseProp.boolValue);
			EditorGUILayout.Space ();
			occludeSoundProp.boolValue = EditorGUILayout.Toggle (new GUIContent("Occlude Sound","Occludes the sound if there is an Collider between the source and the listener with one of the MultiAudioManagerConfig.occludeCheck layers"), occludeSoundProp.boolValue);
			EditorGUILayout.Space ();
	
			targetToFollowProp.objectReferenceValue = EditorGUILayout.ObjectField ("Target to Follow",targetToFollowProp.objectReferenceValue, typeof(Transform),true);
	
			EditorGUILayout.Space ();
			EditorGUILayout.Space ();
			localPauseProp.boolValue = EditorGUILayout.Toggle ("Pause", localPauseProp.boolValue);
			EditorGUILayout.Space ();
			muteProp.boolValue = EditorGUILayout.Toggle ("Mute", muteProp.boolValue);

			audioSourceObj.ApplyModifiedProperties();
		}

		private void OnSceneGUI()
			{

				audioSourceObj.Update ();

		#if UNITY_5_6_OR_NEWER
				Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
		#endif

				if (overrideDistanceProp.boolValue) {

					Handles.color=new Color(0.69f,0.89f,1f,1);

					EditorGUI.BeginChangeCheck ();
					float maxDistance = Handles.RadiusHandle (Quaternion.identity, (audioSourceObj.targetObject as MultiAudioSource).transform.position, maxDistanceOverrideProp.floatValue);
					if (EditorGUI.EndChangeCheck ()) {
						Undo.RecordObject (target, "Changed Max Audible Distance");
						maxDistanceOverrideProp.floatValue = maxDistance;
					}


					EditorGUI.BeginChangeCheck ();
					float minDistance = Handles.RadiusHandle (Quaternion.identity, (audioSourceObj.targetObject as MultiAudioSource).transform.position, minDistanceOverrideProp.floatValue);
					if (EditorGUI.EndChangeCheck ()) {
						Undo.RecordObject (target, "Changed Min Audible Distance");
						minDistanceOverrideProp.floatValue = minDistance;
					}
				}

				if (!overrideDistanceProp.boolValue || overrideDistanceProp.boolValue && !overrideValuesProp.boolValue) {
				
					if (audioObjectProp.objectReferenceValue != null) {
						Handles.color = new Color (0.49f, 0.69f, 0.99f, 1);

						EditorGUI.BeginChangeCheck ();
						float maxDistance = Handles.RadiusHandle (Quaternion.identity, (audioSourceObj.targetObject as MultiAudioSource).transform.position, (audioObjectProp.objectReferenceValue as AudioObject).maxDistance);
						if (EditorGUI.EndChangeCheck ()) {
							Undo.RecordObject (target, "Changed Max Audible Distance");
							maxDistanceOverrideProp.floatValue = maxDistance;
							overrideDistanceProp.boolValue = true;
							overrideValuesProp.boolValue = true;
						}

						EditorGUI.BeginChangeCheck ();
						float minDistance = Handles.RadiusHandle (Quaternion.identity, (audioSourceObj.targetObject as MultiAudioSource).transform.position, (audioObjectProp.objectReferenceValue as AudioObject).minDistance);
						if (EditorGUI.EndChangeCheck ()) {
							Undo.RecordObject (target, "Changed Min Audible Distance");
							minDistanceOverrideProp.floatValue = minDistance;
							overrideDistanceProp.boolValue = true;
							overrideValuesProp.boolValue = true;
						}

					}

				}


				audioSourceObj.ApplyModifiedProperties ();

			}


	}

	#endif

}
}