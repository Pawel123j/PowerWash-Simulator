using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using Zmywak.Kitchen.Dishes;
using Zmywak.Kitchen.Dishwasher;
using Zmywak.Kitchen.Orders;
using Zmywak.Kitchen.Pile;
using Zmywak.Kitchen.Rack;
using Zmywak.Kitchen.Shifts;
using Zmywak.UI.Common;
using Zmywak.UI.HUD;
using Zmywak.UI.Screens;

namespace Zmywak.EditorTools
{
    /// <summary>
    /// The second stage's screens, built like the rest (in code, wired through serialized fields): the settings card every
    /// scene shares, the kitchen's result screen and the guided first plate's overlay, the pause menu's way out, the
    /// evening's menu and its shop. Kept apart from the scene builders, which call these.
    /// </summary>
    public static partial class VerticalSliceSetup
    {
        private static readonly Color SecondaryButton = new Color(0.22f, 0.3f, 0.38f);
        private static readonly Color CardColor = new Color(0.09f, 0.1f, 0.11f, 0.98f);

        // What the QA pass changed in the generated UI (PWS-013, 018, 027: QA_REPORT.md). UiPolishScenePatch applies the same values to
        // scenes that already exist, so they need no new Run All (and no new light bake).
        /// <summary>How dark the screen behind a modal card is: 0.7 let the menu's title and the hub's stats show through it.</summary>
        public const float ModalDimAlpha = 0.92f;
        /// <summary>The same for the result screen, 0.82 before: the clock and the tool names of the kitchen showed round the card.</summary>
        public const float ResultDimAlpha = 0.94f;
        /// <summary>The sound switch's own words say what a tick means: it read "Dźwięk (wyłączony: ...)" while the sound was on.</summary>
        public const string SoundToggleLabel = "Dźwięk włączony";
        private static readonly Color Gold = new Color(1f, 0.83f, 0.3f);

        // ------------------------------------------------------------------ settings (menu, evening, pause)

