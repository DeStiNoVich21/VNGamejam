using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace DIALOGUE
{
    public class TextVertexAnimator : MonoBehaviour
    {
        private TMP_Text textComponent;
        private List<TextSegmentData.TextEffect> activeEffects = new List<TextSegmentData.TextEffect>();

        private float glitchTimer = 0;
        private Vector3[] glitchOffsets;

        void Awake()
        {
            textComponent = GetComponent<TMP_Text>();
        }

        public void SetEffects(List<TextSegmentData.TextEffect> effects) => activeEffects = effects;

        void Update()
        {
            if (activeEffects.Count == 0) return;

            textComponent.ForceMeshUpdate();
            TMP_TextInfo textInfo = textComponent.textInfo;

            glitchTimer += Time.deltaTime;
            bool triggerGlitch = false;
            if (glitchTimer > 0.1f) { glitchTimer = 0; triggerGlitch = true; }

            foreach (var effect in activeEffects)
            {
                for (int i = effect.startIndex; i < effect.endIndex; i++)
                {
                    if (i >= textInfo.characterCount || i >= textComponent.maxVisibleCharacters) continue;

                    TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                    if (!charInfo.isVisible) continue;

                    int meshIndex = charInfo.materialReferenceIndex;
                    int vertexIndex = charInfo.vertexIndex;

                    Vector3[] vertices = textInfo.meshInfo[meshIndex].vertices;
                    Color32[] colors = textInfo.meshInfo[meshIndex].colors32;

                    ApplyEffect(effect, i, vertices, colors, vertexIndex, triggerGlitch);
                }
            }

            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
                textComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
        }

        private void ApplyEffect(TextSegmentData.TextEffect eff, int charIdx, Vector3[] verts, Color32[] colors, int vIdx, bool syncGlitch)
        {
            float t = Time.time;
            switch (eff.type)
            {
                case "shake":
                    Vector3 shakeOff = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)) * eff.intensity;
                    for (int j = 0; j < 4; j++) verts[vIdx + j] += shakeOff;
                    break;

                case "wave":
                    float y = Mathf.Sin(t * 3f + charIdx * 0.5f) * (eff.intensity * 2f);
                    for (int j = 0; j < 4; j++) verts[vIdx + j].y += y;
                    break;

                case "rainbow":
                    float hue = (t * eff.intensity + charIdx * 0.1f) % 1f;
                    Color32 rainbowCol = Color.HSVToRGB(hue, 0.8f, 1f);
                    for (int j = 0; j < 4; j++) colors[vIdx + j] = rainbowCol;
                    break;
                case "pop":
                    float popScale = 1f + Mathf.Max(0, Mathf.Sin(t * 10f) * 0.5f * eff.intensity);
                    Vector3 center = (verts[vIdx + 0] + verts[vIdx + 2]) / 2f;
                    for (int j = 0; j < 4; j++)
                        verts[vIdx + j] = center + (verts[vIdx + j] - center) * popScale;
                    break;
                case "glitch":
                    if (syncGlitch)
                    {
                        Vector3 glitchOff = new Vector3(UnityEngine.Random.Range(-2f, 2f), UnityEngine.Random.Range(-1f, 1f)) * eff.intensity;
                        for (int j = 0; j < 4; j++) verts[vIdx + j] += glitchOff;
                    }
                    break;
            }
        }
    }
}