// Copyright 2022-2025 Niantic.
using Niantic.Lightship.AR.LocationAR;
using Niantic.Lightship.AR.Subsystems;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Niantic.Lightship.AR.Samples
{
    public class ARLocationSelection : MonoBehaviour
    {
        [Tooltip("Button to select the AR Location")] [SerializeField]
        private Button selectbutton;

        [Tooltip("The text used to show the name of the AR Location")] [SerializeField]
        private Text arLocationText;

        public UnityAction ARLocationSelected { get; set; }

        private void OnEnable()
        {
            selectbutton.onClick.AddListener(SelectARLocation);
        }

        private void OnDisable()
        {
            selectbutton.onClick.RemoveListener(SelectARLocation);
        }

        public void Initialize(ARLocation arLocation)
        {
            arLocationText.text = arLocation.name;
        }

        private void SelectARLocation()
        {
            ARLocationSelected?.Invoke();
        }
    }
}