        /// <summary>
        /// The settings card, on two tabs - sound and screen; the game - every row a full 130 units to touch (48 dp on a small,
        /// dense phone). Named SettingsPanel and returned switched off; whoever opens it wires a button to it.
        /// </summary>
        private static GameObject BuildSettingsPanel(Transform parent)
        {
            GameObject panel = CreateModal("SettingsPanel", parent, new Vector2(980, 1560), "Ustawienia");

            Button soundTabButton = CreateButton("SoundTabButton", panel.transform, "Dźwięk i ekran");
            AnchorCenter(RT(soundTabButton.gameObject), new Vector2(430, 130), new Vector2(-222, 580));
            Button gameTabButton = CreateButton("GameTabButton", panel.transform, "Rozgrywka");
            AnchorCenter(RT(gameTabButton.gameObject), new Vector2(430, 130), new Vector2(222, 580));

            GameObject soundTab = CreateUIObject("SoundTab", panel.transform);
            StretchFull(RT(soundTab));
            GameObject gameTab = CreateUIObject("GameTab", panel.transform);
            StretchFull(RT(gameTab));

            Toggle soundToggle = CreateToggle("SoundToggle", soundTab.transform, SoundToggleLabel, new Vector2(0, 425));
            Slider master = CreateSliderRow("MasterVolume", soundTab.transform, "Głośność", 280, 0f, 1f, out TMP_Text masterValue);
            Slider music = CreateSliderRow("MusicVolume", soundTab.transform, "Muzyka i radio", 135, 0f, 1f, out TMP_Text musicValue);
            Slider sfx = CreateSliderRow("SfxVolume", soundTab.transform, "Efekty", -10, 0f, 1f, out TMP_Text sfxValue);
            Slider textScale = CreateSliderRow("TextScale", soundTab.transform, "Rozmiar tekstu", -155, 0.8f, 1.5f, out TMP_Text textScaleValue);
            Toggle screenEffects = CreateToggle("ScreenEffectsToggle", soundTab.transform, "Efekty ekranu (wstrząsy, błyski)", new Vector2(0, -300));
            Toggle colorBlind = CreateToggle("ColorBlindToggle", soundTab.transform, "Tryb dla daltonistów (kolory brudu)", new Vector2(0, -445));

            Toggle haptics = CreateToggle("HapticsToggle", gameTab.transform, "Wibracje", new Vector2(0, 425));
            Toggle tilt = CreateToggle("TiltToggle", gameTab.transform, "Przechylanie telefonu (szklanki pod światło)", new Vector2(0, 280));
            Toggle relaxed = CreateToggle("RelaxedModeToggle", gameTab.transform, "Tryb relaks (dłuższy czas, bez zawalania Góry)", new Vector2(0, 135));
            Toggle toolAssist = CreateToggle("ToolAssistToggle", gameTab.transform, "Łatwiejszy zlew: właściwe narzędzie wybiera się samo", new Vector2(0, -10));
            Toggle autoDishwasher = CreateToggle("AutoDishwasherToggle", gameTab.transform, "Pełny kosz sam uruchamia zmywarkę (po 3 s)", new Vector2(0, -155));
            Button quality = CreateButton("QualityButton", gameTab.transform, "Grafika: automatyczna");
            AnchorCenter(RT(quality.gameObject), new Vector2(880, 130), new Vector2(0, -300));
            gameTab.SetActive(false);

            Button close = CreateButton("CloseSettingsButton", panel.transform, "Zamknij");
            AnchorCenter(RT(close.gameObject), new Vector2(360, 130), new Vector2(0, -650));

            SettingsScreen screen = panel.AddComponent<SettingsScreen>();
            var so = new SerializedObject(screen);
            so.FindProperty("panelRoot").objectReferenceValue = panel;
            so.FindProperty("textScaleSlider").objectReferenceValue = textScale;
            so.FindProperty("textScaleValueText").objectReferenceValue = textScaleValue;
            so.FindProperty("colorBlindToggle").objectReferenceValue = colorBlind;
            so.FindProperty("hapticsToggle").objectReferenceValue = haptics;
            so.FindProperty("soundToggle").objectReferenceValue = soundToggle;
            so.FindProperty("tiltToggle").objectReferenceValue = tilt;
            so.FindProperty("relaxedModeToggle").objectReferenceValue = relaxed;
            so.FindProperty("closeButton").objectReferenceValue = close;
            so.FindProperty("masterSlider").objectReferenceValue = master;
            so.FindProperty("masterValueText").objectReferenceValue = masterValue;
            so.FindProperty("musicSlider").objectReferenceValue = music;
            so.FindProperty("musicValueText").objectReferenceValue = musicValue;
            so.FindProperty("sfxSlider").objectReferenceValue = sfx;
            so.FindProperty("sfxValueText").objectReferenceValue = sfxValue;
            so.FindProperty("screenEffectsToggle").objectReferenceValue = screenEffects;
            so.FindProperty("toolAssistToggle").objectReferenceValue = toolAssist;
            so.FindProperty("autoDishwasherToggle").objectReferenceValue = autoDishwasher;
            so.FindProperty("qualityButton").objectReferenceValue = quality;
            so.FindProperty("soundTabButton").objectReferenceValue = soundTabButton;
            so.FindProperty("gameTabButton").objectReferenceValue = gameTabButton;
            so.FindProperty("soundTab").objectReferenceValue = soundTab;
            so.FindProperty("gameTab").objectReferenceValue = gameTab;
            so.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
            return panel;
        }

