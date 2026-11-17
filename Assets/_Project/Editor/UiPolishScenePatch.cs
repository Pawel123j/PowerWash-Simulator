using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zmywak.UI.Common;
using Zmywak.UI.HUD;

namespace Zmywak.EditorTools
{
    /// <summary>
    /// Puts the QA pass's small UI fixes (PWS-013, 018, 027; the values are VerticalSliceSetup's own constants) into the scenes that already
    /// exist, instead of a full "Run All" - which would also bake the light again, reshuffle every scene's ids and, with a busy GPU, crash.
    /// Idempotent: a scene already right is left as it is. Run from the menu, or in batch mode with
    /// <c>-executeMethod Zmywak.EditorTools.UiPolishScenePatch.Apply</c>. A later Run All builds the same things from the builder.
    /// </summary>
    public static class UiPolishScenePatch
    {
        /// <summary>The backdrop CreateModal used to make.</summary>
        private const float OldModalDimAlpha = 0.7f;

        private static readonly string[] Scenes =
        {
            "Assets/_Project/Scenes/MainMenu.unity",
            "Assets/_Project/Scenes/MetaHub.unity",
            "Assets/_Project/Scenes/Kitchen_Shift.unity"
        };

        [MenuItem("Zmywak/Setup/Patch UI polish into the scenes (no light bake)")]
        public static void Apply()
        {
            foreach (string path in Scenes)
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int changes = Patch();
                if (changes > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }

                Debug.Log($"UiPolishScenePatch: {path}: {changes} change(s)");
            }
        }

        /// <summary>Patches the scene that is open now; returns how many things it changed.</summary>
        public static int Patch()
        {
            int changes = 0;

            // PWS-027: every modal card CreateModal made sits on a darker backdrop. (Only those: the pause menu has a lighter one of its own
            // on purpose, the kitchen showing through it.)
            foreach (PanelTransition modal in Object.FindObjectsByType<PanelTransition>(FindObjectsInactive.Include))
            {
                Image dim = modal.GetComponent<Image>();
                if (dim != null && Mathf.Abs(dim.color.a - OldModalDimAlpha) < 0.01f)
                {
                    changes += SetAlpha(dim, VerticalSliceSetup.ModalDimAlpha);
                }
            }

            // ...and so does the result screen.
            foreach (ShiftResultScreen result in Object.FindObjectsByType<ShiftResultScreen>(FindObjectsInactive.Include))
            {
                changes += SetAlpha(result.GetComponent<Image>(), VerticalSliceSetup.ResultDimAlpha);
            }

            // PWS-018: the sound switch says what a tick means.
            foreach (Toggle toggle in Object.FindObjectsByType<Toggle>(FindObjectsInactive.Include))
            {
                if (toggle.name != "SoundToggle")
                {
                    continue;
                }

                TMP_Text label = toggle.GetComponentInChildren<TMP_Text>(true);
                if (label != null && label.text != VerticalSliceSetup.SoundToggleLabel)
                {
                    label.text = VerticalSliceSetup.SoundToggleLabel;
                    EditorUtility.SetDirty(label);
                    changes++;
                }
            }

            // PWS-013: the pile meter's word starts at the meter's left edge instead of being centred on it, 12 px off the screen.
            foreach (Transform meter in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (meter.name != "PileMeter")
                {
                    continue;
                }

                Transform label = meter.Find("Label");
                if (label == null || !(label is RectTransform rect))
                {
                    continue;
                }

                var left = new Vector2(0f, 1f);
                if (rect.anchorMin != left || rect.anchorMax != left || rect.pivot != Vector2.zero || rect.anchoredPosition != new Vector2(0f, 4f))
                {
                    rect.anchorMin = left;
                    rect.anchorMax = left;
                    rect.pivot = Vector2.zero;
                    rect.sizeDelta = new Vector2(90f, 34f);
                    rect.anchoredPosition = new Vector2(0f, 4f);
                    changes++;
                }

                var text = label.GetComponent<TMP_Text>();
                if (text != null && text.alignment != TextAlignmentOptions.BottomLeft)
                {
                    text.alignment = TextAlignmentOptions.BottomLeft;
                    EditorUtility.SetDirty(text);
                    changes++;
                }

                EditorUtility.SetDirty(rect);
            }

            // PWS-027: the hub's chapter line sits under the modals' backdrop, right behind the day, like the rest of the header.
            foreach (Transform chapter in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (chapter.name != "ChapterText" || chapter.parent == null)
                {
                    continue;
                }

                Transform day = chapter.parent.Find("DayText");
                if (day != null && chapter.GetSiblingIndex() != day.GetSiblingIndex() + 1)
                {
                    chapter.SetSiblingIndex(day.GetSiblingIndex() + 1);
                    EditorUtility.SetDirty(chapter.parent);
                    changes++;
                }
            }

            return changes;
        }

        private static int SetAlpha(Image image, float alpha)
        {
            if (image == null || Mathf.Abs(image.color.a - alpha) < 1e-4f)
            {
                return 0;
            }

            Color color = image.color;
            color.a = alpha;
            image.color = color;
            EditorUtility.SetDirty(image);
            return 1;
        }
    }
}
