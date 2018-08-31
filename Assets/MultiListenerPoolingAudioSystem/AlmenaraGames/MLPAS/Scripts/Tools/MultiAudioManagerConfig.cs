using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AlmenaraGames.Tools
{
	[HelpURL("https://drive.google.com/uc?&id=1wSFIUL0gqVL2Gan0qgB_EZdYGHwa9F_p#page=2")]
	[AddComponentMenu("")]
public class MultiAudioManagerConfig : MonoBehaviour {

	[Tooltip("Default SFX Mixer Group Output")]
	public AudioMixerGroup sfxMixerGroup;
	[Tooltip("Default BGM Mixer Group Output")]
	public AudioMixerGroup bgmMixerGroup;
	[Tooltip("Layer Mask used for check whether or not a collider occludes the sound. Tip: Use a unique layer for the occludable colliders, then you can have more control putting invisible triggers on the objects that you want to occludes sound")]
	public LayerMask occludeCheck = ~0;
	[Tooltip("The higher the value, the less the audio is heard when occluded")]
	public float occludeMultiplier=0.5f;
	[Tooltip("Max pooled Multi Audio Sources")]
	public int maxAudioSources=512;

	

	#if UNITY_EDITOR

	[CustomEditor(typeof(MultiAudioManagerConfig))]
	public class MultiAudioManagerConfigEditor : Editor
	{

			SerializedObject configObj;

			private Texture logoTex;
			private static readonly string[] _dontIncludeMe = new string[]{"m_Script"};

		void OnEnable()
		{
				configObj = new SerializedObject (target);
				logoTex = Resources.Load ("Images/logoSmall") as Texture;
		}

		public override void OnInspectorGUI()
		{

				if (target.name == "Multi Listener Pooling Audio System Config") {
					configObj.Update ();

					GUILayout.Space (10f);

					var centeredStyle = new GUIStyle (GUI.skin.GetStyle ("Label"));
					centeredStyle.alignment = TextAnchor.UpperCenter;

					GUILayout.Label (logoTex, centeredStyle);

					GUILayout.Space (10f);

					DrawPropertiesExcluding (configObj, _dontIncludeMe);

					configObj.ApplyModifiedProperties ();
				}

				else {
					GUILayout.Space (10f);

					var centeredMiniStyle = new GUIStyle (EditorStyles.miniLabel);
					centeredMiniStyle.alignment = TextAnchor.MiddleCenter;
					EditorGUILayout.LabelField ("Don't add this component to any Game Object", centeredMiniStyle);
				}

		}

	}
	#endif

}

}