        /// <summary>A labelled slider row: the name on the left, the track (touchable over the whole 130-unit row), the value.</summary>
        private static Slider CreateSliderRow(string name, Transform parent, string label, float y, float min, float max, out TMP_Text valueText)
        {
            TextMeshProUGUI labelText = CreateText($"{name}Label", parent, label, 32, TextAlignmentOptions.MidlineLeft);
            AnchorCenter(RT(labelText.gameObject), new Vector2(300, 100), new Vector2(-300, y));
            labelText.enableAutoSizing = true;
            labelText.fontSizeMin = 22;
            labelText.fontSizeMax = 32;
            labelText.gameObject.AddComponent<Zmywak.UI.Accessibility.TextScaler>();

            GameObject sliderGo = CreateUIObject($"{name}Slider", parent);
            AnchorCenter(RT(sliderGo), new Vector2(440, 130), new Vector2(90, y));
            Image hitArea = sliderGo.AddComponent<Image>();
            hitArea.color = new Color(1f, 1f, 1f, 0f);
            GameObject track = CreateUIObject("Track", sliderGo.transform);
            RectTransform trackRect = RT(track);
            trackRect.anchorMin = new Vector2(0f, 0.5f);
            trackRect.anchorMax = new Vector2(1f, 0.5f);
            trackRect.sizeDelta = new Vector2(0f, 36f);
            Image trackImage = track.AddComponent<Image>();
            trackImage.sprite = UiArt.Rounded;
            trackImage.type = Image.Type.Sliced;
            trackImage.color = new Color(1f, 1f, 1f, 0.15f);
            trackImage.raycastTarget = false;
            Slider slider = sliderGo.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = max;

            GameObject fillArea = CreateUIObject("Fill Area", sliderGo.transform);
            RectTransform fillAreaRect = RT(fillArea);
            fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
            fillAreaRect.sizeDelta = new Vector2(0f, 36f);
            GameObject fill = CreateUIObject("Fill", fillArea.transform);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.sprite = UiArt.Rounded;
            fillImage.type = Image.Type.Sliced;
            fillImage.color = ButtonGreen;
            fillImage.raycastTarget = false;
            StretchFull(RT(fill));
            slider.fillRect = RT(fill);
            slider.targetGraphic = fillImage;

            GameObject handleArea = CreateUIObject("Handle Slide Area", sliderGo.transform);
            RectTransform handleAreaRect = RT(handleArea);
            handleAreaRect.anchorMin = new Vector2(0f, 0.5f);
            handleAreaRect.anchorMax = new Vector2(1f, 0.5f);
            handleAreaRect.sizeDelta = new Vector2(-40f, 0f);
            GameObject handle = CreateUIObject("Handle", handleArea.transform);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.sprite = UiArt.Circle;
            handleImage.color = Color.white;
            handleImage.raycastTarget = false;
            RT(handle).sizeDelta = new Vector2(60, 60);
            slider.handleRect = RT(handle);

            valueText = CreateText($"{name}Value", parent, "100%", 30, TextAlignmentOptions.MidlineRight);
            AnchorCenter(RT(valueText.gameObject), new Vector2(120, 80), new Vector2(395, y));
            return slider;
        }

        // ------------------------------------------------------------------ the main menu

        /// <summary>A small card with a question and two answers (Tak / Nie), switched off.</summary>
        private static GameObject BuildConfirm(string name, Transform parent, string title, string question, string yes, string no,
            out Button yesButton, out Button noButton, out TMP_Text questionText)
        {
            GameObject panel = CreateModal(name, parent, new Vector2(900, 620), title);
            questionText = CreateText("Question", panel.transform, question, 34, TextAlignmentOptions.Center);
            AnchorCenter(RT(questionText.gameObject), new Vector2(800, 200), new Vector2(0, 90));
            questionText.color = new Color(0.88f, 0.92f, 0.95f);
            Scalable(questionText);
            yesButton = CreateButton($"{name}YesButton", panel.transform, yes);
            AnchorCenter(RT(yesButton.gameObject), new Vector2(370, 140), new Vector2(-200, -170));
            noButton = CreateButton($"{name}NoButton", panel.transform, no);
            noButton.GetComponent<Image>().color = SecondaryButton;
            AnchorCenter(RT(noButton.gameObject), new Vector2(370, 140), new Vector2(200, -170));
            panel.SetActive(false);
            return panel;
        }

