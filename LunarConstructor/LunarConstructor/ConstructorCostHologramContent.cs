using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace LunarConstructor
{
    public class ConstructorCostHologramContent : MonoBehaviour
    {
        public string displayValue;

        public TextMeshPro targetTextMesh;

        private string oldDisplayValue;

        public Color colorValue;

        private Color oldColorValue;

        private void FixedUpdate()
        {
            if (targetTextMesh && ((displayValue != oldDisplayValue) || (colorValue != oldColorValue)))
            {
                oldDisplayValue = displayValue;
                oldColorValue = colorValue;

                targetTextMesh.SetText(displayValue);
                targetTextMesh.color = colorValue;
            }
        }
    }
}
