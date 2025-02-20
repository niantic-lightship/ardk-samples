using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Niantic.Lightship.AR.Mapping;
using Unity.Netcode;
using UnityEngine;

public class DeviceMappingSharingManager : NetworkBehaviour
{
    public Action<DeviceMapChunk, ulong> OnSentSingleChunk;
    public Action<DeviceMapChunk, int, int> OnReceivedSingleChunk;
    public Action<byte[], int> OnReceivedAllChunks;
        
    private int _maxChunkSize; // 1KB
    private Queue<ulong> _waitingClientIdQueue = new ();
    private List<DeviceMapChunk> _sendChunkList = new ();
    private List<DeviceMapChunk> _receivedChunkList = new ();
    private bool _finishAddingAllChunks;
    private bool _isSending;
    
    public void InitializeOptions(int maxChunkSize)
    {
        _maxChunkSize = maxChunkSize;
    }

    public void AddClientToWaitingQueue(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected and add to the waiting list");
        _waitingClientIdQueue.Enqueue(clientId);
    }
    
    public void RemoveClientFromWaitingQueue(ulong clientId)
    {
        var newQueue = new Queue<ulong>();
        while (_waitingClientIdQueue.Count > 0)
        {
            var currentClientId = _waitingClientIdQueue.Dequeue();
            if (currentClientId == clientId) continue;
            newQueue.Enqueue(currentClientId);
        }
        _waitingClientIdQueue = newQueue;
    }

    public bool SetDeviceMapForSharing(ARDeviceMap deviceMap)
    {
        var serializedDeviceMap = deviceMap.Serialize();
        return SplitDeviceMapToChunksForSending(serializedDeviceMap);
    }
    
    /// <summary>
    ///  Split the devicemap data to small chunks
    /// </summary>
    /// <param name="serializedDeviceMap"></param>
    private bool SplitDeviceMapToChunksForSending(byte[] serializedDeviceMap)
    {
        int sent = 0;
        var len = serializedDeviceMap.Length;
        
        Debug.Log($"SerializedDeviceMap Length: {len}");
        
        int chunks = Mathf.CeilToInt((float)len/(float)_maxChunkSize);
        int index = 0;

        var _chunkSize = _maxChunkSize;

        for (int i = 0; i < len; i += _chunkSize)
        {
            if (sent + _chunkSize < len)
            {
                var destinationArray = new byte[_chunkSize];
                Array.Copy(serializedDeviceMap, sent, destinationArray, 0, _chunkSize);
                SetData(index, chunks, destinationArray);
            }
            else
            {
                // last chunk
                _chunkSize = len - sent;
                if (_chunkSize <= 0)
                    return true;
                
                var destinationArray = new byte[_chunkSize];
                if (sent < len)
                {
                    Array.Copy(serializedDeviceMap, sent, destinationArray, 0, _chunkSize);
                }
                
                SetData(index, chunks, destinationArray);
                return true;
            }

            index++;
            sent += _chunkSize;
        }

        return false;
    }
    
    private void SetData(int index, int chunks, byte[] data)
    {
        DeviceMapChunk c = new DeviceMapChunk(index, chunks, data);
        _sendChunkList.Add(c);
        if (index + 1 == chunks)
        {
            _finishAddingAllChunks = true;
        }
    }
    
    private void Update()
    {
        if (!IsHost) return;
        
        // if chunks are not ready, then skip
        if (!_finishAddingAllChunks) return;

        // if isSending, don't start the sending process for other clients.
        if (_isSending) return;
        
        // if no waiting client, then skip
        if (_waitingClientIdQueue.Count == 0) return;
        
        var receiverId = _waitingClientIdQueue.Dequeue();
        
        // Send data to the receiver
        StartCoroutine(SendDeviceMappingChunksToReceiverCoroutine(receiverId));
    }
    
    IEnumerator SendDeviceMappingChunksToReceiverCoroutine(ulong receiverId)
    {
        _isSending = true;
        
        foreach (var chunk in _sendChunkList)
        {
            SendByteDataClientRpc(chunk.ToByteArray(), RpcTarget.Single(receiverId, RpcTargetUse.Temp));
            Debug.Log($"Sent {chunk._index + 1}/{chunk._chunks} ({chunk._data.Length} bytes) to {receiverId}");
            OnSentSingleChunk?.Invoke(chunk, receiverId);
            // wait 0.1 sec in order not to send too much data at the same time
            yield return new WaitForSeconds(0.1f);
        }

        _isSending = false;
    }
    
    [Rpc(SendTo.SpecifiedInParams)]
    private void SendByteDataClientRpc(byte[] data, RpcParams rpcParams = default)
    {
        Debug.Log("Received byte data of length: " + data.Length);
        StashChunk(data);
    }
    
    private void StashChunk(byte[] chunkData)
    {
        DeviceMapChunk chunk = DeviceMapChunk.FromByteArray(chunkData);
        _receivedChunkList.Add(chunk);
        var receivedChunkNum = _receivedChunkList.Count;
        var chunkDataLength = chunkData.Length;
        OnReceivedSingleChunk?.Invoke(chunk, receivedChunkNum, chunkDataLength);

        // When received enough data for reconstruction
        if (_receivedChunkList.Count == chunk._chunks)
        {
            byte[] data;
            if (_receivedChunkList.Count == 1)
            {
                data = chunk._data;
            }
            else
            {
                _receivedChunkList = _receivedChunkList.OrderBy(x => x._index).ToList();
                int byteSize = 0;
                foreach (var c in _receivedChunkList)
                {
                    byteSize += c._data.Length;
                }
                data = new byte[byteSize];
                int k = 0;
                foreach (var c in _receivedChunkList)
                {
                    int chunkLen = c._data.Length;
                    Buffer.BlockCopy(c._data, 0, data, k, chunkLen);
                    k += chunkLen;
                }
            }

            var serializedDeviceMap = data;
            var allChunkNum = chunk._chunks;
            OnReceivedAllChunks?.Invoke(serializedDeviceMap, allChunkNum);
            
            _receivedChunkList.Clear();
        }
    }
    
    [Serializable]
    public struct DeviceMapChunk
    {
        public int _index;
        public int _chunks;
        public byte[] _data;
        
        public DeviceMapChunk(int index, int chunks, byte[] data)
        {
            this._index = index;
            this._chunks = chunks;
            this._data = data;
        }
        
        public static DeviceMapChunk FromByteArray(byte[] byteArray)
        {
            using (MemoryStream stream = new MemoryStream(byteArray))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return (DeviceMapChunk)formatter.Deserialize(stream);
            }
        }

        public byte[] ToByteArray()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, this);
                return stream.ToArray();
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        _sendChunkList.Clear();
        _waitingClientIdQueue.Clear();
        _receivedChunkList.Clear();
        
        _finishAddingAllChunks = false;
        _isSending = false;
        
        base.OnNetworkDespawn();
    }
}