        // ------------------------------------------------------------------ the kitchen: the result screen

        /// <summary>
        /// The end of the shift as a screen of its own (ShiftResultScreen): the grade with its score, the night's rows, the
        /// pay's rows counting up to the total, the rank's bar and a promotion - over a dimmed kitchen, switched off.
        /// </summary>
        private static ShiftResultScreen BuildResultScreen(Transform parent)
        {
            GameObject root = CreateUIObject("ResultScreen", parent);
            StretchFull(RT(root));
            Image dim = root.AddComponent<Image>();
            dim.color = new Color(0.02f, 0.02f, 0.03f, ResultDimAlpha);
            CanvasGroup group = root.AddComponent<CanvasGroup>();

            Image card = CreatePanel("Card", root.transform, CardColor);
            RectTransform cardRect = RT(card.gameObject);
            AnchorCenter(cardRect, new Vector2(1000, 1760), Vector2.zero);
            Shadow shadow = card.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shadow.effectDistance = new Vector2(0f, -10f);

            TextMeshProUGUI title = CreateText("Title", card.transform, "Koniec zmiany", 38, TextAlignmentOptions.Center);
            title.color = new Color(0.85f, 0.9f, 0.95f);
            AnchorTopCenter(RT(title.gameObject), new Vector2(920, 60), new Vector2(0, -30));
            Scalable(title);

            // The grade in a ring on the left; the score, its bar and the verdict beside it.
            Image gradeRing = CreateUIObject("GradeRing", card.transform).AddComponent<Image>();
            gradeRing.sprite = UiArt.Ring;
            gradeRing.color = new Color(1f, 1f, 1f, 0.25f);
            gradeRing.raycastTarget = false;
            AnchorTopLeft(RT(gradeRing.gameObject), new Vector2(250, 250), new Vector2(50, -110));
            TextMeshProUGUI grade = CreateText("Grade", gradeRing.transform, "S", 170, TextAlignmentOptions.Center);
            grade.fontStyle = FontStyles.Bold;
            StretchFull(RT(grade.gameObject));

            TextMeshProUGUI score = CreateText("Score", card.transform, "0/100", 58, TextAlignmentOptions.MidlineLeft);
            score.fontStyle = FontStyles.Bold;
            AnchorTopLeft(RT(score.gameObject), new Vector2(560, 80), new Vector2(350, -130));
            Image scoreTrack = CreatePanel("ScoreBar", card.transform, new Color(1f, 1f, 1f, 0.1f));
            AnchorTopLeft(RT(scoreTrack.gameObject), new Vector2(560, 26), new Vector2(350, -222));
            Image scoreFill = CreatePanel("Fill", scoreTrack.transform, Gold);
            StretchFull(RT(scoreFill.gameObject));
            scoreFill.enabled = false;
            TextMeshProUGUI verdict = CreateText("Verdict", card.transform, string.Empty, 30, TextAlignmentOptions.TopLeft);
            verdict.fontStyle = FontStyles.Italic;
            verdict.color = new Color(0.85f, 0.88f, 0.9f);
            AnchorTopLeft(RT(verdict.gameObject), new Vector2(580, 100), new Vector2(350, -262));
            Scalable(verdict);

            Image recordBadge = CreatePanel("RecordBadge", card.transform, new Color(1f, 0.83f, 0.3f, 0.95f));
            AnchorTopCenter(RT(recordBadge.gameObject), new Vector2(760, 64), new Vector2(0, -385));
            TextMeshProUGUI record = CreateText("RecordText", recordBadge.transform, string.Empty, 30, TextAlignmentOptions.Center);
            record.color = new Color(0.2f, 0.12f, 0.02f);
            record.fontStyle = FontStyles.Bold;
            StretchFull(RT(record.gameObject));
            Scalable(record);
            recordBadge.gameObject.SetActive(false);

            Section(card.transform, "Zmiana", -470);
            const int statRows = 8;
            var statLabels = new TMP_Text[statRows];
            var statValues = new TMP_Text[statRows];
            for (int i = 0; i < statRows; i++)
            {
                Row(card.transform, $"Stat{i}", -520 - i * 50, out statLabels[i], out statValues[i]);
            }

            Section(card.transform, "Wypłata", -935);
            const int payRows = 7;
            var payLabels = new TMP_Text[payRows];
            var payValues = new TMP_Text[payRows];
            for (int i = 0; i < payRows; i++)
            {
                Row(card.transform, $"Pay{i}", -985 - i * 46, out payLabels[i], out payValues[i]);
            }

            Image totalLine = CreatePanel("TotalLine", card.transform, new Color(1f, 1f, 1f, 0.12f));
            AnchorTopCenter(RT(totalLine.gameObject), new Vector2(900, 4), new Vector2(0, -1316));
            TextMeshProUGUI totalLabel = CreateText("TotalLabel", card.transform, "Do kieszeni", 36, TextAlignmentOptions.MidlineLeft);
            totalLabel.fontStyle = FontStyles.Bold;
            AnchorTopLeft(RT(totalLabel.gameObject), new Vector2(420, 80), new Vector2(50, -1328));
            TextMeshProUGUI total = CreateText("Total", card.transform, string.Empty, 60, TextAlignmentOptions.MidlineRight);
            total.fontStyle = FontStyles.Bold;
            total.color = Gold;
            RectTransform totalRect = RT(total.gameObject);
            totalRect.anchorMin = totalRect.anchorMax = new Vector2(1f, 1f);
            totalRect.pivot = new Vector2(1f, 1f);
            totalRect.sizeDelta = new Vector2(520, 80);
            totalRect.anchoredPosition = new Vector2(-50, -1328);

            // The rank: its title, how far to the next, the bar - and the promotion banner over it when one comes.
            TextMeshProUGUI rank = CreateText("Rank", card.transform, string.Empty, 34, TextAlignmentOptions.MidlineLeft);
            rank.fontStyle = FontStyles.Bold;
            AnchorTopLeft(RT(rank.gameObject), new Vector2(440, 60), new Vector2(50, -1425));
            Scalable(rank);
            TextMeshProUGUI rankNext = CreateText("RankNext", card.transform, string.Empty, 26, TextAlignmentOptions.MidlineRight);
            rankNext.color = new Color(0.8f, 0.85f, 0.9f);
            RectTransform rankNextRect = RT(rankNext.gameObject);
            rankNextRect.anchorMin = rankNextRect.anchorMax = new Vector2(1f, 1f);
            rankNextRect.pivot = new Vector2(1f, 1f);
            rankNextRect.sizeDelta = new Vector2(460, 60);
            rankNextRect.anchoredPosition = new Vector2(-50, -1425);
            Scalable(rankNext);
            Image rankTrack = CreatePanel("RankBar", card.transform, new Color(1f, 1f, 1f, 0.1f));
            AnchorTopCenter(RT(rankTrack.gameObject), new Vector2(900, 28), new Vector2(0, -1492));
            Image rankFill = CreatePanel("Fill", rankTrack.transform, new Color(0.45f, 0.85f, 0.6f));
            StretchFull(RT(rankFill.gameObject));
            rankFill.enabled = false;

            Image promotion = CreatePanel("PromotionBanner", card.transform, Gold);
            AnchorTopCenter(RT(promotion.gameObject), new Vector2(820, 96), new Vector2(0, -1410));
            Shadow promotionShadow = promotion.gameObject.AddComponent<Shadow>();
            promotionShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            promotionShadow.effectDistance = new Vector2(0f, -6f);
            TextMeshProUGUI promotionText = CreateText("PromotionText", promotion.transform, string.Empty, 44, TextAlignmentOptions.Center);
            promotionText.color = new Color(0.2f, 0.12f, 0.02f);
            promotionText.fontStyle = FontStyles.Bold;
            StretchFull(RT(promotionText.gameObject));
            promotion.gameObject.SetActive(false);

            // Named as the old card's button, so the autoplay and the tour find their way home.
            Button home = CreateButton("BackToMenuButton", card.transform, "Wróć do domu");
            RectTransform homeRect = RT(home.gameObject);
            homeRect.anchorMin = homeRect.anchorMax = new Vector2(0.5f, 0f);
            homeRect.pivot = new Vector2(0.5f, 0f);
            homeRect.sizeDelta = new Vector2(620, 140);
            homeRect.anchoredPosition = new Vector2(0, 40);

            ShiftResultScreen screen = root.AddComponent<ShiftResultScreen>();
            var so = new SerializedObject(screen);
            so.FindProperty("root").objectReferenceValue = root;
            so.FindProperty("group").objectReferenceValue = group;
            so.FindProperty("card").objectReferenceValue = cardRect;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("gradeText").objectReferenceValue = grade;
            so.FindProperty("scoreText").objectReferenceValue = score;
            so.FindProperty("scoreFill").objectReferenceValue = scoreFill;
            so.FindProperty("verdictText").objectReferenceValue = verdict;
            so.FindProperty("recordBadge").objectReferenceValue = recordBadge.gameObject;
            so.FindProperty("recordText").objectReferenceValue = record;
            SetArray(so.FindProperty("statLabels"), statLabels);
            SetArray(so.FindProperty("statValues"), statValues);
            SetArray(so.FindProperty("payLabels"), payLabels);
            SetArray(so.FindProperty("payValues"), payValues);
            so.FindProperty("totalText").objectReferenceValue = total;
            so.FindProperty("rankText").objectReferenceValue = rank;
            so.FindProperty("rankFill").objectReferenceValue = rankFill;
            so.FindProperty("rankNextText").objectReferenceValue = rankNext;
            so.FindProperty("promotionBanner").objectReferenceValue = promotion.gameObject;
            so.FindProperty("promotionText").objectReferenceValue = promotionText;
            so.FindProperty("homeButton").objectReferenceValue = home;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            return screen;
        }

