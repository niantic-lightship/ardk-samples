// Copyright 2022-2025 Niantic.

using Niantic.Lightship.AR.Samples;
using Unity.Netcode;
using UnityEngine;

namespace Scenes.SharedAR.VpsColocalization
{
    public class PlayerMarker : NetworkBehaviour
    {
        private Transform _arCameraTransform;

        public override void OnNetworkSpawn()
        {
            transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            if (IsOwner)
            {
                if (Camera.main)
                {
                    _arCameraTransform = Camera.main.transform;
                }
            }
            if (IsServer)
            {
                // TODO: add some server specific logic?
            }

            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            //
            if (OwnerClientId == NetworkManager.ServerClientId)
            {
                Debug.Log("Host disconnected!!");
            }
            base.OnNetworkDespawn();
        }
        void Update()
        {
            if (IsOwner)
            {
                if (_arCameraTransform)
                {
                    // Get local AR camera transform
                    _arCameraTransform.GetPositionAndRotation( out var pos,  out var rot);
                    // Since using the ClientNetworkTransform, just update world transform of the cube matching with the
                    // AR Camera's worldTransform. it's local transform will be synced.
                    transform.SetPositionAndRotation(pos ,rot);
                }

            }
        }
    }

}
