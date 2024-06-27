using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace FMSolution.FMETP
{
    public static class FMMicrophone
    {
		//public int GetPosition(string deviceName);
		private static string[] detectedDevices = null;
		public static string[] devices
		{
			get
			{
#if !UNITY_WEBGL || UNITY_EDITOR //FM Notes: stripped in build bug, with #if UNITY_WEBGL || !UNITY_EDITOR
				if (detectedDevices == null) detectedDevices = Microphone.devices;
#endif
				return detectedDevices;
			}
		}

		//internal static bool isAnyDeviceRecording
		//{
		//	[MethodImpl(MethodImplOptions.InternalCall)]
		//	[NativeName("IsAnyRecordDeviceActive")]
		//	get;
		//}

		//[MethodImpl(MethodImplOptions.InternalCall)]
		//[NativeMethod(IsThreadSafe = true)]
		//private static extern int GetMicrophoneDeviceIDFromName(string name);

		//[MethodImpl(MethodImplOptions.InternalCall)]
		//private static extern AudioClip StartRecord(int deviceID, bool loop, float lengthSec, int frequency);

		//[MethodImpl(MethodImplOptions.InternalCall)]
		//private static extern void EndRecord(int deviceID);

		//[MethodImpl(MethodImplOptions.InternalCall)]
		//private static extern bool IsRecording(int deviceID);

		//[MethodImpl(MethodImplOptions.InternalCall)]
		//[NativeMethod(IsThreadSafe = true)]
		//private static extern int GetRecordPosition(int deviceID);

		//[MethodImpl(MethodImplOptions.InternalCall)]
		//private static extern void GetDeviceCaps(int deviceID, out int minFreq, out int maxFreq);

		public static AudioClip Start(string deviceName, bool loop, int lengthSec, int frequency)
		{
#if !UNITY_WEBGL || UNITY_EDITOR //FM Notes: stripped in build bug, with #if UNITY_WEBGL || !UNITY_EDITOR
			return Microphone.Start(deviceName, loop, lengthSec, frequency);
#endif
			return null;
        }

		public static void End(string deviceName)
		{
#if !UNITY_WEBGL || UNITY_EDITOR //FM Notes: stripped in build bug, with #if UNITY_WEBGL || !UNITY_EDITOR
			Microphone.End(deviceName);
#endif
		}

		public static bool IsRecording(string deviceName)
        {
#if !UNITY_WEBGL || UNITY_EDITOR //FM Notes: stripped in build bug, with #if UNITY_WEBGL || !UNITY_EDITOR
			return Microphone.IsRecording(deviceName);
#endif
			return false;
		}

		public static int GetPosition(string deviceName)
        {
#if !UNITY_WEBGL || UNITY_EDITOR //FM Notes: stripped in build bug, with #if UNITY_WEBGL || !UNITY_EDITOR
			return Microphone.GetPosition(deviceName);
#endif
			return -1;
		}

		public static void GetDeviceCaps(string deviceName, out int minFreq, out int maxFreq)
        {
#if !UNITY_WEBGL || UNITY_EDITOR //FM Notes: stripped in build bug, with #if UNITY_WEBGL || !UNITY_EDITOR
			Microphone.GetDeviceCaps(deviceName, out minFreq, out maxFreq);
#endif
			minFreq = -1;
			maxFreq = -1;
		}

#if !UNITY_WEBGL || UNITY_EDITOR //FM Notes: stripped in build bug, with #if UNITY_WEBGL || !UNITY_EDITOR
#else
		[DllImport("__Internal")] private static extern void FMStopMicStream();
		[DllImport("__Internal")] private static extern void FMCaptureMicStart_2021_2(int callbackID, Action<int, int, IntPtr> callback);
		[DllImport("__Internal")] private static extern void FMCoreTools_WebGLAddScript_2021_2(string _innerHTML, string _src);

		private static string initialisedJavascript = null;
		private static void initialiseMicrophoneJavascript(AudioOutputFormat outputFormat, int outputSampleRate, int outputChannels)
		{
			if (string.IsNullOrEmpty(initialisedJavascript))
			{
				string javascript = ""
					+ $@"var OutputFormat = ""{outputFormat.ToString()}"";" + "\n"
					+ $@"var OutputSampleRate = {outputSampleRate};" + "\n"
					+ $@"var OutputChannels = {outputChannels};" + "\n"
					//+ $@"var maxID = {1024};" + "\n"
					//+ $@"var chunkSize = {1400};" + "\n"
					//+ $@"var dataID = {0};" + "\n"
					//+ $@"var label_mic = {2001};" + "\n"
					+ $@"var MicRecorder;" + "\n"
					+ $@"var MicRecording = {"false"};" + "\n"
					;

				initialisedJavascript = javascript;
				FMCoreTools_WebGLAddScript_2021_2(javascript, "");
				Debug.Log(javascript);
			}
		}
		public static int StartFMMicrophoneWebGL(AudioOutputFormat outputFormat, int outputSampleRate, int outputChannels, Action<byte[]> callback)
		{
			initialiseMicrophoneJavascript(outputFormat, outputSampleRate, outputChannels);

			int _callbackID = getDictionaryID;
			dictionary_callbacks.Add(_callbackID, callback);

			FMCaptureMicStart_2021_2(_callbackID, callback_OnReceivedRawPCM16IntPtr);

			return _callbackID;
		}

		[AOT.MonoPInvokeCallback(typeof(Action<int, int, IntPtr>))]
        public static void callback_OnReceivedRawPCM16IntPtr(int dictionaryID, int length, IntPtr ptr)
        {
            byte[] byteData = FMCoreTools.IntPtrToByteArray(ptr, length);
			if (dictionary_callbacks.TryGetValue(dictionaryID, out Action<byte[]> _targetCallback)) _targetCallback.Invoke(byteData);
		}

		private static int dictionaryID = 0;
		private static int getDictionaryID { get { dictionaryID++; dictionaryID %= int.MaxValue - 1; return dictionaryID; } }
		private static Dictionary<int, Action<byte[]>> dictionary_callbacks = new Dictionary<int, Action<byte[]>>();

        public static void StopWebGL(int callbackID)
		{
			FMStopMicStream();
			try { dictionary_callbacks.Remove(callbackID); } catch { }
		}
#endif
	}
}