        private static void Section(Transform parent, string title, float y)
        {
            TextMeshProUGUI text = CreateText($"Section_{title}", parent, title.ToUpperInvariant(), 26, TextAlignmentOptions.MidlineLeft);
            text.color = new Color(1f, 0.83f, 0.3f, 0.85f);
            text.fontStyle = FontStyles.Bold;
            text.characterSpacing = 6f;
            AnchorTopLeft(RT(text.gameObject), new Vector2(600, 44), new Vector2(50, y));
        }

        private static void Row(Transform parent, string name, float y, out TMP_Text label, out TMP_Text value)
        {
            TextMeshProUGUI left = CreateText($"{name}Label", parent, string.Empty, 32, TextAlignmentOptions.MidlineLeft);
            left.color = new Color(0.82f, 0.86f, 0.9f);
            AnchorTopLeft(RT(left.gameObject), new Vector2(520, 48), new Vector2(50, y));
            Scalable(left);
            TextMeshProUGUI right = CreateText($"{name}Value", parent, string.Empty, 32, TextAlignmentOptions.MidlineRight);
            right.fontStyle = FontStyles.Bold;
            RectTransform rightRect = RT(right.gameObject);
            rightRect.anchorMin = rightRect.anchorMax = new Vector2(1f, 1f);
            rightRect.pivot = new Vector2(1f, 1f);
            rightRect.sizeDelta = new Vector2(380, 48);
            rightRect.anchoredPosition = new Vector2(-50, y);
            Scalable(right);
            label = left;
            value = right;
        }

