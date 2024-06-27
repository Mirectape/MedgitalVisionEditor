using System;
using UnityEngine;
using UnityEditor;

using System.Collections.Generic;
using System.Linq;
using FMSolution.Editor;

namespace FMSolution.FMETP
{
    [CustomEditor(typeof(FMPCStreamEncoder))]
    [CanEditMultipleObjects]
    public class FMPCStreamEncoder_Editor : UnityEditor.Editor
    {
        private FMPCStreamEncoder FMPCEncoder;

        SerializedProperty TargetCameraProp;
        SerializedProperty TargetWidthProp;
        SerializedProperty TargetHeightProp;

        SerializedProperty FastModeProp;
        SerializedProperty AsyncModeProp;
        SerializedProperty GZipModeProp;
        SerializedProperty EnableAsyncGPUReadbackProp;

        SerializedProperty QualityProp;
        SerializedProperty ChromaSubsamplingProp;

        SerializedProperty StreamFPSProp;

        SerializedProperty ignoreSimilarTextureProp;
        SerializedProperty similarByteSizeThresholdProp;

        SerializedProperty OutputAsChunksProp;
        SerializedProperty OutputChunkSizeProp;
        SerializedProperty OnDataByteReadyEventProp;

        SerializedProperty labelProp;
        SerializedProperty dataLengthProp;

        void OnEnable()
        {
            TargetCameraProp = serializedObject.FindProperty("TargetCamera");
            TargetWidthProp = serializedObject.FindProperty("TargetWidth");
            TargetHeightProp = serializedObject.FindProperty("TargetHeight");

            FastModeProp = serializedObject.FindProperty("FastMode");
            AsyncModeProp = serializedObject.FindProperty("AsyncMode");
            GZipModeProp = serializedObject.FindProperty("GZipMode");
            EnableAsyncGPUReadbackProp = serializedObject.FindProperty("EnableAsyncGPUReadback");

            QualityProp = serializedObject.FindProperty("Quality");
            ChromaSubsamplingProp = serializedObject.FindProperty("ChromaSubsampling");

            StreamFPSProp = serializedObject.FindProperty("StreamFPS");

            ignoreSimilarTextureProp = serializedObject.FindProperty("ignoreSimilarTexture");
            similarByteSizeThresholdProp = serializedObject.FindProperty("similarByteSizeThreshold");

            OutputAsChunksProp = serializedObject.FindProperty("OutputAsChunks");
            OutputChunkSizeProp = serializedObject.FindProperty("OutputChunkSize");
            OnDataByteReadyEventProp = serializedObject.FindProperty("OnDataByteReadyEvent");

            labelProp = serializedObject.FindProperty("label");
            dataLengthProp = serializedObject.FindProperty("dataLength");
        }

        private bool drawSuccess = false;
        public override void OnInspectorGUI()
        {
            if (FMPCEncoder == null) FMPCEncoder = (FMPCStreamEncoder)target;

            serializedObject.Update();

            GUILayout.Space(10);
            GUILayout.BeginVertical("box");
            {
                drawSuccess = FMCoreEditor.DrawHeader("\nFMETP STREAM 4.0\n");
                if (!FMPCEncoder.EditorShowSettings || !drawSuccess)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Settings")) FMPCEncoder.EditorShowSettings = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Settings")) FMPCEncoder.EditorShowSettings = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginVertical("box");
                    {
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(TargetCameraProp, new GUIContent("TargetCamera"));
                            GUILayout.EndHorizontal();

                            if (FMPCEncoder.TargetCamera == null)
                            {
                                //GUILayout.BeginVertical("box");
                                {
                                    GUIStyle style = new GUIStyle();
                                    style.normal.textColor = Color.red;

                                    GUILayout.BeginHorizontal();
                                    GUILayout.Label(" Target Camera cannot be null", style);
                                    GUILayout.EndHorizontal();

                                }
                                //GUILayout.EndVertical();
                            }

                            GUILayout.BeginVertical("box");
                            {
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.PropertyField(TargetWidthProp, new GUIContent("Sample Width"));
                                GUILayout.EndHorizontal();

                                GUILayout.BeginHorizontal();
                                EditorGUILayout.PropertyField(TargetHeightProp, new GUIContent("Sample Height"));
                                GUILayout.EndHorizontal();

                                GUILayout.BeginHorizontal();
                                GUILayout.Label("Sample Count: " + (FMPCEncoder.TargetWidth * FMPCEncoder.TargetHeight / 2).ToString());
                                GUILayout.EndHorizontal();
                            }
                            GUILayout.EndVertical();
                        }
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(QualityProp, new GUIContent("Quality"));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(StreamFPSProp, new GUIContent("StreamFPS"));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginVertical("box");
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(FastModeProp, new GUIContent("Fast Encode Mode"));
                            GUILayout.EndHorizontal();

