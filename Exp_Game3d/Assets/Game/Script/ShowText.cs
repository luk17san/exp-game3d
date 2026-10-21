using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Platformer
{
    public class ShowText : MonoBehaviour
    {
        public Text showStateText;
        private string currentState;

        void Start()
        {
            currentState = "Start"; // Ustawienie pocz¹tkowego stanu
            UpdateStateText();
        }

        void Update()
        {
            // Przyk³ad zmiany stanu
            if (Input.GetKeyDown(KeyCode.Space))
            {
                currentState = "State Changed!";
                UpdateStateText();
            }
        }

        void UpdateStateText()
        {
            showStateText.text = "Current State: " + currentState;
        }
    }
}
