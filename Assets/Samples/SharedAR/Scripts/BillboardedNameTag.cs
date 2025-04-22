// Copyright 2022-2025 Niantic.
using System.Collections.Generic;

using Unity.Collections;
using Unity.Netcode;

using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Niantic.Lightship.AR.Samples
{
    public class BillboardedNameTag : NetworkBehaviour
    {
        private Transform _arCameraTransform;

        [Tooltip("List of names to randomly choose from")]
        [SerializeField]
        private List<string> NameList = new List<string>()
        {
            "Doty",
            "Momo",
            "Everest",
            "Snowy",
            "Rabbit",
            "Dot",
            "Sheep",
            "Brick",
            "Wheat",
            "Ore",
            "Wood"
        };

        [Tooltip("Renderer gameobject to disable on self")]
        [SerializeField]
        private GameObject RendererContainer;

        [Tooltip("Text mesh to render the name")]
        [SerializeField]
        private TextMesh NameTag;

        private NetworkVariable<FixedString32Bytes> networkedName =
            new NetworkVariable<FixedString32Bytes>
            (
                default,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Owner
            );
        
        private NetworkVariable<bool> networkedTrackingState = 
            new NetworkVariable<bool>
        (
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        private SpriteRenderer _targetRenderer;

        public override void OnNetworkSpawn()
        {
            transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            if (Camera.main)
            {
                _arCameraTransform = Camera.main.transform;
            }

            if (IsOwner)
            {
                // Choose a random name from the list
                var randomName = NameList[Random.Range(0, NameList.Count - 1)];
                networkedName.Value = new FixedString32Bytes(randomName);

                // Disable the renderers on the local nametag
                RendererContainer.SetActive(false);
                ARSession.stateChanged += OnOwnerTrackingStateChanged;
            }
            else
            {
                // If this is not empty, apply it right away
                if (!networkedName.Value.IsEmpty)
                {
                    NameTag.text = networkedName.Value.Value;
                }

                networkedName.OnValueChanged += OnNameChanged;
                networkedTrackingState.OnValueChanged += OnTrackingStateChanged;
                _targetRenderer = RendererContainer.GetComponentInChildren<SpriteRenderer>();
            }

            base.OnNetworkSpawn();
        }

        private void OnOwnerTrackingStateChanged(ARSessionStateChangedEventArgs args)
        {
            if (args.state == ARSessionState.SessionTracking)
            {
                networkedTrackingState.Value = true;
            }
            else
            {
                networkedTrackingState.Value = false;
            }
        }

        private void OnTrackingStateChanged(bool previousvalue, bool newvalue)
        {
            if (newvalue)
            {
                if (_targetRenderer)
                {
                    _targetRenderer.color = Color.white;
                }
                NameTag.text = networkedName.Value.Value;
            }
            else
            {
                if (_targetRenderer)
                {
                    _targetRenderer.color = Color.red;
                }
                NameTag.text = "TRACKING LOST";
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                ARSession.stateChanged -= OnOwnerTrackingStateChanged;
            }

            if (OwnerClientId == NetworkManager.ServerClientId)
            {
                if (ImageTrackingColocalizationDemo.Instance)
                {
                    Debug.Log("Host disconnected!!");
                    ImageTrackingColocalizationDemo.Instance.HandleHostDisconnected();
                }
            }
            base.OnNetworkDespawn();
        }

        private void OnNameChanged(FixedString32Bytes previousvalue, FixedString32Bytes newvalue)
        {
            NameTag.text = newvalue.Value;
        }

        void Update()
        {
            // If the reference was not set up beforehand, try to get it now
            if (!_arCameraTransform && Camera.main)
            {
                _arCameraTransform = Camera.main.transform;
            }

            if (IsOwner)
            {
                if (_arCameraTransform)
                {
                    transform.position = _arCameraTransform.position;
                }
            }
            else
            {
                if (_arCameraTransform)
                {
                    // Billboard everyone else's nametag to the local camera
                    var posToLook = _arCameraTransform.position;

                    // Flatten y so the nametag is flat on screen
                    posToLook.y = transform.position.y;
                    transform.LookAt(posToLook);
                }
            }
        }
    }
}
