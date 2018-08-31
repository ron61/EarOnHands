using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using AlmenaraGames;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AlmenaraGames{
	[HelpURL("https://drive.google.com/uc?&id=1wSFIUL0gqVL2Gan0qgB_EZdYGHwa9F_p#page=19")]
	[AddComponentMenu("")]
public class MultiAudioManager : MonoBehaviour {

	AlmenaraGames.Tools.MultiAudioManagerConfig config;
	public enum UpdateModes
	{
		ScaledTime,
		UnscaledTime
	}

	private AudioMixerGroup sfxMixerGroup;
	private AudioMixerGroup bgmMixerGroup;
	private LayerMask occludeCheck;
	private float occludeMultiplier=0.5f;

	public AudioMixerGroup SfxMixerGroup{ get {return sfxMixerGroup;} }
	public AudioMixerGroup BgmMixerGroup{ get {return bgmMixerGroup;} }
	public LayerMask OccludeCheck{ get {return occludeCheck;} }
	public float OccludeMultiplier{ get {return occludeMultiplier;} }

	private bool prevPauseListener;
	[SerializeField] private bool paused;
	/// <summary>
	/// Sets the pause state of all of the listeners.
	/// </summary>
	public static bool Paused{get { return MultiAudioManager.Instance.paused; } set{MultiAudioManager.Instance.paused = value;}}

	private bool ignore;

	private static MultiAudioManager instance;

	private Vector3 audioListenerPosition;
	public Vector3 AudioListenerPosition{ get { return audioListenerPosition; } }

	public List<MultiAudioListener> listenersComponents = new List<MultiAudioListener>();

	[HideInInspector]
	public List<Vector3> listenersForwards = new List<Vector3>();
	[HideInInspector]
	public List<MultiAudioListener> oldListeners = new List<MultiAudioListener>();
	[HideInInspector]
	public List<Vector3> listenersPositions = new List<Vector3>();
	[HideInInspector]
	public List<Vector3> reverbZonePositions = new List<Vector3>();
	
	[HideInInspector] private List<MultiAudioSource> audioSources=new List<MultiAudioSource>();
	public List<MultiAudioSource> AudioSources{ get { return audioSources; } set {audioSources = value;}}

	private List<AudioObject> globalAudioObjects = new List<AudioObject> ();

	private static bool init=false;
	public static bool Initialized{ get { return MultiAudioManager.init; } }

	private int maxAudioSources=512;

	private string audioSourcePlayingIconStr = "AudioObjectIco";

	public static int sessionIndex=0;
	
	public static bool noListeners = false;

	//Singleton check
	public static MultiAudioManager Instance 
	{
		get {

			if (applicationIsQuitting || !Application.isPlaying)
				return null;

			if (instance == null) {
				
					GameObject _MultiAudioManager = new GameObject ("MultiAudioManager");
					_MultiAudioManager.AddComponent<MultiAudioManager> ();

					foreach (var item in GameObject.FindObjectsOfType (typeof(AudioListener))) {
						Destroy (item as AudioListener);
					}

					_MultiAudioManager.AddComponent<AudioListener> ();

			}
			return instance;
		}
	}

	// Use this for initialization
	void Awake () {


		
		// if the singleton hasn't been initialized yet
		if (instance != null && instance != this) 
		{
			ignore = true;
			Destroy(this.gameObject);
			return;
		}

		instance = this;

		DontDestroyOnLoad(gameObject);



		if (!ignore) {

				sessionIndex = 0;
			
			GameObject configGo = Resources.Load ("Multi Listener Pooling Audio System Config") as GameObject;

			#if UNITY_EDITOR

			if (configGo == null) {

				PrefabUtility.CreatePrefab("Assets/AlmenaraGames/MLPAS/Resources/Multi Listener Pooling Audio System Config.prefab",new GameObject("Multi Listener Pooling Audio System Config",typeof(AlmenaraGames.Tools.MultiAudioManagerConfig)));

				Debug.LogWarning ("<b>Multi Listener Pooling Audio System Config</b> is missing, a <i>New One</i> has been created", Resources.Load ("Multi Listener Pooling Audio System Config"));

			}
			#endif

			configGo = Resources.Load ("Multi Listener Pooling Audio System Config") as GameObject;

			if (configGo!=null)
				config = configGo.GetComponent<AlmenaraGames.Tools.MultiAudioManagerConfig>();

			sfxMixerGroup=config.sfxMixerGroup;
			bgmMixerGroup=config.bgmMixerGroup;
			occludeCheck=config.occludeCheck;
			maxAudioSources=config.maxAudioSources;
			occludeMultiplier = config.occludeMultiplier;

			MultiPoolAudioSystem.audioManager = Instance;

			ClearAudioListeners ();

			//Fill The Pool
			for (int i = 0; i < maxAudioSources; i++) {
				GameObject au = new GameObject ("DynamicAudioSource_" + (i+1).ToString());
				au.hideFlags = HideFlags.HideInHierarchy;
				MultiAudioSource ssAus = au.AddComponent<MultiAudioSource> ();
				ssAus.InsidePool = true;
				MultiPoolAudioSystem.audioManager.AudioSources.Add (ssAus);
				au.transform.parent = this.gameObject.transform;
				au.SetActive (false);
				
			}

			LoadAllGlobalAudioObjects ();



		}

	}

	void OnEnable()
	{
			SceneManager.sceneLoaded += OnSceneLoaded;
			SceneManager.sceneUnloaded += OnSceneUnloaded;

			noListeners = listenersForwards.Count < 1;

	}

	void OnDisable()
	{
			SceneManager.sceneLoaded -= OnSceneLoaded;
			SceneManager.sceneUnloaded -= OnSceneUnloaded;
	}
	
	void OnSceneUnloaded(Scene scene)
	{

				foreach (var source in audioSources) {

					if (source.InsidePool && !source.PersistsBetweenScenes && source.SessionIndex <= sessionIndex)
						source.Stop ();

				}

			sessionIndex += 1;

	}
	
	void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{

			foreach (var item in GameObject.FindObjectsOfType (typeof(AudioListener))) {
				if ((item as AudioListener).gameObject!=this.gameObject)
				Destroy (item as AudioListener);
			}
	}

	void Update()
	{

		if (prevPauseListener != Paused) {
			AudioListener.pause = Paused;
			prevPauseListener = Paused;
		}

		transform.position = Vector3.zero;
		audioListenerPosition = transform.position;

	}

	public void ReloadConfig()
	{
		sfxMixerGroup=config.sfxMixerGroup;
		bgmMixerGroup=config.bgmMixerGroup;
		occludeCheck=config.occludeCheck;
		maxAudioSources=config.maxAudioSources;
	}

	void LoadAllGlobalAudioObjects()
	{

			foreach (var obj in Resources.LoadAll<AudioObject>("Global Audio Objects") ) {

			var asset = obj;

			bool checkSameIdentifier = false;
			AudioObject testObject=asset;

			foreach (var item in globalAudioObjects) {

				if (!string.IsNullOrEmpty (asset.identifier) && !string.IsNullOrEmpty (item.identifier) && item.identifier == asset.identifier) {
					checkSameIdentifier = true;
					testObject = item;
				}

			}

			if (!string.IsNullOrEmpty (asset.identifier) && !checkSameIdentifier) {
				globalAudioObjects.Add (asset);
			}
			else {

				if (!string.IsNullOrEmpty (asset.identifier) && checkSameIdentifier) {

					Debug.LogError ("<b>" + testObject.name + "</b> and " + "<b>" + asset.name + "</b> has the same identifier. Change or remove the "+ "<b>" + asset.name + "</b> identifier to avoid conflicts",asset);

				}

			}

		}

	}

	/// <summary>
	/// Gets the <see cref="AudioObject"/>with the specific identifier.
	/// </summary>
	/// <param name="identifier">Identifier.</param>
	public static AudioObject GetAudioObjectByIdentifier(string identifier)
	{

		for (int i = 0; i <  MultiAudioManager.Instance.globalAudioObjects.Count; i++) {

			if ( MultiPoolAudioSystem.audioManager.globalAudioObjects [i].identifier == identifier) {

				return  MultiPoolAudioSystem.audioManager.globalAudioObjects [i];

			}
			
	}

			Debug.LogWarning ("Can't get an <b>Audio Object</b> with the identifier: " + "<i>"+identifier+"</i>" +"\nRemember that the <b>Audio Object</b> needs to be in the \"Resources\\Global Audio Objects\" folder and the identifier is case sensitive.");
		return null;

	}

	public void AddAvailableListener (MultiAudioListener listener) {

		listenersForwards.Add (listener.RealListener.forward);
		listenersPositions.Add (listener.RealListener.position);
		listenersComponents.Add (listener);
		listener.Index = listenersPositions.Count-1;

	}
		

	public void ClearAudioListeners () {

		listenersComponents.Clear ();
		listenersForwards.Clear ();
		oldListeners.Clear ();
		listenersPositions.Clear ();
		reverbZonePositions.Clear ();

	}

	public void RemoveAudioListener (MultiAudioListener listener) {

		oldListeners = new List<MultiAudioListener>(listenersComponents);

		foreach (var item in oldListeners) {

			item.Index = -1;

		}

		listenersForwards.Clear ();
		listenersPositions.Clear ();
		listenersComponents.Clear ();

		foreach (var item in oldListeners) {
			if (item!=listener)
				AddAvailableListener (item);

		}


		noListeners = listenersForwards.Count < 1;

	}

	static void RealPlayAudioObject(MultiAudioSource _audioSource, AudioObject audioObject,int channel=-1, bool changePosition=false,Vector3 position=default(Vector3),Transform targetToFollow=null, AudioMixerGroup mixerGroup=null,bool occludeSound=true,float delay=0f,float fadeInTime=0f)
	{

		if (changePosition) {
			_audioSource.transform.position = position;
		}

		_audioSource.AudioObject = audioObject;
		_audioSource.gameObject.SetActive (true);
		_audioSource.OccludeSound = occludeSound;
		_audioSource.MixerGroupOverride = mixerGroup;
		if (fadeInTime > 0) {
			_audioSource.PlayFadeIn (fadeInTime, channel, targetToFollow, delay);
		} else {
			_audioSource.Play (channel, targetToFollow, delay);
		}

	}

	static void RealPlayAudioObjectOverride(MultiAudioSource _audioSource, AudioClip audioClipOverride , AudioObject audioObject,int channel=-1, bool changePosition=false,Vector3 position=default(Vector3),Transform targetToFollow=null, AudioMixerGroup mixerGroup=null,bool occludeSound=true,float delay=0f,float fadeInTime=0f)
	{

		if (changePosition) {
			_audioSource.transform.position = position;
		}

		_audioSource.AudioObject = audioObject;
		_audioSource.gameObject.SetActive (true);
		_audioSource.OccludeSound = occludeSound;
		_audioSource.MixerGroupOverride = mixerGroup;
		if (fadeInTime > 0) {
			_audioSource.PlayFadeInOverride (fadeInTime, audioClipOverride, channel, targetToFollow, delay);
		} else {
			_audioSource.PlayOverride (audioClipOverride, channel, targetToFollow, delay);
		}

	}

	#region Play Methods

	#region PlayAudioObject Method
	// Normal with channel-position/transform
	/// <summary>
	/// Plays an <see cref="AudioObject"/>at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	public static MultiAudioSource PlayAudioObject(AudioObject audioObject,int channel,Transform targetToFollow)
	{
		
		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, audioObject, channel, false, Vector3.zero, targetToFollow, null, false);

			return _audioSource;

		}

		return null;
			
	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	public static MultiAudioSource PlayAudioObject(AudioObject audioObject,Transform targetToFollow)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, audioObject, -1, false, Vector3.zero, targetToFollow, null, false);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="position">Position.</param>
	public static MultiAudioSource PlayAudioObject(AudioObject audioObject,Vector3 position)
	{
		
		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, audioObject, -1, true, position, null, null, false);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>at the specified Position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	public static MultiAudioSource PlayAudioObject(AudioObject audioObject,int channel,Vector3 position)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, audioObject, channel, true, position, null, null, false);

			return _audioSource;

		}

		return null;

	}
	// // // // //


	// Normal with channel-position/transform-occludeSound
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObject(AudioObject audioObject,int channel,Transform targetToFollow,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, audioObject, channel, false, Vector3.zero, targetToFollow, null, occludeSound);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObject(AudioObject audioObject,Transform targetToFollow,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, audioObject, -1, false, Vector3.zero, targetToFollow, null, occludeSound);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="position">Position.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObject(AudioObject audioObject,Vector3 position,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, audioObject, -1, true, position, null, null, occludeSound);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObject(AudioObject audioObject,int channel,Vector3 position,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, audioObject, channel, true, position, null, null, occludeSound);

			return _audioSource;

		}

		return null;

	}
	// // // // //

	// Normal with channel-position/transform-mixerGroup
	/// <summary>
	/// Plays an <see cref="AudioObject"/>using a different AudioMixerGroup at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayAudioObject(AudioObject audioObject,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, audioObject, channel, false, Vector3.zero, targetToFollow, mixerGroup, false);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>using a different AudioMixerGroup and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayAudioObject(AudioObject audioObject,Transform targetToFollow,AudioMixerGroup mixerGroup)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, audioObject, -1, false, Vector3.zero, targetToFollow, mixerGroup, false);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>using a different AudioMixerGroup at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayAudioObject(AudioObject audioObject,Vector3 position,AudioMixerGroup mixerGroup)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, audioObject, -1, true, position, null, mixerGroup, false);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>using a different AudioMixerGroup at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayAudioObject(AudioObject audioObject,int channel,Vector3 position,AudioMixerGroup mixerGroup)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, audioObject, channel, true, position, null, mixerGroup, false);

			return _audioSource;

		}

		return null;

	}
	// // // // //

	// Normal with channel-position/transform-mixerGroup-occludeSound
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>using a different AudioMixerGroup at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObject(AudioObject audioObject,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, audioObject, channel, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>using a different AudioMixerGroup and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObject(AudioObject audioObject,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, audioObject, -1, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>using a different AudioMixerGroup at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObject(AudioObject audioObject,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, audioObject, -1, true, position, null, mixerGroup, occludeSound);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>using a different AudioMixerGroup at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObject(AudioObject audioObject,int channel,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, audioObject, channel, true, position, null, mixerGroup, occludeSound);

			return _audioSource;

		}

		return null;

	}
	// // // // //
	#endregion

	#region PlayDelayedAudioObject Method

	// Delayed with channel-position/transform
	/// <summary>
	/// Plays an <see cref="AudioObject"/>with a delay specified in seconds at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	public static MultiAudioSource PlayDelayedAudioObject(AudioObject audioObject,float delay,int channel,Transform targetToFollow)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, audioObject, channel, false, Vector3.zero, targetToFollow, null, false,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>with a delay specified in seconds and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	public static MultiAudioSource PlayDelayedAudioObject(AudioObject audioObject,float delay,Transform targetToFollow)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, audioObject, -1, false, Vector3.zero, targetToFollow, null, false,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>with a delay specified in seconds at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="position">Position.</param>
	public static MultiAudioSource PlayDelayedAudioObject(AudioObject audioObject,float delay,Vector3 position)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, audioObject, -1, true, position, null, null, false,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>with a delay specified in seconds at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	public static MultiAudioSource PlayDelayedAudioObject(AudioObject audioObject,float delay,int channel,Vector3 position)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, audioObject, channel, true, position, null, null, false,delay);

			return _audioSource;

		}

		return null;

	}
	// // // // //


	// Delayed with channel-position/transform-occludeSound
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>with a delay specified in seconds at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObject(AudioObject audioObject,float delay,int channel,Transform targetToFollow,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, audioObject, channel, false, Vector3.zero, targetToFollow, null, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>with a delay specified in seconds and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObject(AudioObject audioObject,float delay,Transform targetToFollow,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, audioObject, -1, false, Vector3.zero, targetToFollow, null, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>with a delay specified in seconds at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="position">Position.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObject(AudioObject audioObject,float delay,Vector3 position,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, audioObject, -1, true, position, null, null, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>with a delay specified in seconds at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObject(AudioObject audioObject,float delay,int channel,Vector3 position,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, audioObject, channel, true, position, null, null, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	// // // // //

	// Delayed with channel-position/transform-mixerGroup
	/// <summary>
	/// Plays an <see cref="AudioObject"/>with a delay specified in seconds using a different AudioMixerGroup at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayDelayedAudioObject(AudioObject audioObject,float delay,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, audioObject, channel, false, Vector3.zero, targetToFollow, mixerGroup, false,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>with a delay specified in seconds using a different AudioMixerGroup and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayDelayedAudioObject(AudioObject audioObject,float delay,Transform targetToFollow,AudioMixerGroup mixerGroup)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, audioObject, -1, false, Vector3.zero, targetToFollow, mixerGroup, false,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>with a delay specified in seconds using a different AudioMixerGroup at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayDelayedAudioObject(AudioObject audioObject,float delay,Vector3 position,AudioMixerGroup mixerGroup)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, audioObject, -1, true, position, null, mixerGroup, false,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>with a delay specified in seconds using a different AudioMixerGroup at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayDelayedAudioObject(AudioObject audioObject,float delay,int channel,Vector3 position,AudioMixerGroup mixerGroup)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, audioObject, channel, true, position, null, mixerGroup, false,delay);

			return _audioSource;

		}

		return null;

	}
	// // // // //

	// Delayed with channel-position/transform-mixerGroup-occludeSound
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>with a delay specified in seconds using a different AudioMixerGroup at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObject(AudioObject audioObject,float delay,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, audioObject, channel, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>with a delay specified in seconds using a different AudioMixerGroup and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObject(AudioObject audioObject,float delay,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, audioObject, -1, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>with a delay specified in seconds using a different AudioMixerGroup at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObject(AudioObject audioObject,float delay,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, audioObject, -1, true, position, null, mixerGroup, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>with a delay specified in seconds using a different AudioMixerGroup at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObject(AudioObject audioObject,float delay,int channel,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, audioObject, channel, true, position, null, mixerGroup, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	// // // // //

	#endregion

	#region PlayAudioObjectOverride Method
	// Normal with channel-position/transform
	/// <summary>
	/// Plays an <see cref="AudioObject"/>with another Audio Clip at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	public static MultiAudioSource PlayAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,int channel,Transform targetToFollow)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, false, Vector3.zero, targetToFollow, null, false);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>with another Audio Clip and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	public static MultiAudioSource PlayAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,Transform targetToFollow)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, false, Vector3.zero, targetToFollow, null, false);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>with another Audio Clip at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="position">Position.</param>
	public static MultiAudioSource PlayAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,Vector3 position)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, true, position, null, null, false);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>with another Audio Clip at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	public static MultiAudioSource PlayAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,int channel,Vector3 position)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, true, position, null, null, false);

			return _audioSource;

		}

		return null;

	}
	// // // // //


	// Normal with channel-position/transform-occludeSound
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>with another Audio Clip at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,int channel,Transform targetToFollow,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, false, Vector3.zero, targetToFollow, null, occludeSound);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>with another Audio Clip and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,Transform targetToFollow,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, false, Vector3.zero, targetToFollow, null, occludeSound);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>with another Audio Clip at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="position">Position.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,Vector3 position,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, true, position, null, null, occludeSound);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>with another Audio Clip at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,int channel,Vector3 position,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, true, position, null, null, occludeSound);

			return _audioSource;

		}

		return null;

	}
	// // // // //

	// Normal with channel-position/transform-mixerGroup
	/// <summary>
	/// Plays an <see cref="AudioObject"/>with another Audio Clip using a different AudioMixerGroup at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, false, Vector3.zero, targetToFollow, mixerGroup, false);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>with another Audio Clip using a different AudioMixerGroup and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,Transform targetToFollow,AudioMixerGroup mixerGroup)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, false, Vector3.zero, targetToFollow, mixerGroup, false);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>with another Audio Clip using a different AudioMixerGroup at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,Vector3 position,AudioMixerGroup mixerGroup)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, true, position, null, mixerGroup, false);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>with another Audio Clip using a different AudioMixerGroup at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,int channel,Vector3 position,AudioMixerGroup mixerGroup)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, true, position, null, mixerGroup, false);

			return _audioSource;

		}

		return null;

	}
	// // // // //

	// Normal with channel-position/transform-mixerGroup-occludeSound
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>with another Audio Clip using a different AudioMixerGroup at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>with another Audio Clip using a different AudioMixerGroup and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>with another Audio Clip using a different AudioMixerGroup at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, true, position, null, mixerGroup, occludeSound);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>with another Audio Clip using a different AudioMixerGroup at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,int channel,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, true, position, null, mixerGroup, occludeSound);

			return _audioSource;

		}

		return null;

	}
	// // // // //
	#endregion

	#region PlayDelayedAudioObjectOverride Method

	// Delayed with channel-position/transform
	/// <summary>
	/// Plays an <see cref="AudioObject"/>with another Audio Clip with a delay specified in seconds at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,int channel,Transform targetToFollow)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, false, Vector3.zero, targetToFollow, null, false,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>with another Audio Clip with a delay specified in seconds and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,Transform targetToFollow)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, false, Vector3.zero, targetToFollow, null, false,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>with another Audio Clip with a delay specified in seconds at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="position">Position.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,Vector3 position)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, true, position, null, null, false,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>with another Audio Clip with a delay specified in seconds at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,int channel,Vector3 position)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, true, position, null, null, false,delay);

			return _audioSource;

		}

		return null;

	}
	// // // // //


	// Delayed with channel-position/transform-occludeSound
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>with another Audio Clip with a delay specified in seconds at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,int channel,Transform targetToFollow,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, false, Vector3.zero, targetToFollow, null, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>with another Audio Clip with a delay specified in seconds and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,Transform targetToFollow,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, false, Vector3.zero, targetToFollow, null, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>with another Audio Clip with a delay specified in seconds at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="position">Position.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,Vector3 position,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, true, position, null, null, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>with another Audio Clip with a delay specified in seconds at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,int channel,Vector3 position,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, true, position, null, null, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	// // // // //

	// Delayed with channel-position/transform-mixerGroup
	/// <summary>
	/// Plays an <see cref="AudioObject"/>with another Audio Clip using a different AudioMixerGroup with a delay specified in seconds at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, false, Vector3.zero, targetToFollow, mixerGroup, false,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>with another Audio Clip using a different AudioMixerGroup with a delay specified in seconds and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,Transform targetToFollow,AudioMixerGroup mixerGroup)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, false, Vector3.zero, targetToFollow, mixerGroup, false,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>with another Audio Clip using a different AudioMixerGroup with a delay specified in seconds at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,Vector3 position,AudioMixerGroup mixerGroup)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, true, position, null, mixerGroup, false,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>with another Audio Clip using a different AudioMixerGroup with a delay specified in seconds at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,int channel,Vector3 position,AudioMixerGroup mixerGroup)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, true, position, null, mixerGroup, false,delay);

			return _audioSource;

		}

		return null;

	}
	// // // // //

	// Delayed with channel-position/transform-mixerGroup-occludeSound
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>with another Audio Clip using a different AudioMixerGroup with a delay specified in seconds at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>with another Audio Clip using a different AudioMixerGroup with a delay specified in seconds and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>with another Audio Clip using a different AudioMixerGroup with a delay specified in seconds at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, true, position, null, mixerGroup, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>with another Audio Clip using a different AudioMixerGroup with a delay specified in seconds at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObject"><see cref="AudioObject"/>.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,int channel,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (audioObject != null) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, true, position, null, mixerGroup, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	// // // // //

	#endregion

	#region PlayAudioObjectByIdentifier Method
	// Normal with channel-position/transform
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	public static MultiAudioSource PlayAudioObjectByIdentifier(string audioObjectIdentifier,int channel,Transform targetToFollow)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, null, false);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	public static MultiAudioSource PlayAudioObjectByIdentifier(string audioObjectIdentifier,Transform targetToFollow)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, null, false);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="position">Position.</param>
	public static MultiAudioSource PlayAudioObjectByIdentifier(string audioObjectIdentifier,Vector3 position)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, null, false);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier at the specified Position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	public static MultiAudioSource PlayAudioObjectByIdentifier(string audioObjectIdentifier,int channel,Vector3 position)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, null, false);

			return _audioSource;

		}

		return null;

	}
	// // // // //


	// Normal with channel-position/transform-occludeSound
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObjectByIdentifier(string audioObjectIdentifier,int channel,Transform targetToFollow,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, null, occludeSound);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObjectByIdentifier(string audioObjectIdentifier,Transform targetToFollow,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, null, occludeSound);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="position">Position.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObjectByIdentifier(string audioObjectIdentifier,Vector3 position,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, null, occludeSound);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObjectByIdentifier(string audioObjectIdentifier,int channel,Vector3 position,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, null, occludeSound);

			return _audioSource;

		}

		return null;

	}
	// // // // //

	// Normal with channel-position/transform-mixerGroup
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier using a different AudioMixerGroup at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayAudioObjectByIdentifier(string audioObjectIdentifier,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, mixerGroup, false);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier using a different AudioMixerGroup and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayAudioObjectByIdentifier(string audioObjectIdentifier,Transform targetToFollow,AudioMixerGroup mixerGroup)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, mixerGroup, false);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier using a different AudioMixerGroup at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayAudioObjectByIdentifier(string audioObjectIdentifier,Vector3 position,AudioMixerGroup mixerGroup)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, mixerGroup, false);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier using a different AudioMixerGroup at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayAudioObjectByIdentifier(string audioObjectIdentifier,int channel,Vector3 position,AudioMixerGroup mixerGroup)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, mixerGroup, false);

			return _audioSource;

		}

		return null;

	}
	// // // // //

	// Normal with channel-position/transform-mixerGroup-occludeSound
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier using a different AudioMixerGroup at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObjectByIdentifier(string audioObjectIdentifier,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier using a different AudioMixerGroup and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObjectByIdentifier(string audioObjectIdentifier,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier using a different AudioMixerGroup at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObjectByIdentifier(string audioObjectIdentifier,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, mixerGroup, occludeSound);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier using a different AudioMixerGroup at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObjectByIdentifier(string audioObjectIdentifier,int channel,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, mixerGroup, occludeSound);

			return _audioSource;

		}

		return null;

	}
	// // // // //
	#endregion

	#region PlayDelayedAudioObjectByIdentifier Method

	// Delayed with channel-position/transform
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier with a delay specified in seconds at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	public static MultiAudioSource PlayDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,int channel,Transform targetToFollow)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, null, false,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier with a delay specified in seconds and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	public static MultiAudioSource PlayDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,Transform targetToFollow)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, null, false,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier with a delay specified in seconds at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="position">Position.</param>
	public static MultiAudioSource PlayDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,Vector3 position)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, null, false,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier with a delay specified in seconds at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	public static MultiAudioSource PlayDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,int channel,Vector3 position)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, null, false,delay);

			return _audioSource;

		}

		return null;

	}
	// // // // //


	// Delayed with channel-position/transform-occludeSound
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a delay specified in seconds at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,int channel,Transform targetToFollow,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, null, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a delay specified in seconds and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,Transform targetToFollow,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, null, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a delay specified in seconds at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="position">Position.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,Vector3 position,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, null, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a delay specified in seconds at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,int channel,Vector3 position,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, null, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	// // // // //

	// Delayed with channel-position/transform-mixerGroup
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier with a delay specified in seconds using a different AudioMixerGroup at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, mixerGroup, false,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier with a delay specified in seconds using a different AudioMixerGroup and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,Transform targetToFollow,AudioMixerGroup mixerGroup)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, mixerGroup, false,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier with a delay specified in seconds using a different AudioMixerGroup at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,Vector3 position,AudioMixerGroup mixerGroup)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, mixerGroup, false,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier with a delay specified in seconds using a different AudioMixerGroup at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,int channel,Vector3 position,AudioMixerGroup mixerGroup)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, mixerGroup, false,delay);

			return _audioSource;

		}

		return null;

	}
	// // // // //

	// Delayed with channel-position/transform-mixerGroup-occludeSound
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a delay specified in seconds using a different AudioMixerGroup at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a delay specified in seconds using a different AudioMixerGroup and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a delay specified in seconds using a different AudioMixerGroup at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, mixerGroup, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a delay specified in seconds using a different AudioMixerGroup at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,int channel,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObject (_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, mixerGroup, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	// // // // //

	#endregion

	#region PlayAudioObjectOverrideByIdentifier Method
	// Normal with channel-position/transform
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier with another Audio Clip at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	public static MultiAudioSource PlayAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,int channel,Transform targetToFollow)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, null, false);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier with another Audio Clip and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	public static MultiAudioSource PlayAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,Transform targetToFollow)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, null, false);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier with another Audio Clip at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="position">Position.</param>
	public static MultiAudioSource PlayAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,Vector3 position)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, null, false);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier with another Audio Clip at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	public static MultiAudioSource PlayAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,int channel,Vector3 position)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, null, false);

			return _audioSource;

		}

		return null;

	}
	// // // // //


	// Normal with channel-position/transform-occludeSound
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier with another Audio Clip at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,int channel,Transform targetToFollow,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, null, occludeSound);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier with another Audio Clip and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,Transform targetToFollow,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, null, occludeSound);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier with another Audio Clip at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="position">Position.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,Vector3 position,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, null, occludeSound);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier with another Audio Clip at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,int channel,Vector3 position,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, null, occludeSound);

			return _audioSource;

		}

		return null;

	}
	// // // // //

	// Normal with channel-position/transform-mixerGroup
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier with another Audio Clip using a different AudioMixerGroup at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, mixerGroup, false);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier with another Audio Clip using a different AudioMixerGroup and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,Transform targetToFollow,AudioMixerGroup mixerGroup)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, mixerGroup, false);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier with another Audio Clip using a different AudioMixerGroup at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,Vector3 position,AudioMixerGroup mixerGroup)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, mixerGroup, false);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier with another Audio Clip using a different AudioMixerGroup at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,int channel,Vector3 position,AudioMixerGroup mixerGroup)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, mixerGroup, false);

			return _audioSource;

		}

		return null;

	}
	// // // // //

	// Normal with channel-position/transform-mixerGroup-occludeSound
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier with another Audio Clip using a different AudioMixerGroup at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier with another Audio Clip using a different AudioMixerGroup and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier with another Audio Clip using a different AudioMixerGroup at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, mixerGroup, occludeSound);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier with another Audio Clip using a different AudioMixerGroup at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,int channel,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, mixerGroup, occludeSound);

			return _audioSource;

		}

		return null;

	}
	// // // // //
	#endregion

	#region PlayDelayedAudioObjectOverrideByIdentifier Method

	// Delayed with channel-position/transform
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier with another Audio Clip with a delay specified in seconds at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,int channel,Transform targetToFollow)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, null, false,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier with another Audio Clip with a delay specified in seconds and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,Transform targetToFollow)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, null, false,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier with another Audio Clip with a delay specified in seconds at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="position">Position.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,Vector3 position)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, null, false,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier with another Audio Clip with a delay specified in seconds at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,int channel,Vector3 position)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, null, false,delay);

			return _audioSource;

		}

		return null;

	}
	// // // // //


	// Delayed with channel-position/transform-occludeSound
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier with another Audio Clip with a delay specified in seconds at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,int channel,Transform targetToFollow,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, null, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier with another Audio Clip with a delay specified in seconds and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,Transform targetToFollow,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, null, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier with another Audio Clip with a delay specified in seconds at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="position">Position.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,Vector3 position,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, null, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier with another Audio Clip with a delay specified in seconds at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,int channel,Vector3 position,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, null, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	// // // // //

	// Delayed with channel-position/transform-mixerGroup
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier with another Audio Clip using a different AudioMixerGroup with a delay specified in seconds at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, mixerGroup, false,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier with another Audio Clip using a different AudioMixerGroup with a delay specified in seconds and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,Transform targetToFollow,AudioMixerGroup mixerGroup)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, mixerGroup, false,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier with another Audio Clip using a different AudioMixerGroup with a delay specified in seconds at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,Vector3 position,AudioMixerGroup mixerGroup)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, mixerGroup, false,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an <see cref="AudioObject"/>by its identifier with another Audio Clip using a different AudioMixerGroup with a delay specified in seconds at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,int channel,Vector3 position,AudioMixerGroup mixerGroup)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, mixerGroup, false,delay);

			return _audioSource;

		}

		return null;

	}
	// // // // //

	// Delayed with channel-position/transform-mixerGroup-occludeSound
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier with another Audio Clip using a different AudioMixerGroup with a delay specified in seconds at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier with another Audio Clip using a different AudioMixerGroup with a delay specified in seconds and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="targetToFollow">Target to follow.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier with another Audio Clip using a different AudioMixerGroup with a delay specified in seconds at the specified position.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio();

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, mixerGroup, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	/// <summary>
	/// Plays an Occludable <see cref="AudioObject"/>by its identifier with another Audio Clip using a different AudioMixerGroup with a delay specified in seconds at the specified position in the specified Channel.
	/// </summary>
	/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
	/// <param name="audioClipOverride">Audio clip override.</param>
	/// <param name="audioObjectIdentifier">Identifier.</param>
	/// <param name="delay">Delay.</param>
	/// <param name="channel">Channel.</param>
	/// <param name="position">Position.</param>
	/// <param name="mixerGroup">Mixer group.</param>
	/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
	public static MultiAudioSource PlayDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,int channel,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound)
	{

		if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

			MultiAudioSource _audioSource = GetPooledAudio(channel);

			RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, mixerGroup, occludeSound,delay);

			return _audioSource;

		}

		return null;

	}
	// // // // //

	#endregion

		#region FadeInAudioObject Method
		// Normal with channel-position/transform
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObject(AudioObject audioObject,int channel,Transform targetToFollow,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, audioObject, channel, false, Vector3.zero, targetToFollow, null, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObject(AudioObject audioObject,Transform targetToFollow,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, audioObject, -1, false, Vector3.zero, targetToFollow, null, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="position">Position.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObject(AudioObject audioObject,Vector3 position,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, audioObject, -1, true, position, null, null, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in at the specified Position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObject(AudioObject audioObject,int channel,Vector3 position,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, audioObject, channel, true, position, null, null, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //


		// Normal with channel-position/transform-occludeSound
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObject(AudioObject audioObject,int channel,Transform targetToFollow,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, audioObject, channel, false, Vector3.zero, targetToFollow, null, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObject(AudioObject audioObject,Transform targetToFollow,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, audioObject, -1, false, Vector3.zero, targetToFollow, null, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="position">Position.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObject(AudioObject audioObject,Vector3 position,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, audioObject, -1, true, position, null, null, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObject(AudioObject audioObject,int channel,Vector3 position,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, audioObject, channel, true, position, null, null, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //

		// Normal with channel-position/transform-mixerGroup
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in using a different AudioMixerGroup at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObject(AudioObject audioObject,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, audioObject, channel, false, Vector3.zero, targetToFollow, mixerGroup, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in using a different AudioMixerGroup and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObject(AudioObject audioObject,Transform targetToFollow,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, audioObject, -1, false, Vector3.zero, targetToFollow, mixerGroup, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in using a different AudioMixerGroup at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObject(AudioObject audioObject,Vector3 position,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, audioObject, -1, true, position, null, mixerGroup, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in using a different AudioMixerGroup at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObject(AudioObject audioObject,int channel,Vector3 position,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, audioObject, channel, true, position, null, mixerGroup, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //

		// Normal with channel-position/transform-mixerGroup-occludeSound
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in using a different AudioMixerGroup at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObject(AudioObject audioObject,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, audioObject, channel, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in using a different AudioMixerGroup and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObject(AudioObject audioObject,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, audioObject, -1, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in using a different AudioMixerGroup at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObject(AudioObject audioObject,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, audioObject, -1, true, position, null, mixerGroup, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in using a different AudioMixerGroup at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObject(AudioObject audioObject,int channel,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, audioObject, channel, true, position, null, mixerGroup, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //
		#endregion

		#region FadeInDelayedAudioObject Method

		// Delayed with channel-position/transform
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in and a delay specified in seconds at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObject(AudioObject audioObject,float delay,int channel,Transform targetToFollow,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, audioObject, channel, false, Vector3.zero, targetToFollow, null, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in and a delay specified in seconds and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObject(AudioObject audioObject,float delay,Transform targetToFollow,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, audioObject, -1, false, Vector3.zero, targetToFollow, null, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in and a delay specified in seconds at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="position">Position.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObject(AudioObject audioObject,float delay,Vector3 position,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, audioObject, -1, true, position, null, null, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in and a delay specified in seconds at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObject(AudioObject audioObject,float delay,int channel,Vector3 position,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, audioObject, channel, true, position, null, null, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //


		// Delayed with channel-position/transform-occludeSound
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in and a delay specified in seconds at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObject(AudioObject audioObject,float delay,int channel,Transform targetToFollow,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, audioObject, channel, false, Vector3.zero, targetToFollow, null, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in and a delay specified in seconds and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObject(AudioObject audioObject,float delay,Transform targetToFollow,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, audioObject, -1, false, Vector3.zero, targetToFollow, null, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in and a delay specified in seconds at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="position">Position.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObject(AudioObject audioObject,float delay,Vector3 position,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, audioObject, -1, true, position, null, null, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in and a delay specified in seconds at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObject(AudioObject audioObject,float delay,int channel,Vector3 position,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, audioObject, channel, true, position, null, null, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //

		// Delayed with channel-position/transform-mixerGroup
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in and a delay specified in seconds using a different AudioMixerGroup at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObject(AudioObject audioObject,float delay,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, audioObject, channel, false, Vector3.zero, targetToFollow, mixerGroup, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in and a delay specified in seconds using a different AudioMixerGroup and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObject(AudioObject audioObject,float delay,Transform targetToFollow,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, audioObject, -1, false, Vector3.zero, targetToFollow, mixerGroup, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in and a delay specified in seconds using a different AudioMixerGroup at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObject(AudioObject audioObject,float delay,Vector3 position,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, audioObject, -1, true, position, null, mixerGroup, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in and a delay specified in seconds using a different AudioMixerGroup at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObject(AudioObject audioObject,float delay,int channel,Vector3 position,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, audioObject, channel, true, position, null, mixerGroup, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //

		// Delayed with channel-position/transform-mixerGroup-occludeSound
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in and a delay specified in seconds using a different AudioMixerGroup at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObject(AudioObject audioObject,float delay,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, audioObject, channel, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in and a delay specified in seconds using a different AudioMixerGroup and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObject(AudioObject audioObject,float delay,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, audioObject, -1, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in and a delay specified in seconds using a different AudioMixerGroup at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObject(AudioObject audioObject,float delay,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, audioObject, -1, true, position, null, mixerGroup, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in and a delay specified in seconds using a different AudioMixerGroup at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObject(AudioObject audioObject,float delay,int channel,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, audioObject, channel, true, position, null, mixerGroup, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //

		#endregion

		#region FadeInAudioObjectOverride Method
		// Normal with channel-position/transform
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in using another Audio Clip at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,int channel,Transform targetToFollow,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, false, Vector3.zero, targetToFollow, null, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in using another Audio Clip and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,Transform targetToFollow,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, false, Vector3.zero, targetToFollow, null, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in using another Audio Clip at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="position">Position.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,Vector3 position,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, true, position, null, null, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in using another Audio Clip at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,int channel,Vector3 position,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, true, position, null, null, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //


		// Normal with channel-position/transform-occludeSound
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in using another Audio Clip at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,int channel,Transform targetToFollow,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, false, Vector3.zero, targetToFollow, null, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in using another Audio Clip and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,Transform targetToFollow,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, false, Vector3.zero, targetToFollow, null, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in using another Audio Clip at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="position">Position.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,Vector3 position,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, true, position, null, null, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in using another Audio Clip at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,int channel,Vector3 position,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, true, position, null, null, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //

		// Normal with channel-position/transform-mixerGroup
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in using another Audio Clip and a different AudioMixerGroup at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, false, Vector3.zero, targetToFollow, mixerGroup, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in using another Audio Clip and a different AudioMixerGroup and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,Transform targetToFollow,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, false, Vector3.zero, targetToFollow, mixerGroup, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in using another Audio Clip and a different AudioMixerGroup at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,Vector3 position,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, true, position, null, mixerGroup, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in using another Audio Clip and a different AudioMixerGroup at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,int channel,Vector3 position,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, true, position, null, mixerGroup, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //

		// Normal with channel-position/transform-mixerGroup-occludeSound
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in using another Audio Clip and a different AudioMixerGroup at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in using another Audio Clip and a different AudioMixerGroup and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in using another Audio Clip and a different AudioMixerGroup at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, true, position, null, mixerGroup, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in using another Audio Clip and a different AudioMixerGroup at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,int channel,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, true, position, null, mixerGroup, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //
		#endregion

		#region FadeInDelayedAudioObjectOverride Method

		// Delayed with channel-position/transform
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in using another Audio Clip with a delay specified in seconds at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,int channel,Transform targetToFollow,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, false, Vector3.zero, targetToFollow, null, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in using another Audio Clip with a delay specified in seconds and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,Transform targetToFollow,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, false, Vector3.zero, targetToFollow, null, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in using another Audio Clip with a delay specified in seconds at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="position">Position.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,Vector3 position,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, true, position, null, null, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in using another Audio Clip with a delay specified in seconds at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,int channel,Vector3 position,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, true, position, null, null, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //


		// Delayed with channel-position/transform-occludeSound
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in using another Audio Clip with a delay specified in seconds at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,int channel,Transform targetToFollow,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, false, Vector3.zero, targetToFollow, null, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in using another Audio Clip with a delay specified in seconds and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,Transform targetToFollow,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, false, Vector3.zero, targetToFollow, null, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in using another Audio Clip with a delay specified in seconds at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="position">Position.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,Vector3 position,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, true, position, null, null, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in using another Audio Clip with a delay specified in seconds at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,int channel,Vector3 position,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, true, position, null, null, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //

		// Delayed with channel-position/transform-mixerGroup
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in using another Audio Clip and a different AudioMixerGroup with a delay specified in seconds at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, false, Vector3.zero, targetToFollow, mixerGroup, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in using another Audio Clip and a different AudioMixerGroup with a delay specified in seconds and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,Transform targetToFollow,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, false, Vector3.zero, targetToFollow, mixerGroup, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in using another Audio Clip and a different AudioMixerGroup with a delay specified in seconds at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,Vector3 position,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, true, position, null, mixerGroup, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>with a fade in using another Audio Clip and a different AudioMixerGroup with a delay specified in seconds at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,int channel,Vector3 position,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, true, position, null, mixerGroup, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //

		// Delayed with channel-position/transform-mixerGroup-occludeSound
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in using another Audio Clip and a different AudioMixerGroup with a delay specified in seconds at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in using another Audio Clip and a different AudioMixerGroup with a delay specified in seconds and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in using another Audio Clip and a different AudioMixerGroup with a delay specified in seconds at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, -1, true, position, null, mixerGroup, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>with a fade in using another Audio Clip and a different AudioMixerGroup with a delay specified in seconds at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObject"><see cref="AudioObject"/>.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverride(AudioClip audioClipOverride,AudioObject audioObject,float delay,int channel,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (audioObject != null) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,audioObject, channel, true, position, null, mixerGroup, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //

		#endregion

		#region FadeInAudioObjectByIdentifier Method
		// Normal with channel-position/transform
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectByIdentifier(string audioObjectIdentifier,int channel,Transform targetToFollow,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, null, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectByIdentifier(string audioObjectIdentifier,Transform targetToFollow,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, null, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="position">Position.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectByIdentifier(string audioObjectIdentifier,Vector3 position,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, null, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in at the specified Position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectByIdentifier(string audioObjectIdentifier,int channel,Vector3 position,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, null, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //


		// Normal with channel-position/transform-occludeSound
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectByIdentifier(string audioObjectIdentifier,int channel,Transform targetToFollow,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, null, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectByIdentifier(string audioObjectIdentifier,Transform targetToFollow,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, null, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="position">Position.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectByIdentifier(string audioObjectIdentifier,Vector3 position,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, null, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectByIdentifier(string audioObjectIdentifier,int channel,Vector3 position,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, null, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //

		// Normal with channel-position/transform-mixerGroup
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in using a different AudioMixerGroup at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectByIdentifier(string audioObjectIdentifier,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, mixerGroup, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in using a different AudioMixerGroup and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectByIdentifier(string audioObjectIdentifier,Transform targetToFollow,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, mixerGroup, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in using a different AudioMixerGroup at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectByIdentifier(string audioObjectIdentifier,Vector3 position,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, mixerGroup, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in using a different AudioMixerGroup at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectByIdentifier(string audioObjectIdentifier,int channel,Vector3 position,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, mixerGroup, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //

		// Normal with channel-position/transform-mixerGroup-occludeSound
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in using a different AudioMixerGroup at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectByIdentifier(string audioObjectIdentifier,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in using a different AudioMixerGroup and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectByIdentifier(string audioObjectIdentifier,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in using a different AudioMixerGroup at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectByIdentifier(string audioObjectIdentifier,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, mixerGroup, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in using a different AudioMixerGroup at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectByIdentifier(string audioObjectIdentifier,int channel,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, mixerGroup, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //
		#endregion

		#region FadeInDelayedAudioObjectByIdentifier Method

		// Delayed with channel-position/transform
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in and a delay specified in seconds at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,int channel,Transform targetToFollow,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, null, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in and a delay specified in seconds and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,Transform targetToFollow,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, null, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in and a delay specified in seconds at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="position">Position.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,Vector3 position,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, null, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in and a delay specified in seconds at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,int channel,Vector3 position,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, null, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //


		// Delayed with channel-position/transform-occludeSound
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in and a delay specified in seconds at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,int channel,Transform targetToFollow,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, null, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in and a delay specified in seconds and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,Transform targetToFollow,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, null, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in and a delay specified in seconds at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="position">Position.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,Vector3 position,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, null, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in and a delay specified in seconds at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,int channel,Vector3 position,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, null, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //

		// Delayed with channel-position/transform-mixerGroup
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in and a delay specified in seconds using a different AudioMixerGroup at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, mixerGroup, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in and a delay specified in seconds using a different AudioMixerGroup and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,Transform targetToFollow,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, mixerGroup, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in and a delay specified in seconds using a different AudioMixerGroup at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,Vector3 position,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, mixerGroup, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in and a delay specified in seconds using a different AudioMixerGroup at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,int channel,Vector3 position,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, mixerGroup, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //

		// Delayed with channel-position/transform-mixerGroup-occludeSound
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in and a delay specified in seconds using a different AudioMixerGroup at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in and a delay specified in seconds using a different AudioMixerGroup and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in and a delay specified in seconds using a different AudioMixerGroup at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, mixerGroup, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in and a delay specified in seconds using a different AudioMixerGroup at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectByIdentifier(string audioObjectIdentifier,float delay,int channel,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObject(_audioSource, MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, mixerGroup, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //

		#endregion

		#region FadeInAudioObjectOverrideByIdentifier Method
		// Normal with channel-position/transform
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,int channel,Transform targetToFollow,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, null, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,Transform targetToFollow,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, null, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="position">Position.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,Vector3 position,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, null, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,int channel,Vector3 position,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, null, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //


		// Normal with channel-position/transform-occludeSound
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,int channel,Transform targetToFollow,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, null, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,Transform targetToFollow,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, null, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="position">Position.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,Vector3 position,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, null, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,int channel,Vector3 position,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, null, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //

		// Normal with channel-position/transform-mixerGroup
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip and a different AudioMixerGroup at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, mixerGroup, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip and a different AudioMixerGroup and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,Transform targetToFollow,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, mixerGroup, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip and a different AudioMixerGroup at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,Vector3 position,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, mixerGroup, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip and a different AudioMixerGroup at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,int channel,Vector3 position,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, mixerGroup, false,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //

		// Normal with channel-position/transform-mixerGroup-occludeSound
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip and a different AudioMixerGroup at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip and a different AudioMixerGroup and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip and a different AudioMixerGroup at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, mixerGroup, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip and a different AudioMixerGroup at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,int channel,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, mixerGroup, occludeSound,0,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //
		#endregion

		#region FadeInDelayedAudioObjectOverrideByIdentifier Method

		// Delayed with channel-position/transform
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip with a delay specified in seconds at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,int channel,Transform targetToFollow,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, null, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip with a delay specified in seconds and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,Transform targetToFollow,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, null, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip with a delay specified in seconds at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="position">Position.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,Vector3 position,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, null, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip with a delay specified in seconds at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,int channel,Vector3 position,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, null, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //


		// Delayed with channel-position/transform-occludeSound
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip with a delay specified in seconds at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,int channel,Transform targetToFollow,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, null, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip with a delay specified in seconds and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,Transform targetToFollow,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, null, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip with a delay specified in seconds at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="position">Position.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,Vector3 position,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, null, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip with a delay specified in seconds at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,int channel,Vector3 position,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, null, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //

		// Delayed with channel-position/transform-mixerGroup
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip and a different AudioMixerGroup with a delay specified in seconds at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, mixerGroup, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip and a different AudioMixerGroup with a delay specified in seconds and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,Transform targetToFollow,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, mixerGroup, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip and a different AudioMixerGroup with a delay specified in seconds at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,Vector3 position,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, mixerGroup, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip and a different AudioMixerGroup with a delay specified in seconds at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,int channel,Vector3 position,AudioMixerGroup mixerGroup,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, mixerGroup, false,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //

		// Delayed with channel-position/transform-mixerGroup-occludeSound
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip and a different AudioMixerGroup with a delay specified in seconds at the specified Channel and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,int channel,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip and a different AudioMixerGroup with a delay specified in seconds and makes that its pooled <see cref="MultiAudioSource"/>follow a target.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="targetToFollow">Target to follow.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,Transform targetToFollow,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, false, Vector3.zero, targetToFollow, mixerGroup, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip and a different AudioMixerGroup with a delay specified in seconds at the specified position.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio();

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), -1, true, position, null, mixerGroup, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		/// <summary>
		/// Plays an Occludable <see cref="AudioObject"/>by its identifier with a fade in using another Audio Clip and a different AudioMixerGroup with a delay specified in seconds at the specified position in the specified Channel.
		/// </summary>
		/// <returns>The pooled <see cref="MultiAudioSource"/>.</returns>
		/// <param name="audioClipOverride">Audio clip override.</param>
		/// <param name="audioObjectIdentifier">Identifier.</param>
		/// <param name="delay">Delay.</param>
		/// <param name="channel">Channel.</param>
		/// <param name="position">Position.</param>
		/// <param name="mixerGroup">Mixer group.</param>
		/// <param name="occludeSound">If set to <c>true</c> occlude sound.</param>
		/// <param name="fadeInTime">Fade In Time.</param>
		public static MultiAudioSource FadeInDelayedAudioObjectOverrideByIdentifier(AudioClip audioClipOverride,string audioObjectIdentifier,float delay,int channel,Vector3 position,AudioMixerGroup mixerGroup,bool occludeSound,float fadeInTime=1f)
		{

			if (!string.IsNullOrEmpty(audioObjectIdentifier)) {

				MultiAudioSource _audioSource = GetPooledAudio(channel);

				RealPlayAudioObjectOverride (_audioSource, audioClipOverride,MultiAudioManager.GetAudioObjectByIdentifier(audioObjectIdentifier), channel, true, position, null, mixerGroup, occludeSound,delay,fadeInTime);

				return _audioSource;

			}

			return null;

		}
		// // // // //

		#endregion

	#endregion

	/// <summary>
	/// Stops the <see cref="MultiAudioSource"/>playing at the specified channel.
	/// </summary>
	/// <param name="_channel">Channel.</param>
	/// <param name="disableObject">If set to <c>true</c> disable object.</param>
	public static void StopAudioSource(int _channel,bool disableObject=true)
	{

		for (int i = 0; i < MultiPoolAudioSystem.audioManager.AudioSources.Count; i++) {
			if (MultiPoolAudioSystem.audioManager.AudioSources[i].isActiveAndEnabled && MultiPoolAudioSystem.audioManager.AudioSources[i].Channel==_channel) {
					MultiPoolAudioSystem.audioManager.AudioSources [i].Stop(disableObject);
			}
		}

	}

	/// <summary>
	/// Stops the specified <see cref="MultiAudioSource"/>.
	/// </summary>
	/// <param name="audioSource">Multi Audio source.</param>
	/// <param name="disableObject">If set to <c>true</c> disable object.</param>
	public static void StopAudioSource(MultiAudioSource audioSource,bool disableObject=true)
	{

		audioSource.Stop (disableObject);

	}

		/// <summary>
		/// Pauses/Unpauses the <see cref="MultiAudioSource"/>playing at the specified channel.
		/// </summary>
		/// <param name="_channel">Channel.</param>
		/// <param name="pause">If set to <c>true</c> pauses the source.</param>
		public static void PauseAudioSource(int _channel,bool pause=true)
		{

			for (int i = 0; i < MultiPoolAudioSystem.audioManager.AudioSources.Count; i++) {
				if (MultiPoolAudioSystem.audioManager.AudioSources[i].isActiveAndEnabled && MultiPoolAudioSystem.audioManager.AudioSources[i].Channel==_channel) {
					MultiPoolAudioSystem.audioManager.AudioSources [i].LocalPause=pause;
				}
			}

		}

		/// <summary>
		/// Pauses/Unpauses the specified <see cref="MultiAudioSource"/>.
		/// </summary>
		/// <param name="audioSource">Multi Audio source.</param>
		/// <param name="pause">If set to <c>true</c> pauses the source.</param>
		public static void PauseAudioSource(MultiAudioSource audioSource,bool pause=true)
		{

			audioSource.LocalPause=pause;

		}

		/// <summary>
		/// Stops the <see cref="MultiAudioSource"/>playing at the specified channel with a fade out.
		/// </summary>
		/// <param name="_channel">Channel.</param>
		/// <param name="fadeOutTime">Fade out time.</param>
		/// <param name="disableObject">If set to <c>true</c> disable object.</param>
		public static void FadeOutAudioSource(int _channel,float fadeOutTime=1f,bool disableObject=true)
		{
			bool finded = false;
			for (int i = 0; i < MultiPoolAudioSystem.audioManager.AudioSources.Count; i++) {
				if (!finded && MultiPoolAudioSystem.audioManager.AudioSources[i].isActiveAndEnabled && MultiPoolAudioSystem.audioManager.AudioSources[i].Channel==_channel) {
					MultiPoolAudioSystem.audioManager.AudioSources [i].FadeOut(fadeOutTime,disableObject);
					finded=true;
				}
			}

		}

		/// <summary>
		/// Stops the specified <see cref="MultiAudioSource"/>with a fade out.
		/// </summary>
		/// <param name="audioSource">Multi Audio source.</param>
		/// <param name="fadeOutTime">Fade out time.</param>
		/// <param name="disableObject">If set to <c>true</c> disable object.</param>
		public static void FadeOutAudioSource(MultiAudioSource audioSource,float fadeOutTime=1f,bool disableObject=true)
		{

			audioSource.FadeOut(fadeOutTime,disableObject);

		}

	/// <summary>
	/// Stops all playing Multi Audio Sources.
	/// </summary>
	public static void StopAllAudioSources()
	{

		for (int i = 0; i < MultiPoolAudioSystem.audioManager.AudioSources.Count; i++) {
			if (MultiPoolAudioSystem.audioManager.AudioSources[i].isActiveAndEnabled) {
				MultiPoolAudioSystem.audioManager.AudioSources [i].Stop();
			}
		}

	}

	/// <summary>
	/// Stops all Multi Audio Sources marked as BGM audios.
	/// </summary>
	public static void StopAllBGMAudios()
	{

		for (int i = 0; i < MultiPoolAudioSystem.audioManager.AudioSources.Count; i++) {
			if (MultiPoolAudioSystem.audioManager.AudioSources[i].isActiveAndEnabled && MultiPoolAudioSystem.audioManager.AudioSources[i].AudioObject.isBGM) {
				MultiPoolAudioSystem.audioManager.AudioSources [i].Stop();
			}
		}

	}

	/// <summary>
	/// Stops all Multi Audio Sources not marked as BGM audios.
	/// </summary>
	public static void StopAllNonBGMAudios()
	{

		for (int i = 0; i < MultiPoolAudioSystem.audioManager.AudioSources.Count; i++) {
			if (MultiPoolAudioSystem.audioManager.AudioSources[i].isActiveAndEnabled && !MultiPoolAudioSystem.audioManager.AudioSources[i].AudioObject.isBGM) {
				MultiPoolAudioSystem.audioManager.AudioSources [i].Stop();
			}
		}

	}

	/// <summary>
	/// Stops all looped Multi Audio Sources.
	/// </summary>
	public static void StopAllLoopedAudioSources()
	{

		for (int i = 0; i < MultiPoolAudioSystem.audioManager.AudioSources.Count; i++) {
			if (MultiPoolAudioSystem.audioManager.AudioSources[i].isActiveAndEnabled && MultiPoolAudioSystem.audioManager.AudioSources[i].Loop) {
				MultiPoolAudioSystem.audioManager.AudioSources [i].Stop();
			}
		}

	}

		/// <summary>
		/// Stops all persistent Multi Audio Sources.
		/// </summary>
		public static void StopAllPersistentAudioSources()
		{

			for (int i = 0; i < MultiPoolAudioSystem.audioManager.AudioSources.Count; i++) {
				if (MultiPoolAudioSystem.audioManager.AudioSources[i].isActiveAndEnabled && MultiPoolAudioSystem.audioManager.AudioSources[i].PersistsBetweenScenes) {
					MultiPoolAudioSystem.audioManager.AudioSources [i].Stop();
				}
			}

		}
			
	private static MultiAudioSource GetPooledAudio(int _channel=-1)
	{

			if (MultiAudioManager.Instance.maxAudioSources == 0)
				Debug.LogError ("maxAudioSources can't be 0",MultiAudioManager.Instance.gameObject);


			bool finded = false;

			MultiAudioSource toReturn = MultiPoolAudioSystem.audioManager.AudioSources [0];

			for (int i = 0; i < MultiPoolAudioSystem.audioManager.AudioSources.Count; i++) {

				if (!finded && MultiPoolAudioSystem.audioManager.AudioSources [i].InsidePool && !MultiPoolAudioSystem.audioManager.AudioSources [i].isActiveAndEnabled || MultiPoolAudioSystem.audioManager.AudioSources [i].InsidePool && MultiPoolAudioSystem.audioManager.AudioSources [i].isActiveAndEnabled && MultiPoolAudioSystem.audioManager.AudioSources [i].Channel == _channel && _channel > -1) {
					toReturn = MultiPoolAudioSystem.audioManager.AudioSources [i];
					finded = true;
				}
			}

			init = true;

			if (finded) {
				toReturn.SessionIndex = sessionIndex;
				return toReturn;
			} else {

				Debug.LogWarning ("Not enought pooled audio sources. Returning the first pooled audio source", MultiAudioManager.instance.gameObject);

				MultiPoolAudioSystem.audioManager.AudioSources [0].SessionIndex = sessionIndex;
				return MultiPoolAudioSystem.audioManager.AudioSources [0];
			
			}

	}

	/// <summary>
	/// Gets the <see cref="MultiAudioSource"/>played at the specified Channel (returns NULL if there is no <see cref="MultiAudioSource"/>playing at the specified Channel).
	/// </summary>
	/// <returns>The <see cref="MultiAudioSource"/>played at the specified Channel.</returns>
	/// <param name="_channel">Channel.</param>
	public static MultiAudioSource GetAudioSourceAtChannel(int _channel)
	{

			if (_channel > -1) {

				if (MultiAudioManager.Instance.maxAudioSources == 0)
					Debug.LogError ("maxAudioSources can't be 0",MultiAudioManager.Instance.gameObject);

				MultiAudioSource toReturn = null;

				for (int i = 0; i < MultiPoolAudioSystem.audioManager.AudioSources.Count; i++) {
					if (MultiPoolAudioSystem.audioManager.AudioSources [i].isActiveAndEnabled && MultiPoolAudioSystem.audioManager.AudioSources [i].Channel == _channel) {
						toReturn = MultiPoolAudioSystem.audioManager.AudioSources [i];
					}
				}

				return toReturn;

			} else {

				Debug.LogWarning ("<b>GetAudioSourceAtChannel</b><i>(int _channel)</i> has an invalid channel index, returning NULL");
				return null;

			}

	}

	private static bool applicationIsQuitting = false;

	public void OnDestroy () {
		applicationIsQuitting = true;
	}

		#if UNITY_EDITOR
	void OnDrawGizmos()
	{

		if (Application.isPlaying && MultiPoolAudioSystem.audioManager.AudioSources!=null) {
			for (int i = 0; i < MultiPoolAudioSystem.audioManager.AudioSources.Count; i++) {

				if (MultiPoolAudioSystem.audioManager.AudioSources [i].InsidePool && MultiPoolAudioSystem.audioManager.AudioSources [i].isActiveAndEnabled) {
					Gizmos.color = Color.blue;
					Gizmos.DrawIcon (MultiPoolAudioSystem.audioManager.AudioSources [i].transform.position, audioSourcePlayingIconStr);
						DrawAudioObjectName(MultiPoolAudioSystem.audioManager.AudioSources [i].AudioObject.name,MultiPoolAudioSystem.audioManager.AudioSources [i].transform.position-Vector3.up*0.35f);
				}

			}
		}

	}

		static void DrawAudioObjectName(string text, Vector3 worldPos, Color? colour = null) {
			if (SceneView.lastActiveSceneView!=null && SceneView.lastActiveSceneView.camera!=null && Vector3.Distance(worldPos,SceneView.lastActiveSceneView.camera.transform.position)<15f && UnityEditor.SceneView.currentDrawingSceneView!=null && UnityEditor.SceneView.currentDrawingSceneView.camera!=null)
			{
				UnityEditor.Handles.BeginGUI ();

				var restoreColor = GUI.color;

				if (colour.HasValue)
					GUI.color = colour.Value;
				var view = UnityEditor.SceneView.currentDrawingSceneView;
				Vector3 screenPos = view.camera.WorldToScreenPoint (worldPos);

				if (screenPos.y < 0 || screenPos.y > Screen.height || screenPos.x < 0 || screenPos.x > Screen.width || screenPos.z < 0) {
					GUI.color = restoreColor;
					UnityEditor.Handles.EndGUI ();
					return;
				}

				Vector2 size = GUI.skin.label.CalcSize (new GUIContent (text));
				GUI.Label (new Rect (screenPos.x - (size.x / 2), -screenPos.y + view.position.height + 2, size.x, size.y), text);
				GUI.color = restoreColor;
				UnityEditor.Handles.EndGUI ();
			}
		}
		#endif

	void OnDrawGizmosSelected()
	{

		Gizmos.DrawIcon (transform.position, "AudioManagerIco");

	}


	#if UNITY_EDITOR

	[CustomEditor(typeof(MultiAudioManager))]
	public class MultiAudioManagerEditor : Editor
	{

		private Texture logoTex;
		private int specifiedChannel=-1;

		[MenuItem("Almenara Games/Multi Listener Pooling Audio System Config")]
		static void SSConfig()
		{

			GameObject configGo = Resources.Load ("Multi Listener Pooling Audio System Config") as GameObject;

			#if UNITY_EDITOR
			if (configGo == null) {

				PrefabUtility.CreatePrefab("Assets/AlmenaraGames/MLPAS/Resources/Multi Listener Pooling Audio System Config.prefab",new GameObject("Multi Listener Pooling Audio System Config",typeof(AlmenaraGames.Tools.MultiAudioManagerConfig)));

				Debug.LogWarning ("<b>Multi Listener Pooling Audio System Config</b> is missing, a <i>New One</i> has been created", Resources.Load ("Multi Listener Pooling Audio System Config"));

			}
			#endif

			if (GameObject.Find ("Multi Listener Pooling Audio System Config")) {

				DestroyImmediate (GameObject.Find ("Multi Listener Pooling Audio System Config"));

			}

			configGo = Resources.Load ("Multi Listener Pooling Audio System Config") as GameObject;


			Selection.activeGameObject = configGo;

		}

			[MenuItem("GameObject/Almenara Games/MLPAS/Multi Audio Source",false,+4)]
			static void CreateMultiAudioSource()
			{

				GameObject multiAudioSourceGo = new GameObject("Multi Audio Source",typeof(MultiAudioSource));
				if (SceneView.lastActiveSceneView!=null && SceneView.lastActiveSceneView.camera) {
					multiAudioSourceGo.transform.position= SceneView.lastActiveSceneView.camera.transform.position + (SceneView.lastActiveSceneView.camera.transform.forward * 10f);
				}
				if (Selection.activeTransform != null) {
					multiAudioSourceGo.transform.parent = Selection.activeTransform;
					multiAudioSourceGo.transform.localPosition = Vector3.zero;
				}
				Selection.activeGameObject = multiAudioSourceGo;

				if (multiAudioSourceGo.name == "Multi Audio Source" && GameObject.Find ("Multi Audio Source")!=null && GameObject.Find ("Multi Audio Source")!=multiAudioSourceGo) {

					for (int i = 1; i < 999; i++) {

						if (GameObject.Find ("Multi Audio Source (" + i.ToString () + ")") == null) {

							multiAudioSourceGo.name = "Multi Audio Source (" + i.ToString () + ")";
							break;

						}
						
					}

				}

			}

			[MenuItem("GameObject/Almenara Games/MLPAS/Multi Audio Listener",false,+4)]
			static void CreateMultiAudioListener()
			{

				GameObject multiAudioListenerGo = new GameObject("Multi Audio Listener",typeof(MultiAudioListener));
				if (SceneView.lastActiveSceneView!=null && SceneView.lastActiveSceneView.camera) {
					multiAudioListenerGo.transform.position= SceneView.lastActiveSceneView.camera.transform.position + (SceneView.lastActiveSceneView.camera.transform.forward * 10f);
				}
				if (Selection.activeTransform != null) {
					multiAudioListenerGo.transform.parent = Selection.activeTransform;
					multiAudioListenerGo.transform.localPosition = Vector3.zero;
				}
				Selection.activeGameObject = multiAudioListenerGo;

				if (multiAudioListenerGo.name == "Multi Audio Listener" && GameObject.Find ("Multi Audio Listener")!=null && GameObject.Find ("Multi Audio Listener")!=multiAudioListenerGo) {

					for (int i = 1; i < 999; i++) {

						if (GameObject.Find ("Multi Audio Listener (" + i.ToString () + ")") == null) {

							multiAudioListenerGo.name = "Multi Audio Listener (" + i.ToString () + ")";
							break;

						}

					}

				}

			}

		void OnEnable()
		{

			logoTex = Resources.Load ("Images/logoSmall") as Texture;

		}


		public override void OnInspectorGUI()
		{

				if (target.name == "MultiAudioManager") {
					GUILayout.Space (10f);

					var centeredStyle = new GUIStyle (GUI.skin.GetStyle ("Label"));
					centeredStyle.alignment = TextAnchor.UpperCenter;

					GUILayout.Label (logoTex, centeredStyle);

					GUILayout.Space (10f);

					if (GUILayout.Button ("Stop All Audio Sources")) {
						MultiAudioManager.StopAllAudioSources ();
					}

					if (GUILayout.Button ("Stop All Looped Sources")) {
						MultiAudioManager.StopAllLoopedAudioSources ();
					}

					GUILayout.Space (10f);

					specifiedChannel = EditorGUILayout.IntField (specifiedChannel);

			
					if (GUILayout.Button (specifiedChannel > -1 ? "Stop Audio Source at Channel " + specifiedChannel.ToString () : "Stop Audio Sources with no Channel")) {
						MultiAudioManager.StopAudioSource (specifiedChannel);
					}



					GUILayout.Space (10f);

					DrawDefaultInspector ();
				}

				else {
					GUILayout.Space (10f);

					var centeredMiniStyle = new GUIStyle (EditorStyles.miniLabel);
					centeredMiniStyle.alignment = TextAnchor.MiddleCenter;
					EditorGUILayout.LabelField ("Don't add this component manually to a Game Object", centeredMiniStyle);
				}
			

		}

	}
	#endif

}
}