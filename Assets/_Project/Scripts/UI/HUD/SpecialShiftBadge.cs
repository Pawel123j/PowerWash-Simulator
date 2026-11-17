using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zmywak.Core.Data;
using Zmywak.Kitchen.Shifts;

namespace Zmywak.UI.HUD
{
    /// <summary>
    /// The night's card beside the radio. A special shift's: its name, then - after a few seconds of telling why the night
    /// is different - how it is going: the newlyweds' envelope waiting at the end, the inspection clean so far or already
    /// failed, where the lasagne tray is, St. Patrick's double tips. On any other day of the schedule: the kind of shift
    /// and its quota, then the quota so far and where the service is (a rush coming, the peak, a breather). Hidden at the
    /// prep board, and for the kitchen on its own.
    /// </summary>
    public class SpecialShiftBadge : MonoBehaviour
    {
        private const float IntroSeconds = 6f;
        private static readonly Color ShiftTint = new Color(0.12f, 0.16f, 0.2f, 0.92f);
        private static readonly Color RushTint = new Color(0.45f, 0.22f, 0.08f, 0.95f);

        [SerializeField] private ShiftController shiftController;
        [SerializeField] private GameObject card;
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text statusText;

        private float _shownFor;
        private float _nextRefresh;

        /// <summary>What the card says under the title right now.</summary>
        public string Status => statusText != null ? statusText.text : string.Empty;

        private void Update()
        {
            bool special = shiftController != null && shiftController.Special != SpecialShift.None;
            bool visible = shiftController != null && (special || shiftController.IsScheduled) &&
                           !shiftController.AwaitingPrep && !shiftController.IsShiftOver;
            if (card != null && card.activeSelf != visible)
            {
                card.SetActive(visible);
                _nextRefresh = 0f; // shown: its text at once
            }

            if (!visible)
            {
                return;
            }

            // The card's lines change with a count or a phase, not every frame: worked out four times a second, not built
            // anew as strings sixty times.
            _shownFor += Time.unscaledDeltaTime;
            if (Time.unscaledTime < _nextRefresh)
            {
                return;
            }

            _nextRefresh = Time.unscaledTime + 0.25f;
            if (!special)
            {
                ShowShift();
                return;
            }

            SpecialShiftRules rules = shiftController.SpecialRules;
            titleText.text = rules.Title;
            statusText.text = _shownFor < IntroSeconds ? rules.Intro : Progress(rules);
            background.color = shiftController.InspectionFailed ? new Color(0.5f, 0.12f, 0.1f, 0.95f) : Tint(shiftController.Special);
        }

        /// <summary>A plain day's card: what kind of night it is and its quota; then the quota so far and the service's rhythm.</summary>
        private void ShowShift()
        {
            int quota = shiftController.Quota;
            titleText.text = shiftController.Title;
            if (_shownFor < IntroSeconds)
            {
                statusText.text = $"{shiftController.Intro} Norma: {PolishPlural.Dishes(quota)}.";
                background.color = ShiftTint;
                return;
            }

            int cleaned = shiftController.DishesCleanedTotal;
            string goal = cleaned >= quota ? $"Norma zrobiona ({cleaned}/{quota})!" : $"Norma: {cleaned}/{quota}.";
            switch (shiftController.Phase)
            {
                case ServicePhase.Rising:
                    statusText.text = $"{goal} Sala się zapełnia.";
                    break;
                case ServicePhase.Peak:
                    // The rush's own words when it has them: the calm first night's few tables at once are no "dishes
                    // flying one after another" (the card said so, under the HUD's "Kilka stolików naraz.").
                    string callout = shiftController.PhaseCallout;
                    statusText.text = string.IsNullOrEmpty(callout)
                        ? $"{goal} Szczyt - naczynia lecą jedno za drugim."
                        : $"{goal} {callout}";
                    break;
                case ServicePhase.Easing:
                    statusText.text = $"{goal} Ruch słabnie.";
                    break;
                default:
                    statusText.text = $"{goal} Spokojnie, na razie.";
                    break;
            }

            background.color = shiftController.Phase == ServicePhase.Peak ? RushTint : ShiftTint;
        }

        private string Progress(SpecialShiftRules rules)
        {
            Place place = shiftController.Place;
            string bonus = PlaceMoney.Format(rules.BonusEuros * PlaceMoney.SmallSums(place), place);
            switch (shiftController.Special)
            {
                case SpecialShift.Wedding:
                    return $"Napiwki ×1,5. Koperta od młodych na koniec: +{bonus}.";
                case SpecialShift.Inspection:
                    if (shiftController.InspectionFailed)
                    {
                        return shiftController.PileCollapsed
                            ? "Góra się zawaliła na oczach inspektora. Kontrola niezaliczona."
                            : "Stłuczka na oczach inspektora. Kontrola niezaliczona.";
                    }

                    return $"Na razie czysto. Stłuczka kosztuje podwójnie. Za czystą kontrolę: +{bonus}.";
                case SpecialShift.LasagneTray:
                    if (shiftController.TrayScrubbed)
                    {
                        return $"Taca domyta! Sala robi zrzutkę: +{bonus}.";
                    }

                    if (!shiftController.TrayArrived)
                    {
                        return "Taca wjedzie w trakcie zmiany.";
                    }

                    return shiftController.LegendaryTray == null
                        ? "Taca poszła do dużej zmywarki. Zrzutki nie będzie."
                        : "Taca w Górze. Domyj ją ręcznie: skrobak, druciak, moczenie.";
                case SpecialShift.StPatricks:
                    return "Podwójne napiwki. Szklanki, szklanki, szklanki.";
                default:
                    return string.Empty;
            }
        }

        private static Color Tint(SpecialShift special)
        {
            switch (special)
            {
                case SpecialShift.Wedding:
                    return new Color(0.42f, 0.25f, 0.45f, 0.95f);
                case SpecialShift.Inspection:
                    return new Color(0.14f, 0.27f, 0.42f, 0.95f);
                case SpecialShift.LasagneTray:
                    return new Color(0.5f, 0.3f, 0.1f, 0.95f);
                case SpecialShift.StPatricks:
                    return new Color(0.08f, 0.4f, 0.2f, 0.95f);
                default:
                    return new Color(0.1f, 0.1f, 0.1f, 0.9f);
            }
        }
    }
}