        private static void SetArray(SerializedProperty property, Object[] items)
        {
            property.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
        }

        // ------------------------------------------------------------------ the kitchen: the guided first plate

        /// <summary>
        /// The guided first plate's overlay (KitchenTutorial): a card with one line, a pulsing ring and an arrow over what to
        /// touch, a finger showing the gesture. Over the HUD, under the pause and the result; it never takes a touch.
        /// </summary>
        private static KitchenTutorial BuildTutorialOverlay(Transform canvas, ShiftController shiftController, WashStation washStation,
            RackGrid rackGrid, DishwasherController dishwasher, DishPileController pile, ChefOrderController orders, RectTransform toolbar,
            CanvasGroup toolbarGroup, RectTransform dishwasherButton)
        {
            GameObject overlay = CreateUIObject("TutorialOverlay", canvas);
            StretchFull(RT(overlay));

            Image ring = CreateUIObject("Ring", overlay.transform).AddComponent<Image>();
            ring.sprite = UiArt.Ring;
            ring.color = new Color(1f, 0.85f, 0.3f, 0.95f);
            ring.raycastTarget = false;
            AnchorCenter(RT(ring.gameObject), new Vector2(220, 220), Vector2.zero);

            Image arrow = CreateUIObject("Arrow", overlay.transform).AddComponent<Image>();
            arrow.sprite = UiArt.Arrow;
            arrow.color = new Color(1f, 0.85f, 0.3f);
            arrow.raycastTarget = false;
            AnchorCenter(RT(arrow.gameObject), new Vector2(96, 120), Vector2.zero);

            Image finger = CreateUIObject("Finger", overlay.transform).AddComponent<Image>();
            finger.sprite = UiArt.Finger;
            finger.raycastTarget = false;
            AnchorCenter(RT(finger.gameObject), new Vector2(130, 130), Vector2.zero);

            Image card = CreatePanel("CoachCard", overlay.transform, new Color(0.06f, 0.07f, 0.08f, 0.94f));
            card.raycastTarget = false;
            RectTransform cardRect = RT(card.gameObject);
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.8f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(960, 210);
            Outline outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.83f, 0.3f, 0.6f);
            outline.effectDistance = new Vector2(3f, -3f);
            TextMeshProUGUI text = CreateText("CoachText", card.transform, string.Empty, 38, TextAlignmentOptions.Center);
            StretchFull(RT(text.gameObject));
            text.margin = new Vector4(40f, 24f, 40f, 24f);
            text.raycastTarget = false;
            Scalable(text);
            TextMeshProUGUI step = CreateText("StepText", card.transform, string.Empty, 24, TextAlignmentOptions.TopRight);
            step.color = new Color(1f, 0.83f, 0.3f, 0.8f);
            step.raycastTarget = false;
            AnchorTopRight(RT(step.gameObject), new Vector2(120, 40), new Vector2(-18, -10));

