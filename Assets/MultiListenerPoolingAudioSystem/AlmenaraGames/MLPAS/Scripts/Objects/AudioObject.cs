using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using System.Reflection;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AlmenaraGames {
	[CreateAssetMenu(fileName = "New Audio Object", menuName = "Multi Listener Pooling Audio System/Audio Object")]
	[HelpURL("https://drive.google.com/uc?&id=1wSFIUL0gqVL2Gan0qgB_EZdYGHwa9F_p#page=8")]
public class AudioObject : ScriptableObject {

		[Tooltip("The identifier to get this Audio Object.\nRemember that the Audio Object needs to be in the \"Resources\\Global Audio Objects\" folder in order to be accessed via this identifier")]
	/// <summary>
	/// The identifier for get this Audio Object.
	/// </summary>
	public string identifier=string.Empty;

	/// <summary>
	/// Gets or sets the clips.
	/// </summary>
	/// <value>The clips.</value>
	public AudioClip[] clips=new AudioClip[0];

	/// <summary>
	/// Returns a random clip.
	/// </summary>
	/// <value>Random Clip.</value>
	public AudioClip RandomClip
	{
		get
		{
			//Some other code
			return clips.Length>0?clips[Random.Range(0,clips.Length)]:null;
		}
	}

	[Space(10)]

	[Tooltip("Sets the overall volume of the sound")]
	[Range(0,1)]
	/// <summary>
	/// The overall volume of the sound.
	/// </summary>
	public float volume = 1;
	[Space(5)]

	[Tooltip("Sets the spread of a 3d sound in speaker space")]
	[Range(0,360)]
	/// <summary>
	/// The spread of a 3d sound in speaker space.
	/// </summary>
	public float spread = 0f;

	[Space(5)]

	[Tooltip("Sets the frequency of the sound. Use this to slow down or speed up the sound. A negative pitch value will going to make the sound plays backwards")]
	[Range(-3,3)]
	/// <summary>
	/// The frequency of the sound. Use this to slow down or speed up the sound. A negative pitch value will going to make the sound plays backwards.
	/// </summary>
	public float pitch = 1;

	[Space(5)]

	[Tooltip("A 2D sound ignores all types of spatial attenuation, thats include spatial position and spread")]
	/// <summary>
	/// A 2D sound ignores all types of spatial attenuation, thats include spatial position and spread.
	/// </summary>
	public bool spatialMode2D = false;

	[Space(5)]

	[Tooltip("Only for 2D sound")]
	[Range(-1,1)]
	/// <summary>
	/// The 2D Stereo pan.
	/// </summary>
	public float stereoPan = 0;

	[Space(10)]
	[Tooltip("Sets the source to loop")]
	/// <summary>
	/// Is the audio clip looping?. If you disable looping on a playing source the sound will stop after the end of the current loop.
	/// </summary>
	public bool loop = false;
	[Tooltip("The source will going to automatically changes the current clip to the next clip on the list at the end of the current loop")]
	/// <summary>
	/// The source will going to automatically changes the current clip to the next clip on the list at the end of the current loop.
	/// </summary>
	public bool loopClipsSequentially = false;

	[Space(10)]

	[Tooltip("Sets the priority of the sound. Note that a sound with a larger priority value will more likely be stolen by sounds with smaller priority values")]
	[Range(0,256)]
	/// <summary>
	/// The priority of the sound. Note that a sound with a larger priority value will more likely be stolen by sounds with smaller priority values.
	/// </summary>
	public int priority = 128;

	[Space(10)]

	[Tooltip("Withing the Min Distance, the volume will stay at the loudest possible. Outside of this Min Distance it begins to attenuate")]
	/// <summary>
	/// Withing the Min Distance, the volume will stay at the loudest possible. Outside of this Min Distance it begins to attenuate.
	/// </summary>
	public float minDistance = 1;
	[Tooltip("Max Distance is the distance where the sound is completely inaudible")]
	/// <summary>
	/// Max Distance is the distance where the sound is completely inaudible.
	/// </summary>
	public float maxDistance = 20;

	[Space(10)]

	[Tooltip("Min random value for multiply the pitch of the sound")]
	[Range(0.75f,1)]
	/// <summary>
	/// Min random value for multiply the pitch of the sound.
	/// </summary>
	public float minPitchMultiplier = 0.9f;
	[Tooltip("Max random value for multiply the pitch of the sound")]
	[Range(1,1.25f)]
	/// <summary>
	/// Max random value for multiply the pitch of the sound.
	/// </summary>
	public float maxPitchMultiplier = 1.1f;

	[Space(10)]

	[Tooltip("Min random value for multiply the volume of the sound")]
	[Range(0.25f,1)]
	/// <summary>
	/// Min random value for multiply the volume of the sound.
	/// </summary>
	public float minVolumeMultiplier = 0.8f;
	[Tooltip("Max random value for multiply the volume of the sound")]
	[Range(1,1.75f)]
	/// <summary>
	/// Max random value for multiply the volume of the sound.
	/// </summary>
	public float maxVolumeMultiplier = 1.2f;

	[Space(10)]

	[Tooltip("The amount by which the signal from the sound will be mixed into the global reverb associated with the Reverb from the listeners. The range from 0 to 1 is linear (like the volume property) while the range from 1 to 1.1 is an extra boost range that allows you to boost the reverberated signal by 10 dB")]
	[Range(0,1.1f)]
	/// <summary>
	/// The amount by which the signal from the sound will be mixed into the global reverb associated with the Reverb from the listeners. The range from 0 to 1 is linear (like the volume property) while the range from 1 to 1.1 is an extra boost range that allows you to boost the reverberated signal by 10 dB.
	/// </summary>
	public float reverbZoneMix = 1;

	[Space(10)]
	[Tooltip("Specifies how much the pitch is changed based on the relative velocity between the listener and the source")]
	[Range(0,5f)]
	/// <summary>
	/// Specifies how much the pitch is changed based on the relative velocity between the listener and the source.
	/// </summary>
	public float dopplerLevel = 0.25f;

	[Space(10)]
	[Tooltip("Enables or disables sound attenuation over distance")]
	/// <summary>
	/// Enables or disables sound attenuation over distance.
	/// </summary>
	public bool volumeRolloff = true;
	[Tooltip("Sets how the sound attenuates over distance")]
	/// <summary>
	/// The volume rolloff curve. Sets how the sound attenuates over distance.
	/// </summary>
	public AnimationCurve volumeRolloffCurve=AnimationCurve.EaseInOut(0,0,1,1);

	[Space(10)]

	[Tooltip("Set whether the sound should play through an Audio Mixer first or directly to the Listener. Leave NULL to use the default SFX/BGM output specified in the <b>Multi Listener Pooling Audio System Config</b>.")]
	/// <summary>
	/// Set whether the sound should play through an Audio Mixer first or directly to the Listener.
	/// </summary>
	public AudioMixerGroup mixerGroup;

	[Space(10)]
	[Tooltip("Enables or disables custom spatialization for the source")]
	/// <summary>
	/// Enables or disables custom spatialization for the source.
	/// </summary>
	public bool spatialize;

	[Space(10)]
	[Tooltip("is the sound a BGM?")]
	/// <summary>
	/// Is the sound a BGM?.
	/// </summary>
	public bool isBGM = false;


	#if UNITY_EDITOR
	[InitializeOnLoad]
	[CustomEditor(typeof(AudioObject))]
	public class AudioObjectEditor : Editor
	{

		SerializedObject audioObj;
		SerializedProperty _clips;
		SerializedProperty rollOffCurve;
		SerializedProperty minPitchMultiplier;
		SerializedProperty maxPitchMultiplier;
		SerializedProperty minVolumeMultiplier;
		SerializedProperty maxVolumeMultiplier;


		private Texture playIcon;
		private Texture stopIcon;
		private Texture addIcon;
		private Texture removeIcon;
		private Texture emptyIcon;

		private static bool unfolded=true;
		int currentPickerWindow=-1 ;

		private static readonly string[] _dontIncludeMe_1 = new string[]{"m_Script","clips","minVolumeMultiplier","maxVolumeMultiplier","reverbZoneMix","dopplerLevel","volumeRolloff","volumeRolloffCurve","mixerGroup","spatialize","isBGM"};
		private static readonly string[] _dontIncludeMe_2 = new string[]{"m_Script","clips","identifier","volume","spread","pitch","spatialMode2D","stereoPan","loop","loopClipsSequentially","priority","minDistance","maxDistance","minPitchMultiplier","maxPitchMultiplier","reverbZoneMix","dopplerLevel","volumeRolloff","volumeRolloffCurve","mixerGroup","spatialize","isBGM"};
		private static readonly string[] _dontIncludeMe_3 = new string[]{"m_Script","clips","identifier","volume","spread","pitch","spatialMode2D","stereoPan","loop","loopClipsSequentially","priority","minDistance","maxDistance","minPitchMultiplier","maxPitchMultiplier","minVolumeMultiplier","maxVolumeMultiplier","mixerGroup","spatialize","isBGM"};
		private static readonly string[] _dontIncludeMe_4 = new string[]{"m_Script","clips","identifier","volume","spread","pitch","spatialMode2D","stereoPan","loop","loopClipsSequentially","priority","minDistance","maxDistance","minPitchMultiplier","maxPitchMultiplier","minVolumeMultiplier","maxVolumeMultiplier","reverbZoneMix","dopplerLevel","volumeRolloff","volumeRolloffCurve"};

		private object[] droppedObjects;

		void OnEnable()
		{
			audioObj = new SerializedObject (target);

			_clips = audioObj.FindProperty("clips");

			rollOffCurve = audioObj.FindProperty ("volumeRolloffCurve");

			minPitchMultiplier = audioObj.FindProperty ("minPitchMultiplier");
			maxPitchMultiplier = audioObj.FindProperty ("maxPitchMultiplier");
			minVolumeMultiplier = audioObj.FindProperty ("minVolumeMultiplier");
			maxVolumeMultiplier = audioObj.FindProperty ("maxVolumeMultiplier");

			playIcon = Resources.Load ("Images/playIcon") as Texture;
			stopIcon = Resources.Load ("Images/pauseIcon") as Texture;
			addIcon = Resources.Load ("Images/addIcon") as Texture;
			removeIcon = Resources.Load ("Images/removeIcon") as Texture;
			emptyIcon = Resources.Load ("Images/emptyIcon") as Texture;


		}
		
		private static bool _isRegistered = false;
		private static bool _didSelectionChange = false;

		private static Object prevSelection;

		private void OnSelectionChanged()
		{
			_didSelectionChange = true;
		}

		private void OnEditorUpdate()
		{

			if (Selection.activeObject!=null)
			prevSelection = Selection.activeObject;
			
			if (_didSelectionChange) {
				_didSelectionChange = false;

			
				if (prevSelection!=null && prevSelection.GetType()==typeof(AudioObject))
				StopAllClips ();

			}
				

		}

		public override void OnInspectorGUI()
		{

			audioObj.Update();

			if ( !_isRegistered )
			{
				_isRegistered = true;

				Selection.selectionChanged += OnSelectionChanged;
				EditorApplication.update += OnEditorUpdate;
			}

			GUILayout.Space (15f);

			var centeredStyle = GUI.skin.GetStyle("Button");
			centeredStyle.alignment = TextAnchor.MiddleCenter;
			centeredStyle.stretchWidth = true;

			GUIStyle playButton = EditorStyles.toolbarButton;
			playButton.stretchWidth = false;

			unfolded = EditorGUILayout.Foldout (unfolded, "Audio Clips");

			if (unfolded) {

				GUILayout.BeginVertical (EditorStyles.helpBox);
				GUILayout.BeginHorizontal ();


				currentPickerWindow = EditorGUIUtility.GetControlID(FocusType.Passive) + 10;

				if (GUILayout.Button (addIcon, playButton)) {
				
					_clips.InsertArrayElementAtIndex (_clips.arraySize);

					EditorGUIUtility.ShowObjectPicker<AudioClip>(null, false, "", currentPickerWindow);

				}

				if (Event.current.commandName == "ObjectSelectorUpdated" && EditorGUIUtility.GetObjectPickerControlID() == currentPickerWindow) {

					currentPickerWindow = -1;

					AudioClip newAudioClip = EditorGUIUtility.GetObjectPickerObject() as AudioClip;

						_clips.GetArrayElementAtIndex (_clips.arraySize - 1).objectReferenceValue = newAudioClip;

				}

				GUILayout.Label ("Add Audio Clip", EditorStyles.miniLabel);

				GUILayout.EndHorizontal ();
				for (int i = 0; i < _clips.arraySize; i++) {

					bool hasClip = _clips.GetArrayElementAtIndex(i).objectReferenceValue != null;

		
						GUILayout.BeginHorizontal ();

					if (GUILayout.Button (removeIcon, playButton)) {

							StopAllClips ();

						_clips.GetArrayElementAtIndex (i).objectReferenceValue=null;
						_clips.DeleteArrayElementAtIndex (i);

					}
						
					if (hasClip) {
						if (GUILayout.Button (hasClip ? playIcon : emptyIcon, playButton)) {
							if (hasClip) {
								StopAllClips ();
								PlayClip (_clips.GetArrayElementAtIndex (i).objectReferenceValue as AudioClip);
							}
						}
						if (GUILayout.Button (hasClip ? stopIcon : emptyIcon, playButton)) {
							if (hasClip) {
								StopClip (_clips.GetArrayElementAtIndex (i).objectReferenceValue as AudioClip);
							}
						}
					}
						

					if (_clips.arraySize > i) {
						_clips.GetArrayElementAtIndex (i).objectReferenceValue = EditorGUILayout.ObjectField (_clips.GetArrayElementAtIndex (i).objectReferenceValue, typeof(AudioClip), false);
					}
						

						GUILayout.EndHorizontal ();
		
				}

				droppedObjects = DropZone ();

				if (droppedObjects!=null && droppedObjects.Length > 0) {
					foreach (var item in droppedObjects) {

						if (item.GetType () == typeof(AudioClip)) {

							_clips.InsertArrayElementAtIndex (_clips.arraySize);
							_clips.GetArrayElementAtIndex (_clips.arraySize-1).objectReferenceValue=item as AudioClip;

						}

					}
				}
					
				GUILayout.EndVertical ();

			}

			GUILayout.Space (10f);


				DrawPropertiesExcluding(audioObj, _dontIncludeMe_1);

				if (GUILayout.Button ("Disable Random Pitch Multiplier at Start",EditorStyles.miniButton)) {

					minPitchMultiplier.floatValue = 1;
					maxPitchMultiplier.floatValue = 1;

				}

				DrawPropertiesExcluding(audioObj, _dontIncludeMe_2);

				if (GUILayout.Button ("Disable Random Volume Multiplier at Start",EditorStyles.miniButton)) {

					minVolumeMultiplier.floatValue = 1;
					maxVolumeMultiplier.floatValue = 1;

				}

				DrawPropertiesExcluding(audioObj, _dontIncludeMe_3);


			if (GUILayout.Button ("Use Logarithmic Rolloff Curve",EditorStyles.miniButton)) {
				
				rollOffCurve.animationCurveValue = new AnimationCurve(new Keyframe[]{new Keyframe(0,0,0,0),new Keyframe(0.2f,0.015f,0.09f,0.09f),new Keyframe(0.6f,0.1f,0.3916f,0.3916f),new Keyframe(0.8f,0.25f,1.33f,1.33f),new Keyframe(0.9f,0.5f,5f,5f),new Keyframe(0.95f,1f,14.26f,14.26f) });

			}

				DrawPropertiesExcluding(audioObj, _dontIncludeMe_4);


			audioObj.ApplyModifiedProperties();

			if (target!=null)
				target.name=Path.GetFileName (AssetDatabase.GetAssetPath (target)).Replace(".asset","");

		}

		public static void PlayClip(AudioClip clip)
		{

			Assembly assembly = typeof(AudioImporter).Assembly;
			System.Type audioUtilType = assembly.GetType("UnityEditor.AudioUtil");

			System.Type[] typeParams = { typeof(AudioClip), typeof(int), typeof(bool) };
			object[] objParams = { clip, 0, false };

			MethodInfo method = audioUtilType.GetMethod("PlayClip", typeParams);
			method.Invoke(null, BindingFlags.Static | BindingFlags.Public, null, objParams, null);
		}

		public static void StopClip(AudioClip clip) {
			Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
			System.Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
			MethodInfo method = audioUtilClass.GetMethod(
				"StopClip",
				BindingFlags.Static | BindingFlags.Public,
				null,
				new System.Type[] {
					typeof(AudioClip)
				},
				null
			);
			method.Invoke(
				null,
				new object[] {
					clip
				}
			);
		}

		public static void StopAllClips () {
			Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
			System.Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
			MethodInfo method = audioUtilClass.GetMethod(
				"StopAllClips",
				BindingFlags.Static | BindingFlags.Public
			);

			method.Invoke(
				null,
				null
			);
		}

		public static object[] DropZone(){

			EventType eventType = Event.current.type;
			bool isAccepted = false;
		
			if (eventType == EventType.DragUpdated || eventType == EventType.DragPerform){
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

				if (eventType == EventType.DragPerform) {
					DragAndDrop.AcceptDrag();
					isAccepted = true;
				}
				Event.current.Use();
			}

			return isAccepted ? DragAndDrop.objectReferences : null;
		}

	}
	#endif

}
}
