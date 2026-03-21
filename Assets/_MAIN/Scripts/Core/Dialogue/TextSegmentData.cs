using System.Collections.Generic;
using UnityEngine;

namespace DIALOGUE
{
    public class TextSegmentData
    {
        public string text;
        public float speedMult = 1f;
        public float pauseBefore = 0f;
        public bool isBold;
        public Color? color;
        public List<TextEffect> activeEffects = new List<TextEffect>();

        [System.Serializable]
        public struct TextEffect
        {
            public string type;
            public float intensity;
            public int startIndex;
            public int endIndex;
            public float startTime;
        }
    }
}