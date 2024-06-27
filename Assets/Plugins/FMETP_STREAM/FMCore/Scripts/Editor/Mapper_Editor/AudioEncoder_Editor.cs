using System;
using UnityEngine;
using UnityEditor;
using FMSolution.Editor;

namespace FMSolution.FMETP
{
    [CustomEditor(typeof(AudioEncoder))]
    [CanEditMultipleObjects]
    public class AudioEncoder_Editor : UnityEditor.Editor
    {
        private AudioEncoder AEncoder;

        SerializedProperty StreamGameSoundProp;
        SerializedProperty MuteLocalAudioPlaybackProp;

        SerializedProperty ForceMonoProp;
        SerializedProperty MatchSystemSampleRateProp;
        SerializedProperty TargetSampleRateProp;
        SerializedProperty AudioReadModeProp;

        SerializedProperty GZipModeProp;
        SerializedProperty StreamFPSProp;

        SerializedProperty OutputFormatProp;
        SerializedProperty OutputAsChunksProp;
        SerializedProperty OutputChunkSizeProp;
        SerializedProperty OnDataByteReadyEventProp;
        SerializedProperty OnRawPCM16ReadyEventProp;


        SerializedProperty labelProp;
        SerializedProperty dataLengthProp;

        private bool drawSuccess = false;
        private void OnEnable()
        {
            StreamGameSoundProp = serializedObject.FindProperty("StreamGameSound");
            MuteLocalAudioPlaybackProp = serializedObject.FindProperty("MuteLocalAudioPlayback");

            ForceMonoProp = serializedObject.FindProperty("ForceMono");
            MatchSystemSampleRateProp = serializedObject.FindProperty("MatchSystemSampleRate");
            TargetSampleRateProp = serializedObject.FindProperty("TargetSampleRate");
            AudioReadModeProp = serializedObject.FindProperty("AudioReadMode");

            StreamFPSProp = serializedObject.FindProperty("StreamFPS");
            GZipModeProp = serializedObject.FindProperty("GZipMode");

            OutputFormatProp = serializedObject.FindProperty("OutputFormat");
            OutputAsChunksProp = serializedObject.FindProperty("OutputAsChunks");
            OutputChunkSizeProp = serializedObject.FindProperty("OutputChunkSize");
            OnDataByteReadyEventProp = serializedObject.FindProperty("OnDataByteReadyEvent");
            OnRawPCM16ReadyEventProp = serializedObject.FindProperty("OnRawPCM16ReadyEvent");

            labelProp = serializedObject.FindProperty("label");
            dataLengthProp = serializedObject.FindProperty("dataLength");
        }

        // Update is called once per frame
        public override void OnInspectorGUI()
        {
            if (AEncoder == null) AEncoder = (AudioEncoder)target;

            serializedObject.Update();

            GUILayout.BeginVertical("box");
            {
                drawSuccess = FMCoreEditor.DrawHeader("\nFMETP STREAM 4.0\n");
                if (!AEncoder.EditorShowCapture || !drawSuccess)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Capture")) AEncoder.EditorShowCapture = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Capture")) AEncoder.EditorShowCapture = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(StreamGameSoundProp, new GUIContent("Stream Game Sound"));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(MuteLocalAudioPlaybackProp, new GUIContent("Mute Local Audio Playback"));
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(ForceMonoProp, new GUIContent("Force Mono"));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(MatchSystemSampleRateProp, new GUIContent("Match System SampleRate"));
                        GUILayout.EndHorizontal();

                        if (!AEncoder.MatchSystemSampleRate)
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(TargetSampleRateProp, new GUIContent("Target Sample Rate"));
                            GUILayout.EndHorizontal();
                        }

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(AudioReadModeProp, new GUIContent("Audio Read Mode", "- OnAudioFilterRead: default latency \n (with minor impact on local audio playback)\n - AudioListenerGetOutputData: high latency \n (without impact on local audio playback)\n - DSPTimeGetOutputData: lowest latency supported \n (refer to \"Audio/DSP Buffer Size\")"));
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();


            GUILayout.Space(2);
            GUILayout.BeginVertical("box");
            {
                if (!AEncoder.EditorShowAudioInfo || !drawSuccess)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Audio Info ")) AEncoder.EditorShowAudioInfo = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Audio Info ")) AEncoder.EditorShowAudioInfo = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("System Sample Rate: " + AEncoder.SystemSampleRate);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("System Channels: " + AEncoder.SystemChannels);
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        //GUILayout.Label("Output Sample Rate: " + AEncoder.OutputSampleRate);
                        GUILayout.Label("Output Sample Rate: " + (AEncoder.MatchSystemSampleRate ? AEncoder.SystemSampleRate : AEncoder.TargetSampleRate));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        //GUILayout.Label("Output Channels: " + AEncoder.OutputChannels);
                        GUILayout.Label("Output Channels: " + (AEncoder.ForceMono ? 1 : AEncoder.SystemChannels));
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(2);
            GUILayout.BeginVertical("box");
            {
                if (!AEncoder.EditorShowEncoded || !drawSuccess)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Encoded ")) AEncoder.EditorShowEncoded = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Encoded ")) AEncoder.EditorShowEncoded = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(StreamFPSProp, new GUIContent("StreamFPS"));
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(GZipModeProp, new GUIContent("GZip Mode"));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUIStyle style = new GUIStyle();
                        style.normal.textColor = Color.yellow;
                        GUILayout.Label(" Experiment feature: Reduce network traffic", style);
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(OutputFormatProp, new GUIContent("Output Format"));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(OutputAsChunksProp, new GUIContent("Output As Chunks", "Split the data into small chunks, and recommended for UDP stream"));
                        GUILayout.EndHorizontal();
                        if (AEncoder.OutputAsChunks)
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(OutputChunkSizeProp, new GUIContent("Output Chunk Size"));
                            GUILayout.EndHorizontal();
                        }
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        if (AEncoder.OutputFormat == AudioOutputFormat.FMPCM16)
                        {
                            EditorGUILayout.PropertyField(OnDataByteReadyEventProp, new GUIContent("OnDataByteReadyEvent"));
                        }
                        else if (AEncoder.OutputFormat == AudioOutputFormat.PCM16)
                        {
                            EditorGUILayout.PropertyField(OnRawPCM16ReadyEventProp, new GUIContent("OnRawPCM16ReadyEvent"));
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(2);
            GUILayout.BeginVertical("box");
            {
                if (!AEncoder.EditorShowPairing || !drawSuccess)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Pair Encoder & Decoder ")) AEncoder.EditorShowPairing = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Pair Encoder & Decoder ")) AEncoder.EditorShowPairing = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(labelProp, new GUIContent("label"));
                        GUILayout.EndHorizontal();


                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(dataLengthProp, new GUIContent("Encoded Size(byte)"));
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