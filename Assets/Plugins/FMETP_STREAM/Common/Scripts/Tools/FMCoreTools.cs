using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace FMSolution
{
    public static class FMCoreTools
    {
        public static void NativeArrayCopyTo(NativeArray<byte> nativeArray, ref byte[] referenceByteArray)
        {
            if (referenceByteArray.Length != nativeArray.Length) referenceByteArray = new byte[nativeArray.Length];
            nativeArray.CopyTo(referenceByteArray);
        }

        public static byte[] IntPtrToByteArray(IntPtr ptr, int length)
        {
            byte[] byteData = new byte[length];
            Marshal.Copy(ptr, byteData, 0, length);
            return byteData;
        }
        /// <summary>
        /// convert byte[] to float[]
        /// </summary>
        public static float[] ToFloatArray(byte[] byteArray)
        {
            int len = byteArray.Length / 2;
            float[] floatArray = new float[len];
            for (int i = 0; i < byteArray.Length; i += 2)
            {
                floatArray[i / 2] = ((float)BitConverter.ToInt16(byteArray, i)) / 32767f;
            }
            return floatArray;
        }

        /// <summary>
        /// convert float to Int16 space
        /// </summary>
        public static Int16 FloatToInt16(float inputFloat)
        {
            return Convert.ToInt16(inputFloat * Int16.MaxValue);
            //inputFloat *= 32767;
            //if (inputFloat < -32768) inputFloat = -32768;
            //if (inputFloat > 32767) inputFloat = 32767;
            //return Convert.ToInt16(inputFloat);
        }

        /// <summary>
        /// compare two int values, return true if they are similar referring to the sizeThreshold
        /// </summary>
        public static bool CheckSimilarSize(int inputByteLength1, int inputByteLength2, int sizeThreshold)
        {
            float diff = Mathf.Abs(inputByteLength1 - inputByteLength2);
            return diff < sizeThreshold;
        }

        public static class AsyncTask
        {
            /// <summary>
            /// Async Task delay
            /// </summary>
            public static async Task Yield()
            {
#if UNITY_WSA && !UNITY_EDITOR
                //found overhead issue on HoloLens 2, adding extra 1ms delay should help
                await Task.Delay(1);
#endif
                await Task.Yield();
            }

            private static TimeSpan timeSpanOneMS = TimeSpan.FromMilliseconds(1f);
            /// <summary>
            /// Async Task delay
            /// </summary>
            public static async Task Delay(TimeSpan inputTimeSpan)
            {
                try
                {
#if UNITY_WSA && !UNITY_EDITOR
                    //found overhead issue on HoloLens 2, force minimal 1ms delay
                    inputTimeSpan = inputTimeSpan < timeSpanOneMS ? timeSpanOneMS : inputTimeSpan;
#endif

#if !UNITY_WEBGL || UNITY_EDITOR //FM Notes: stripped in build bug, with #if UNITY_WEBGL || !UNITY_EDITOR
                    // For NON-WebGL platform
                    await Task.Delay(inputTimeSpan);
#else
                    // Unity 2021 do NOT support Task.Delay() in WebGL
                    float startTime = Time.realtimeSinceStartup;
                    float delay = inputTimeSpan.Seconds;

                    while (Time.realtimeSinceStartup - startTime < delay)
                    {
                        // Wait for the delay time to pass
                        await Task.Yield();
                    }
#endif
                }
                catch { }
            }

            /// <summary>
            /// Async Task delay
            /// </summary>
            public static async Task Delay(TimeSpan inputTimeSpan, CancellationToken ct)
            {
                try
                {
#if UNITY_WSA && !UNITY_EDITOR
                    //found overhead issue on HoloLens 2, force minimal 1ms delay
                    inputTimeSpan = inputTimeSpan < timeSpanOneMS ? timeSpanOneMS : inputTimeSpan;
#endif

#if !UNITY_WEBGL || UNITY_EDITOR //FM Notes: stripped in build bug, with #if UNITY_WEBGL || !UNITY_EDITOR
                    // For NON-WebGL platform
                    await Task.Delay(inputTimeSpan, ct);
#else
                    // Unity 2021 do NOT support Task.Delay() in WebGL
                    float startTime = Time.realtimeSinceStartup;
                    float delay = inputTimeSpan.Seconds;

                    while (Time.realtimeSinceStartup - startTime < delay && !ct.IsCancellationRequested)
                    {
                        // Wait for the delay time to pass
                        await Task.Yield();
                    }
#endif
                }
                catch { }
            }

            /// <summary>
            /// Async Task delay
            /// </summary>
            public static async Task Delay(int inputMS)
            {
                try
                {
#if UNITY_WSA && !UNITY_EDITOR
                    //found overhead issue on HoloLens 2, force minimal 1ms delay
                    inputMS = Mathf.Clamp(inputMS, 1, int.MaxValue);
#endif

#if !UNITY_WEBGL || UNITY_EDITOR //FM Notes: stripped in build bug, with #if UNITY_WEBGL || !UNITY_EDITOR
                    // For NON-WebGL platform
                    await Task.Delay(inputMS);
#else
                    // Unity 2021 do NOT support Task.Delay() in WebGL
                    float startTime = Time.realtimeSinceStartup;
                    float delay = (float)inputMS / 1000f;

                    while (Time.realtimeSinceStartup - startTime < delay)
                    {
                        // Wait for the delay time to pass
                        await Task.Yield();
                    }
#endif
                }
                catch { }
            }
            /// <summary>
            /// Async Task delay
            /// </summary>
            public static async Task Delay(int inputMS, CancellationToken ct)
            {
                try
                {
#if UNITY_WSA && !UNITY_EDITOR
                    //found overhead issue on HoloLens 2, force minimal 1ms delay
                    inputMS = Mathf.Clamp(inputMS, 1, int.MaxValue);
#endif

#if !UNITY_WEBGL || UNITY_EDITOR //FM Notes: stripped in build bug, with #if UNITY_WEBGL || !UNITY_EDITOR
                    // For NON-WebGL platform
                    await Task.Delay(inputMS, ct);
#else
                    // Unity 2021 do NOT support Task.Delay() in WebGL
                    float startTime = Time.realtimeSinceStartup;
                    float delay = (float)inputMS / 1000f;

                    while (Time.realtimeSinceStartup - startTime < delay && !ct.IsCancellationRequested)
                    {
                        // Wait for the delay time to pass
                        await Task.Yield();
                    }
#endif
                }
                catch { }
            }

            public static IEnumerator WaitForEndOfFrameCOR(TaskCompletionSource<bool> tcs)
            {
                yield return new WaitForEndOfFrame();
                tcs.TrySetResult(true);
            }
        }
    }
}