            GameObject controller = CreateUIObject("KitchenTutorial", canvas);
            KitchenTutorial tutorial = controller.AddComponent<KitchenTutorial>();
            var so = new SerializedObject(tutorial);
            so.FindProperty("shiftController").objectReferenceValue = shiftController;
            so.FindProperty("washStation").objectReferenceValue = washStation;
            so.FindProperty("rackGrid").objectReferenceValue = rackGrid;
            so.FindProperty("dishwasher").objectReferenceValue = dishwasher;
            so.FindProperty("pile").objectReferenceValue = pile;
            so.FindProperty("chefOrders").objectReferenceValue = orders;
            so.FindProperty("toolbar").objectReferenceValue = toolbar;
            so.FindProperty("toolbarGroup").objectReferenceValue = toolbarGroup;
            so.FindProperty("dishwasherButton").objectReferenceValue = dishwasherButton;
            so.FindProperty("overlay").objectReferenceValue = RT(overlay);
            so.FindProperty("coachCard").objectReferenceValue = cardRect;
            so.FindProperty("coachText").objectReferenceValue = text;
            so.FindProperty("stepText").objectReferenceValue = step;
            so.FindProperty("ring").objectReferenceValue = RT(ring.gameObject);
            so.FindProperty("arrow").objectReferenceValue = RT(arrow.gameObject);
            so.FindProperty("finger").objectReferenceValue = RT(finger.gameObject);
            so.ApplyModifiedPropertiesWithoutUndo();

