using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FMSolution.FMNetwork
{
    [DisallowMultipleComponent]
    public class FMNetworkTransformView : MonoBehaviour
    {
        public FMNetworkManager FMNetwork;
        private void Reset()
        {
            FMNetworkManager[] _networks = Resources.FindObjectsOfTypeAll<FMNetworkManager>();
            for(int i = 0; i<_networks.Length; i++)
            {
                if (i == 0 || _networks[i].NetworkType == FMNetworkType.Server || _networks[i].NetworkType == FMNetworkType.Client)
                {
                    FMNetwork = _networks[i];
                }
            }

            UpdateAllIDs();
        }

        public void UpdateAllIDs()
        {
            FMNetworkTransformView[] allViews = Resources.FindObjectsOfTypeAll<FMNetworkTransformView>();
            for (int i = 0; i < allViews.Length; i++)
            {
                allViews[i].viewID = i + 1;
            }
        }
        public bool CheckAllIDs()
        {
            bool _duplicated = false;
            FMNetworkTransformView[] allViews = Resources.FindObjectsOfTypeAll<FMNetworkTransformView>();
            for (int i = 0; i < allViews.Length; i++)
            {
                if (allViews[i].viewID == viewID) _duplicated = true;
            }
            if (_duplicated) UpdateAllIDs();

            return _duplicated;
        }


        [SerializeField] private int viewID = -1;
        public int GetViewID() { return viewID; }
        public void SetViewID(int inputViewID) { viewID = inputViewID; }

        [SerializeField] private bool isOwner = false;
        public bool IsOwner
        {
            get { return isOwner; }
            set
            {
                isOwner = value;
                try
                {
                    ResetNetworkObjectSyncTimestamp();

                    //force taking ownership when it's true...etc
                    if (isOwner)
                    {
                        updatingOwnership = true;
                        EnqueueTransformSyncData();
                    }
                }
                catch (Exception e) { Debug.LogError(e); }
            }
        }
        public bool TakeOwnership = false;
        private bool updatingOwnership = false;

        private float syncTimer = 0f;
        [Range(1f, 60f)] public float SyncFPS = 20f;
        private float SyncFPS_old = -1;
        public FMNetworkTransformSyncType SyncType = FMNetworkTransformSyncType.PositionAndRotation;

        private FMNetworkTransformSyncData syncData = new FMNetworkTransformSyncData();
        private FMNetworkTransformSyncData receivedSyncData = new FMNetworkTransformSyncData();

        private float updateOwnershipTimer = 0f;
        private float updateOwnershipThreshold = 1f;
        public void Action_UpdateSyncData(FMNetworkTransformSyncData inputSyncData, float inputTimestamp)
        {
            //ignore and skip it if updating ownership
            if (updatingOwnership) return;

            isOwner = false;
            receivedSyncData = inputSyncData;

            float Timestamp = inputTimestamp;
            if (Timestamp > LastReceivedTimestamp)
            {
                LastReceivedTimestamp = TargetTimestamp;
                TargetTimestamp = Timestamp;
                CurrentTimestamp = LastReceivedTimestamp;
            }
        }

        private float LastReceivedTimestamp = 0f;
        private float TargetTimestamp = 0f;
        private float CurrentTimestamp = 0f;
        private void ResetNetworkObjectSyncTimestamp()
        {
            //reset network sync timestamp
            CurrentTimestamp = 0f;
            LastReceivedTimestamp = 0f;
            TargetTimestamp = 0f;
        }

        private void EnqueueTransformSyncData()
        {
            if (SyncType == FMNetworkTransformSyncType.None) return;

            syncData.viewID = viewID;
            syncData.syncType = SyncType;

            syncData.position = transform.position;
            syncData.rotation = transform.rotation;
            syncData.localScale = transform.localScale;
            FMNetwork.Action_EnqueueTransformSyncData(syncData);
        }

        public void Action_TakeOwnership()
        {
            IsOwner = true;
            updatingOwnership = true;
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            if (updatingOwnership)
            {
                updateOwnershipTimer += Time.deltaTime;
                if (updateOwnershipTimer > updateOwnershipThreshold)
                {
                    updatingOwnership = false;
                    updateOwnershipTimer = 0f;
                }

                return;
            }

            if (isOwner)
            {
                //on sync fps changes, reset the timer..
                if (SyncFPS != SyncFPS_old)
                {
                    SyncFPS_old = SyncFPS;
                    syncTimer = 0f;
                }

                syncTimer += Time.deltaTime;
                if (syncTimer > (1f / SyncFPS))
                {
                    syncTimer %= (1f / SyncFPS);

                    EnqueueTransformSyncData();
                }
            }
            else
            {
                if (LastReceivedTimestamp <= 0)
                {
                    //force stay at the bottom
                    transform.position = new Vector3(0f, int.MinValue, 0f);
                    transform.rotation = Quaternion.identity;
                    transform.localScale = new Vector3(1f, 1f, 1f);
                }
            }
        }
        private void LateUpdate()
        {
            if (!Application.isPlaying) return;

            if (isOwner) return;
            if (LastReceivedTimestamp > 0)
            {
                //keep delta time, but update in late update, to make sure it override the transformation properly.
                CurrentTimestamp += Time.deltaTime;
                float step = (CurrentTimestamp - LastReceivedTimestamp) / (TargetTimestamp - LastReceivedTimestamp);
                step = Mathf.Clamp(step, 0f, 1f);

                switch (receivedSyncData.syncType)
                {
                    case FMNetworkTransformSyncType.All: OverrideTransformation(true, true, true, step); break;
                    case FMNetworkTransformSyncType.PositionOnly: OverrideTransformation(true, false, false, step); break;
                    case FMNetworkTransformSyncType.RotationOnly: OverrideTransformation(false, true, false, step); break;
                    case FMNetworkTransformSyncType.ScaleOnly: OverrideTransformation(false, false, true, step); break;
                    case FMNetworkTransformSyncType.PositionAndRotation: OverrideTransformation(true, true, false, step); break;
                    case FMNetworkTransformSyncType.PositionAndScale: OverrideTransformation(true, false, true, step); break;
                    case FMNetworkTransformSyncType.RotationAndScale: OverrideTransformation(false, true, true, step); break;
                    case FMNetworkTransformSyncType.None: break;
                }
            }
            else
            {
                //force stay at the bottom
                transform.position = new Vector3(0f, int.MinValue, 0f);
                transform.rotation = Quaternion.identity;
                transform.localScale = new Vector3(1f, 1f, 1f);
            }
        }

        private void OverrideTransformation(bool overridePosition, bool overrideRotation, bool overrideScale, float inputStep)
        {
            if (overridePosition) transform.position = Vector3.Slerp(transform.position, receivedSyncData.position, inputStep);
            if (overrideRotation) transform.rotation = Quaternion.Slerp(transform.rotation, receivedSyncData.rotation, inputStep);
            if (overrideScale) transform.localScale = Vector3.Slerp(transform.localScale, receivedSyncData.localScale, inputStep);
        }

        private bool IsFMNetworkExisted()
        {
            if (FMNetwork == null)
            {
                UnityEngine.Object[] managerObjects = Resources.FindObjectsOfTypeAll(typeof(FMNetworkManager));
                for (int i = 0; i < managerObjects.Length; i++)
                {
                    if (FMNetwork == null)
                    {
                        FMNetwork = managerObjects[i] as FMNetworkManager;
                    }
                    else
                    {
                        if (FMNetwork.NetworkType != FMNetworkType.Server && FMNetwork.NetworkType != FMNetworkType.Client)
                        {
                            FMNetwork = managerObjects[i] as FMNetworkManager;
                        }
                    }
                }
            }

            return FMNetwork != null;
        }
        private void OnFoundServerAction(string inputIP) { ResetNetworkObjectSyncTimestamp(); }
        private void RegisterFMNetworkID()
        {
            //register id...
            if (IsFMNetworkExisted())
            {
                if (!FMNetwork.GetTransformViewDictionary().ContainsKey(viewID))
                {
                    FMNetwork.RegisterFMNetworkID(this);
                    FMNetwork.OnFoundServerEvent.AddListener(OnFoundServerAction);
                }
                else
                {
                    Debug.LogError("view id existed: " + viewID + ", " + this.gameObject.name + " will be ignored.");
                }

                ResetNetworkObjectSyncTimestamp();
            }
        }
        private void UnregisterFMNetworkID()
        {
            //remove id...
            if (IsFMNetworkExisted())
            {
                FMNetwork.UnregisterFMNetworkID(this);
                FMNetwork.OnFoundServerEvent.RemoveListener(OnFoundServerAction);
            }
        }

        private void OnEnable() { RegisterFMNetworkID(); }
        //private void OnDisable()
        //{
        //    //no need to unregister it immediately in runtime.. otherwise it may casue bug
        //    if (Application.isPlaying) return;
        //    UnregisterFMNetworkID();
        //}
        private void OnDestroy() { UnregisterFMNetworkID(); }
    }
}
