using FMSolution.FMNetwork;
using System;
using UnityEngine;

public class FMNetworkHandler : MonoBehaviour
{
    private Transform _cameraTransform;

    public GameObject[] Objects;

    [SerializeField] private FMNetworkManager _fmManager;

    private void Update()
    {
        //try to send/sync in each update 
        ActionEncoderTransformation();
    }

    public void ActionEncoderTransformation()
    {
        if(Objects == null || _fmManager.NetworkType == FMNetworkType.Client)
        {
            return;
        }

        //convert posX, posY, posZ, rotX. rotY, rotZ, rotW into byte[]
        byte[] sendBytes = new byte[Objects.Length * 7 * 4];

        int offset = 0;

        for (int i = 0; i < Objects.Length; i++)
        {
            //get byte from transform
            byte[] byte_px = BitConverter.GetBytes(Objects[i].transform.position.x);
            byte[] byte_py = BitConverter.GetBytes(Objects[i].transform.position.y);
            byte[] byte_pz = BitConverter.GetBytes(Objects[i].transform.position.z);

            byte[] byte_rx = BitConverter.GetBytes(Objects[i].transform.rotation.x);
            byte[] byte_ry = BitConverter.GetBytes(Objects[i].transform.rotation.y);
            byte[] byte_rz = BitConverter.GetBytes(Objects[i].transform.rotation.z);
            byte[] byte_rw = BitConverter.GetBytes(Objects[i].transform.rotation.w);

            //copy each byte[] to SendBytes
            Buffer.BlockCopy(byte_px, 0, sendBytes, offset, 4); offset += 4;
            Buffer.BlockCopy(byte_py, 0, sendBytes, offset, 4); offset += 4;
            Buffer.BlockCopy(byte_pz, 0, sendBytes, offset, 4); offset += 4;

            Buffer.BlockCopy(byte_rx, 0, sendBytes, offset, 4); offset += 4;
            Buffer.BlockCopy(byte_ry, 0, sendBytes, offset, 4); offset += 4;
            Buffer.BlockCopy(byte_rz, 0, sendBytes, offset, 4); offset += 4;
            Buffer.BlockCopy(byte_rw, 0, sendBytes, offset, 4); offset += 4;

            //send the bytes[]
            if (_fmManager.NetworkType == FMNetworkType.Server)
            {
                _fmManager.SendToOthers(sendBytes);
            }
        }
    }

    public void ActionDecodeTransformation(byte[] receivedBytes)
    {
        // make sure id doesn't override server pos
        if(_fmManager.NetworkType == FMNetworkType.Server)
        {
            return;
        }

        //decode received data for each object
        int offset = 0;
        for (int i = 0; i < Objects.Length; i++)
        {
            float px = BitConverter.ToSingle(receivedBytes, offset); offset += 4;
            float py = BitConverter.ToSingle(receivedBytes, offset); offset += 4;
            float pz = BitConverter.ToSingle(receivedBytes, offset); offset += 4;

            float rx = BitConverter.ToSingle(receivedBytes, offset); offset += 4;
            float ry = BitConverter.ToSingle(receivedBytes, offset); offset += 4;
            float rz = BitConverter.ToSingle(receivedBytes, offset); offset += 4;
            float rw = BitConverter.ToSingle(receivedBytes, offset); offset += 4;

            Objects[i].transform.position = new Vector3(px, py, pz);
            Objects[i].transform.rotation = new Quaternion(rx, ry, rz, rw);
        }
    }
}
