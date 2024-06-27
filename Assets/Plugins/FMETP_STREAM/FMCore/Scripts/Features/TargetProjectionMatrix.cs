using UnityEngine;
using System.Collections;
using System.Threading.Tasks;
using System.Threading;

namespace FMSolution.FMETP
{
    public class TargetProjectionMatrix : MonoBehaviour
    {
        public Camera referenceCam;
        public Camera targetCam;

        public bool useCustomFOV = false;
        public float fov = 60f;
        [Header("limit the maximum fov")]
        public bool maxFovAsReference = false;
        public void Action_UpdateFOV(float _fov)
        {
            fov = _fov;
        }

        public bool allowUpdate = true;
        public void Action_SetAllowUpdate(bool _value)
        {
            allowUpdate = _value;
        }

        public bool ForceDisableUpdate = false;
        public void Action_SetForceDisableUpdate(bool _value)
        {
            ForceDisableUpdate = _value;
        }

        private async void UpdateProjectionMatrixLoopAsync()
        {
            while (!stoppedOrCancelled())
            {
                while (!allowUpdate || ForceDisableUpdate)
                {
#if UNITY_2023_1_OR_NEWER
                    await Awaitable.EndOfFrameAsync();
#else
                    await WaitForEndOfFrameAsync();
#endif
                }
                Matrix4x4 rm = referenceCam.projectionMatrix;
                if (!useCustomFOV)
                {
                    fov = referenceCam.fieldOfView;
                }

                if (maxFovAsReference)
                {
                    if (fov > referenceCam.fieldOfView)
                    {
                        fov = referenceCam.fieldOfView;
                    }
                }

                float aspect = referenceCam.aspect;

                float matrixY = 1f / Mathf.Tan(fov / (2f * Mathf.Rad2Deg));
                float matrixX = matrixY / aspect; // as matrixY IS the calculated fov ratio

                rm[0, 0] = matrixX;
                rm[1, 1] = matrixY;

                targetCam.fieldOfView = fov;
                targetCam.projectionMatrix = rm;

                await FMCoreTools.AsyncTask.Delay(5);
            }
        }

        private void Start()
        {
            cancellationTokenSource_global = new CancellationTokenSource();
            UpdateProjectionMatrixLoopAsync();
        }

        private void OnDisable() { StopAll(); }
        private void OnApplicationQuit() { StopAll(); }
        private void OnDestroy() { StopAll(); }

        private CancellationTokenSource cancellationTokenSource_global;
        private bool stoppedOrCancelled() { return cancellationTokenSource_global.IsCancellationRequested; }
        private void StopAllAsync()
        {
            if (cancellationTokenSource_global != null)
            {
                if (!cancellationTokenSource_global.IsCancellationRequested) cancellationTokenSource_global.Cancel();
            }
        }

        private void StopAll() { StopAllAsync(); }
        public Task<bool> WaitForEndOfFrameAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            StartCoroutine(FMCoreTools.AsyncTask.WaitForEndOfFrameCOR(tcs));
            return tcs.Task;
        }
    }
}