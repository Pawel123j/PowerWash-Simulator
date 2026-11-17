using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using Zmywak.Narrative.LanguageBarrier;

namespace Zmywak.Tests.PlayMode
{
    /// <summary>An unknown word reaches the screen blurred, not as a printed tag: TextMeshPro finds the blurred font.</summary>
    public class WordBlurPlayModeTests
    {
        private GameObject _canvas;
        private GameObject _camera;

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_canvas);
            Object.Destroy(_camera);
        }

        [UnityTest]
        public IEnumerator AnUnknownWord_IsSetInTheBlurredFont_NotPrintedAsATag()
        {
            _canvas = new GameObject("Canvas", typeof(Canvas));
            TextMeshProUGUI text = Line(_canvas.transform, Vector2.zero);
            text.text = WordBlurFormatter.Format("the craic", new VocabularyTracker(new[] { "the" }));
            text.ForceMeshUpdate();
            yield return null;

            Assert.AreEqual("the " + WordBlurFormatter.Silhouette("craic"), text.GetParsedText(), "The tag was printed instead of parsed");
            TMP_CharacterInfo known = text.textInfo.characterInfo[0];
            TMP_CharacterInfo unknown = text.textInfo.characterInfo[4];
            Assert.AreNotEqual(WordBlurFormatter.BlurFont, known.fontAsset.name);
            Assert.AreEqual(WordBlurFormatter.BlurFont, unknown.fontAsset.name, "The unknown word is not set in the blurred font (Zmywak > Setup > Run All makes it)");
            foreach (char standIn in WordBlurFormatter.StandIns)
            {
                Assert.IsTrue(unknown.fontAsset.HasCharacter(standIn), $"The blurred font has no '{standIn}'");
            }
        }

        /// <summary>
        /// Not a check - the same line blurred at several strengths (the blurred font's softness and dilation), to
        /// judge by eye: Logs/Screenshots/art_word_blur.png. Explicit.
        /// </summary>
        [UnityTest, Explicit]
        public IEnumerator ArtPreview_BlurStrengths()
        {
            _camera = new GameObject("PreviewCamera", typeof(Camera));
            Camera camera = _camera.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.06f, 0.07f);
            _canvas = new GameObject("Canvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = _canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);

            var blurFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/" + WordBlurFormatter.BlurFont);
            Assert.IsNotNull(blurFont, "No blurred font (Zmywak > Setup > Run All makes it)");
            const string sentence = "Emerald Recruitment, Gerry speaking. Ah, the Polish lad! Small problem, the job is gone.";
            var known = new VocabularyTracker(new[] { "the", "ah" });
            float[] softness = { 1f, 1f, 0.7f, 0.5f, 0.3f };
            float[] dilate = { 0.25f, 0f, 0.2f, 0.2f, 0.2f };
            for (int i = 0; i < softness.Length; i++)
            {
                var variant = new Material(blurFont.material) { name = $"Blur preview {i}" };
                variant.SetFloat(ShaderUtilities.ID_OutlineSoftness, softness[i]);
                variant.SetFloat(ShaderUtilities.ID_FaceDilate, dilate[i]);
                ShaderUtilities.UpdateShaderRatios(variant);
                MaterialReferenceManager.AddFontMaterial((int)TMP_TextUtilities.GetHashCodeCaseInSensitive(variant.name), variant);

                string blurred = WordBlurFormatter.Format(sentence, known)
                    .Replace(WordBlurFormatter.Open, $"<font=\"{WordBlurFormatter.BlurFont}\" material=\"{variant.name}\">");
                TextMeshProUGUI text = Line(_canvas.transform, new Vector2(0f, 760f - i * 360f));
                text.text = $"<size=24><color=#E0B040>softness {softness[i]}, dilate {dilate[i]}</color></size>\n{blurred}";
            }

            yield return null;
            TestScreenshot.Capture(camera, "art_word_blur.png");
        }

        private static TextMeshProUGUI Line(Transform parent, Vector2 position)
        {
            var text = new GameObject("Line", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            text.transform.SetParent(parent, false);
            text.rectTransform.sizeDelta = new Vector2(980f, 300f);
            text.rectTransform.anchoredPosition = position;
            text.fontSize = 36;
            return text;
        }
    }
}