                            if (FMPCEncoder.FastMode)
                            {
                                GUILayout.BeginVertical("box");
                                {
                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(AsyncModeProp, new GUIContent("Async Encode (multi-threading)"));
                                    GUILayout.EndHorizontal();
                                }
                                GUILayout.EndVertical();

                                GUILayout.BeginVertical("box");
                                {
                                    GUILayout.BeginHorizontal();

                                    GUIStyle style = new GUIStyle();
                                    style.normal.textColor = FMPCEncoder.SupportsAsyncGPUReadback ? Color.green : Color.gray;
                                    GUILayout.Label("* Async GPU Readback (" + (FMPCEncoder.SupportsAsyncGPUReadback ? "Supported" : "Unknown or Not Supported") + ")", style);
                                    GUILayout.EndHorizontal();

                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(EnableAsyncGPUReadbackProp, new GUIContent("Enabled When Supported"));
                                    GUILayout.EndHorizontal();

                                }
                                GUILayout.EndVertical();

                                GUILayout.BeginVertical("box");
                                {
                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.PropertyField(ChromaSubsamplingProp, new GUIContent("Chroma Subsampling"));
                                    GUILayout.EndHorizontal();
                                }
                                GUILayout.EndVertical();
                            }

                            {
                                GUILayout.BeginHorizontal();
                                GUIStyle style = new GUIStyle();
                                style.normal.textColor = Color.yellow;
                                GUILayout.Label("* Experiment for Mac, Windows, Android (Forced Enabled on iOS)", style);
                                GUILayout.EndHorizontal();
                            }
                        }
                        GUILayout.EndVertical();

                        GUILayout.BeginVertical("box");
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(GZipModeProp, new GUIContent("GZip Mode"));
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);
            GUILayout.BeginVertical("box");
            {
                if (!FMPCEncoder.EditorShowNetworking || !drawSuccess)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Networking")) FMPCEncoder.EditorShowNetworking = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Networking")) FMPCEncoder.EditorShowNetworking = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(ignoreSimilarTextureProp, new GUIContent("Ignore Similar Texture"));
                        GUILayout.EndHorizontal();

                        if (FMPCEncoder.ignoreSimilarTexture)
                        { 
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(similarByteSizeThresholdProp, new GUIContent("Similar Byte Size Threshold"));
                            GUILayout.EndHorizontal();
                        }
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);
            GUILayout.BeginVertical("box");
            {
                if (!FMPCEncoder.EditorShowEncoded || !drawSuccess)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Encoded")) FMPCEncoder.EditorShowEncoded = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Encoded")) FMPCEncoder.EditorShowEncoded = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(OutputAsChunksProp, new GUIContent("Output As Chunks", "Split the data into small chunks, and recommended for UDP stream"));
                        GUILayout.EndHorizontal();
                        if (FMPCEncoder.OutputAsChunks)
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
                        EditorGUILayout.PropertyField(OnDataByteReadyEventProp, new GUIContent("OnDataByteReadyEvent"));
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();


            GUILayout.Space(10);
            GUILayout.BeginVertical("box");
            {
                if (!FMPCEncoder.EditorShowPairing || !drawSuccess)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Pair Encoder & Decoder ")) FMPCEncoder.EditorShowPairing = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Pair Encoder & Decoder ")) FMPCEncoder.EditorShowPairing = false;
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