            card.gameObject.SetActive(false);
            ring.gameObject.SetActive(false);
            arrow.gameObject.SetActive(false);
            finger.gameObject.SetActive(false);
            return tutorial;
        }

        // ------------------------------------------------------------------ the evening: the menu and the shop

        /// <summary>The evening's menu (back with nothing open, or its button): back to the evening, the settings, the main menu.</summary>
        private static GameObject BuildHubMenu(Transform parent, out Button resume, out Button settings, out Button mainMenu, out TMP_Text note)
        {
            GameObject panel = CreateModal("HubMenuPanel", parent, new Vector2(900, 900), "Menu");
            resume = CreateButton("HubMenuResumeButton", panel.transform, "Wracam do wieczoru");
            AnchorCenter(RT(resume.gameObject), new Vector2(620, 140), new Vector2(0, 220));
            settings = CreateButton("HubMenuSettingsButton", panel.transform, "Ustawienia");
            settings.GetComponent<Image>().color = SecondaryButton;
            AnchorCenter(RT(settings.gameObject), new Vector2(620, 140), new Vector2(0, 50));
            mainMenu = CreateButton("HubMenuMainMenuButton", panel.transform, "Menu główne");
            mainMenu.GetComponent<Image>().color = SecondaryButton;
            AnchorCenter(RT(mainMenu.gameObject), new Vector2(620, 140), new Vector2(0, -120));
            note = CreateText("HubMenuNote", panel.transform, "Wszystko zapisuje się na bieżąco.", 28, TextAlignmentOptions.Center);
            note.color = new Color(0.8f, 0.85f, 0.9f);
            AnchorCenter(RT(note.gameObject), new Vector2(780, 80), new Vector2(0, -280));
            Scalable(note);
            panel.SetActive(false);
            return panel;
        }

        /// <summary>The few things worth buying (MetaHubScreen fills in the names, prices and what each is good for).</summary>
        private static GameObject BuildShop(Transform parent, out Button[] buy, out TMP_Text[] notes, out Button close)
        {
            GameObject panel = CreateModal("ShopPanel", parent, new Vector2(960, 1200), "Zakupy");
            TextMeshProUGUI intro = CreateText("ShopIntro", panel.transform, "Raz kupione zostaje. Nigdy za pieniądze na rachunki.", 28,
                TextAlignmentOptions.Center);
            intro.color = new Color(0.8f, 0.85f, 0.9f);
            AnchorCenter(RT(intro.gameObject), new Vector2(860, 70), new Vector2(0, 420));
            Scalable(intro);
            buy = new Button[3];
            notes = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                float y = 250 - i * 250;
                buy[i] = CreateButton($"BuyButton{i + 1}", panel.transform, string.Empty);
                buy[i].GetComponent<Image>().color = SecondaryButton;
                AnchorCenter(RT(buy[i].gameObject), new Vector2(360, 150), new Vector2(-250, y));
                TextMeshProUGUI note = CreateText($"BuyNote{i + 1}", panel.transform, string.Empty, 28, TextAlignmentOptions.MidlineLeft);
                note.color = new Color(0.85f, 0.9f, 0.95f);
                AnchorCenter(RT(note.gameObject), new Vector2(440, 150), new Vector2(190, y));
                Scalable(note);
                notes[i] = note;
            }

            close = CreateButton("CloseShopButton", panel.transform, "Zamknij");
            AnchorCenter(RT(close.gameObject), new Vector2(360, 130), new Vector2(0, -490));
            UnityEventTools.AddBoolPersistentListener(close.onClick, panel.SetActive, false);
            panel.SetActive(false);
            return panel;
        }
    }
}
