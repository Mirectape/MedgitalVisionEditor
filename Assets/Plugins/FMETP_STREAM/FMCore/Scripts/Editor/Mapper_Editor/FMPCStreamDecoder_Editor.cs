using System;
using UnityEngine;
using UnityEditor;
using FMSolution.Editor;

namespace FMSolution.FMETP
{
    [CustomEditor(typeof(FMPCStreamDecoder))]
    [CanEditMultipleObjects]
    public class FMPCStreamDecoder_Editor : UnityEditor.Editor
    {
        private FMPCStreamDecoder FMPCDecoder;

        SerializedProperty FastModeProp;
        SerializedProperty AsyncModeProp;
        SerializedProperty GZipModeProp;

        SerializedProperty MainColorProp;
        SerializedProperty PointSizeProp;
        SerializedProperty ApplyDistanceProp;

        SerializedProperty labelProp;

        void OnEnable()
        {
            FastModeProp = serializedObject.FindProperty("FastMode");
            AsyncModeProp = serializedObject.FindProperty("AsyncMode");
            GZipModeProp = serializedObject.FindProperty("GZipMode");

            MainColorProp = serializedObject.FindProperty("MainColor");
            PointSizeProp = serializedObject.FindProperty("PointSize");
            ApplyDistanceProp = serializedObject.FindProperty("ApplyDistance");

            labelProp = serializedObject.FindProperty("label");
        }

        private bool drawSuccess = false;
        public override void OnInspectorGUI()
        {
            if (FMPCDecoder == null) FMPCDecoder = (FMPCStreamDecoder)target;

            serializedObject.Update();
            GUILayout.Space(10);
            GUILayout.BeginVertical("box");
            {
                drawSuccess = FMCoreEditor.DrawHeader("\nFMETP STREAM 4.0\n");
                if (!FMPCDecoder.EditorShowSettings || !drawSuccess)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Settings")) FMPCDecoder.EditorShowSettings = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Settings")) FMPCDecoder.EditorShowSettings = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(FastModeProp, new GUIContent("Fast Decode Mode"));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUIStyle style = new GUIStyle();
                        style.normal.textColor = Color.yellow;
                        GUILayout.Label("* Experiment for Mac, Windows, Android (Forced Enabled on iOS)", style);
                        GUILayout.EndHorizontal();

                        if (FMPCDecoder.FastMode)
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(AsyncModeProp, new GUIContent("Async Decode (multi-threading)"));
                            GUILayout.EndHorizontal();
                        }

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(GZipModeProp, new GUIContent("GZip Mode", "Decode Gzip when needed\n -LowLatency: Apply GZip in current Thread(Sync)\n -Balance: Apply GZip in current Thread(Async)\n -HighPerformance: Apply GZip in other Thread(Async)"));
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            {
                if (!FMPCDecoder.EditorShowDecoded || !drawSuccess)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Decoded")) FMPCDecoder.EditorShowDecoded = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Decoded")) FMPCDecoder.EditorShowDecoded = false;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                    GUILayout.Label("Total Point Count: " + (Application.isPlaying ? FMPCDecoder.PCCount.ToString() : "null"));

                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(MainColorProp, new GUIContent("Main Color"));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(PointSizeProp, new GUIContent("Point Size"));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(ApplyDistanceProp, new GUIContent("Apply Distance"));
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);
            GUILayout.BeginVertical("box");
            {
                if (!FMPCDecoder.EditorShowPairing || !drawSuccess)
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("+ Pair Encoder & Decoder ")) FMPCDecoder.EditorShowPairing = true;
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("- Pair Encoder & Decoder ")) FMPCDecoder.EditorShowPairing = false;
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