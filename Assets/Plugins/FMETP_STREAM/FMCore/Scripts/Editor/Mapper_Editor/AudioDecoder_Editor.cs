using System;
using UnityEngine;
using UnityEditor;
using FMSolution.Editor;

namespace FMSolution.FMETP
{
    [CustomEditor(typeof(AudioDecoder))]
    [CanEditMultipleObjects]
    public class AudioDecoder_Editor : UnityEditor.Editor
    {
        private AudioDecoder ADecoder;
        SerializedProperty labelProp;

        SerializedProperty GZipModeProp;
        SerializedProperty volumeProp;

        SerializedProperty SourceFormatDectionProp;
        SerializedProperty SourceSampleRateProp;
        SerializedProperty SourceChannelsProp;

        SerializedProperty OnPCMFloatReadyEventProp;

        void OnEnable()
        {
            GZipModeProp = serializedObject.FindProperty("GZipMode");

            labelProp = serializedObject.FindProperty("label");
            volumeProp = serializedObject.FindProperty("volume");

            SourceFormatDectionProp = serializedObject.FindProperty("SourceFormatDection");
            SourceSampleRateProp = serializedObject.FindProperty("SourceSampleRate");
            SourceChannelsProp = serializedObject.FindProperty("SourceChannels");

            OnPCMFloatReadyEventProp = serializedObject.FindProperty("OnPCMFloatReadyEvent");
        }

        private bool drawSuccess = false;
        public override void OnInspectorGUI()
        {
            if (ADecoder == null) ADecoder = (AudioDecoder)target;

            serializedObject.Update();

            GUILayout.Space(2);
            GUILayout.BeginVertical("box");
            {
                drawSuccess = FMCoreEditor.DrawHeader("\nFMETP STREAM 4.0\n");
                if (!ADecoder.EditorShowSettings || !drawSuccess)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Settings")) ADecoder.EditorShowSettings = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Settings")) ADecoder.EditorShowSettings = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(GZipModeProp, new GUIContent("GZip Mode", "Decode Gzip when needed\n -LowLatency: Apply GZip in current Thread(Sync)\n -Balance: Apply GZip in current Thread(Async)\n -HighPerformance: Apply GZip in other Thread(Async)"));
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }

                if (!ADecoder.EditorShowPlayback || !drawSuccess)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Playback")) ADecoder.EditorShowPlayback = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Playback")) ADecoder.EditorShowPlayback = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(volumeProp, new GUIContent("Volume"));
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }

                if (!ADecoder.EditorShowAudioInfo || !drawSuccess)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Audio Info")) ADecoder.EditorShowAudioInfo = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Audio Info")) ADecoder.EditorShowAudioInfo = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Device Sample Rate: " + ADecoder.DeviceSampleRate);
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(SourceFormatDectionProp, new GUIContent(" Source Format Dection"));
                        GUILayout.EndHorizontal();

                        if (ADecoder.SourceFormatDection == AudioDecoderSourceFormat.Auto)
                        {
                            {
                                GUIStyle style = new GUIStyle();
                                style.normal.textColor = Color.yellow;

                                GUILayout.BeginHorizontal();
                                GUILayout.Label(" * Requires FMPCM16 Output Format from Audio/Mic Encoder", style);
                                GUILayout.EndHorizontal();
                            }

                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Source Sample Rate: " + ADecoder.SourceSampleRate);
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Source Channels: " + ADecoder.SourceChannels);
                            GUILayout.EndHorizontal();
                        }
                        else
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(SourceSampleRateProp, new GUIContent(" Source Sample Rate"));
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(SourceChannelsProp, new GUIContent(" Source Channels"));
                            GUILayout.EndHorizontal();
                        }
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(2);
            GUILayout.BeginVertical("box");
            {
                if (!ADecoder.EditorShowDecoded || !drawSuccess)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Decoded")) ADecoder.EditorShowDecoded = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Decoded")) ADecoder.EditorShowDecoded = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(OnPCMFloatReadyEventProp, new GUIContent("OnPCMFloatReadyEvent"));
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(2);
            GUILayout.BeginVertical("box");
            {
                if (!ADecoder.EditorShowPairing || !drawSuccess)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Pair Encoder & Decoder")) ADecoder.EditorShowPairing = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Pair Encoder & Decoder ")) ADecoder.EditorShowPairing = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(labelProp, new GUIContent("label"));
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}