using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zmywak.Core.Audio;
using Zmywak.Core.Data;
using Zmywak.Core.Haptics;
using Zmywak.UI.Common;

namespace Zmywak.UI.HUD
{
    /// <summary>
    /// The end of a shift, as a screen of its own. It comes up in steps, so each part is seen: the night's grade lands
    /// with its score, then what the night was - washed against the quota, the best Flow, the breakages, the chef's urgent
    /// calls, the special shift's task - row by row; then the pay, every part of it, counting up to what Kuba takes home;
    /// then his rank at the sink, the bar filling with this night's work - and a promotion, when it comes, and a personal
    /// best, when one falls. A tap skips to the end; the button takes him home.
    /// </summary>
    public class ShiftResultScreen : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform card;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text gradeText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private Image scoreFill;
        [SerializeField] private TMP_Text verdictText;
        [SerializeField] private GameObject recordBadge;
        [SerializeField] private TMP_Text recordText;
        [Tooltip("The night's rows: label left, value right, in order.")]
        [SerializeField] private TMP_Text[] statLabels = new TMP_Text[0];
        [SerializeField] private TMP_Text[] statValues = new TMP_Text[0];
        [Tooltip("The pay's rows, the same way.")]
        [SerializeField] private TMP_Text[] payLabels = new TMP_Text[0];
        [SerializeField] private TMP_Text[] payValues = new TMP_Text[0];
        [SerializeField] private TMP_Text totalText;
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private Image rankFill;
        [SerializeField] private TMP_Text rankNextText;
        [SerializeField] private GameObject promotionBanner;
        [SerializeField] private TMP_Text promotionText;
        [SerializeField] private Button homeButton;

        private static readonly Color[] GradeColors =
        {
            new Color(0.7f, 0.7f, 0.7f), new Color(1f, 0.48f, 0.42f), new Color(1f, 0.7f, 0.42f), new Color(0.66f, 0.85f, 1f),
            new Color(0.48f, 0.88f, 0.54f), new Color(1f, 0.83f, 0.3f)
        };

        private readonly List<(string label, string value)> _stats = new List<(string, string)>();
        private readonly List<(string label, float amount, bool minus)> _pay = new List<(string, float, bool)>();
        private ShiftResult _result;
        private CareerRecord _before;
        private CareerRecord _after;
        private int _rankBefore;
        private int _rankAfter;
        private bool _done;
        private Coroutine _play;

        public bool IsShown => root != null && root.activeSelf;

        /// <summary>The whole sequence has played (or was skipped): every number shows its final value.</summary>
        public bool IsComplete => _done;

        /// <summary>The home button was pressed.</summary>
        public event System.Action HomePressed;

        // The screen sits on its own root, built switched off; Show switches it on.
        private void OnEnable()
        {
            homeButton?.onClick.AddListener(OnHome);
        }

        private void OnDisable()
        {
            homeButton?.onClick.RemoveListener(OnHome);
        }

        /// <summary>
        /// Plays the result of a shift. <paramref name="careerBefore"/> is Kuba's record as the shift began (null without the
        /// life around the kitchen): the rank and the bests are measured against it.
        /// </summary>
        public void Show(ShiftResult result, CareerRecord careerBefore)
        {
            _result = result;
            _before = careerBefore != null ? careerBefore.Clone() : new CareerRecord();
            _after = _before.Clone();
            _after.Record(result);
            _rankBefore = Career.RankFor(_before);
            _rankAfter = Career.RankFor(_after);
            BuildRows();
            _done = false;
            root?.SetActive(true);
            if (_play != null)
            {
                StopCoroutine(_play);
            }

            _play = StartCoroutine(Play());
        }

        /// <summary>Jumps to the end: every value final, the button up (a tap on the screen does this).</summary>
        public void Complete()
        {
            if (!_done)
            {
                if (_play != null)
                {
                    StopCoroutine(_play);
                }

                ShowFinal();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_done)
            {
                Complete();
            }
        }

        private void Update()
        {
            if (!IsShown || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return;
            }

            // The back gesture: the first skips to the end, the next goes home.
            if (_done)
            {
                OnHome();
            }
            else
            {
                Complete();
            }
        }

        private void OnHome()
        {
            HomePressed?.Invoke();
        }

        /// <summary>Everything the screen says, in one string - for tests and the log.</summary>
        public string Describe()
        {
            var text = new StringBuilder();
            void Add(TMP_Text t)
            {
                if (t != null && t.gameObject.activeInHierarchy && !string.IsNullOrEmpty(t.text))
                {
                    text.AppendLine(t.text);
                }
            }

            Add(titleText);
            Add(gradeText);
            Add(scoreText);
            Add(verdictText);
            Add(recordText);
            for (int i = 0; i < statLabels.Length; i++)
            {
                if (i < statValues.Length && statLabels[i] != null && statLabels[i].gameObject.activeInHierarchy)
                {
                    text.AppendLine($"{statLabels[i].text} {statValues[i].text}");
                }
            }

            for (int i = 0; i < payLabels.Length; i++)
            {
                if (i < payValues.Length && payLabels[i] != null && payLabels[i].gameObject.activeInHierarchy)
                {
                    text.AppendLine($"{payLabels[i].text} {payValues[i].text}");
                }
            }

            Add(totalText);
            Add(rankText);
            Add(rankNextText);
            Add(promotionText);
            return text.ToString();
        }

        // ------------------------------------------------------------------ what it says

        /// <summary>
        /// The quota row. Clocked out early, the pay asks the quota of the share worked (ShiftResultCalculator), so that is what is met or
        /// missed - the row used to say "brakło 7" of the whole night's, right above a pay line "Premia za normę (część zmiany)".
        /// </summary>
        private static string QuotaRow(ShiftResult r)
        {
            if (r.Quota <= 0)
            {
                return "-";
            }

            bool cutShort = r.PlannedSeconds > 0f && r.ElapsedSeconds < r.PlannedSeconds - 0.5f;
            if (!cutShort)
            {
                return r.DishesCleaned >= r.Quota ? "zrobiona" : $"brakło {r.Quota - r.DishesCleaned}";
            }

            int asked = ShiftScoring.EffectiveQuota(r.Quota, Mathf.Clamp01(r.ElapsedSeconds / r.PlannedSeconds));
            return r.DishesCleaned >= asked ? "zrobiona za część zmiany" : $"brakło {asked - r.DishesCleaned} za część zmiany";
        }

        private void BuildRows()
        {
            ShiftResult r = _result;
            _stats.Clear();
            _stats.Add(("Umyte naczynia", r.Quota > 0 ? $"{r.DishesCleaned} / {r.Quota}" : r.DishesCleaned.ToString()));
            _stats.Add(("Norma", QuotaRow(r)));
            _stats.Add(("Najlepszy Flow", $"×{r.BestFlow}"));
            _stats.Add(("Stłuczki", r.DishesBroken == 0 ? "żadnej" : r.DishesBroken.ToString()));
            _stats.Add(("Pilne zamówienia", r.UrgentOrders > 0 ? $"{r.UrgentOrdersMet} / {r.UrgentOrders}" : "-"));
            _stats.Add(("Staranność", $"{Mathf.RoundToInt(Mathf.Clamp01(r.WashAccuracy) * 100f)}%"));
            string special = SpecialTask(r);
            if (special != null)
            {
                _stats.Add(("Zadanie specjalne", special));
            }

            if (r.CrewHelped > 0)
            {
                _stats.Add(("Pomoc ekipy", $"+{PolishPlural.Dishes(r.CrewHelped)}"));
            }

            _pay.Clear();
            _pay.Add(("Stawka", r.BaseWage, false));
            if (r.QuotaBonus > 0f)
            {
                // Clocked out early: the bonus is for the share of the quota the part worked asked for, not the night's whole one
                // the stats above show missing.
                bool cutShort = r.PlannedSeconds > 0f && r.ElapsedSeconds < r.PlannedSeconds - 0.5f;
                _pay.Add((cutShort ? "Premia za normę (część zmiany)" : "Premia za normę", r.QuotaBonus, false));
            }

            if (r.GradeBonus > 0f)
            {
                _pay.Add(($"Premia za ocenę {ShiftScoring.Letter(r.Grade)}", r.GradeBonus, false));
            }

            _pay.Add(("Napiwki", r.Tips, false));
            if (r.PrepBonus > 0f)
            {
                _pay.Add(("Deska", r.PrepBonus, false));
            }

            if (r.SpecialBonus > 0f)
            {
                _pay.Add((SpecialBonusName(r.Special), r.SpecialBonus, false));
            }

            if (r.IdleDocked > 0f)
            {
                _pay.Add(("Potrącenie: prawie nic nie umyte", r.IdleDocked, true));
            }

            if (r.Deductions > 0f)
            {
                _pay.Add(("Potrącenia za stłuczki", r.Deductions, true));
            }
        }

        private static string SpecialTask(ShiftResult r)
        {
            switch (r.Special)
            {
                case SpecialShift.Wedding:
                    return "wesele obsłużone";
                case SpecialShift.Inspection:
                    return r.InspectionPassed ? "kontrola zaliczona" : "kontrola niezaliczona";
                case SpecialShift.LasagneTray:
                    return r.TrayScrubbed ? "Taca domyta!" : "Taca czeka";
                case SpecialShift.StPatricks:
                    return "Dzień św. Patryka";
                default:
                    return null;
            }
        }

        private static string SpecialBonusName(SpecialShift special)
        {
            switch (special)
            {
                case SpecialShift.Wedding:
                    return "Koperta od młodych";
                case SpecialShift.Inspection:
                    return "Od Declana za kontrolę";
                case SpecialShift.LasagneTray:
                    return "Zrzutka sali";
                default:
                    return "Premia specjalna";
            }
        }

        private string Money(float amount) => PlaceMoney.Format(amount, _result.Place);

        private string Record()
        {
            if (_before.shifts > 0 && _result.Score.Total > _before.bestScore)
            {
                return $"Nowy rekord: {_result.Score.Total} pkt!";
            }

            if (_before.shifts > 0 && _result.DishesCleaned > _before.mostDishes)
            {
                return $"Nowy rekord: {PolishPlural.Dishes(_result.DishesCleaned)} na zmianie!";
            }

            if (_before.shifts > 0 && _result.BestFlow > _before.bestFlow)
            {
                return $"Najdłuższy Flow: ×{_result.BestFlow}!";
            }

            return null;
        }

        // ------------------------------------------------------------------ how it comes in

        private void PrepareRows()
        {
            for (int i = 0; i < statLabels.Length; i++)
            {
                bool used = i < _stats.Count;
                SetRow(statLabels[i], i < statValues.Length ? statValues[i] : null, used, used ? _stats[i].label : null, used ? _stats[i].value : null);
            }

            for (int i = 0; i < payLabels.Length; i++)
            {
                bool used = i < _pay.Count;
                SetRow(payLabels[i], i < payValues.Length ? payValues[i] : null, used, used ? _pay[i].label : null,
                    used ? PayValue(_pay[i], _pay[i].amount) : null);
            }
        }

        private string PayValue((string label, float amount, bool minus) row, float shown) => (row.minus ? "- " : "+ ") + Money(shown);

        private static void SetRow(TMP_Text label, TMP_Text value, bool used, string labelText, string valueText)
        {
            if (label != null)
            {
                label.gameObject.SetActive(used);
                label.text = labelText ?? string.Empty;
                label.alpha = 0f;
            }

            if (value != null)
            {
                value.gameObject.SetActive(used);
                value.text = valueText ?? string.Empty;
                value.alpha = 0f;
            }
        }

        private IEnumerator Play()
        {
            ShiftResult r = _result;
            titleText.text = string.IsNullOrEmpty(r.ShiftTitle) ? "Koniec zmiany" : $"Koniec zmiany · {r.ShiftTitle}";
            gradeText.text = ShiftScoring.Letter(r.Grade);
            gradeText.color = GradeColors[Mathf.Clamp((int)r.Grade, 0, GradeColors.Length - 1)];
            gradeText.alpha = 0f;
            scoreText.text = string.Empty;
            verdictText.text = ShiftScoring.Verdict(r.Grade);
            verdictText.alpha = 0f;
            string record = Record();
            recordBadge?.SetActive(false);
            if (recordText != null)
            {
                recordText.text = record ?? string.Empty;
            }

            PrepareRows();
            totalText.text = string.Empty;
            promotionBanner?.SetActive(false);
            SetFill(scoreFill, 0f);
            SetRank(_rankBefore, Career.ProgressToNext(_before, _rankBefore));

            // The screen and the card come in.
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.3f)
            {
                float e = 1f - (1f - t) * (1f - t);
                group.alpha = t;
                card.anchoredPosition = new Vector2(0f, -80f * (1f - e));
                yield return null;
            }

            group.alpha = 1f;
            card.anchoredPosition = Vector2.zero;

            // The grade lands, and the score fills to it.
            bool good = r.Grade >= ShiftGrade.B;
            GameAudio.Play(good ? GameSound.GradeGood : GameSound.GradePoor, 0.75f);
            if (r.Grade == ShiftGrade.S)
            {
                HapticsService.Pulse(HapticPulse.Medium);
            }

            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.45f)
            {
                gradeText.alpha = Mathf.Clamp01(t * 3f);
                gradeText.transform.localScale = Vector3.one * Pop(t);
                SetFill(scoreFill, t * r.Score.Total / 100f);
                scoreText.text = $"{Mathf.RoundToInt(t * r.Score.Total)}/100";
                yield return null;
            }

            gradeText.transform.localScale = Vector3.one;
            gradeText.alpha = 1f;
            SetFill(scoreFill, r.Score.Total / 100f);
            scoreText.text = $"{r.Score.Total}/100";
            yield return Fade(verdictText, 0.25f);

            // The night, row by row.
            for (int i = 0; i < _stats.Count && i < statLabels.Length; i++)
            {
                GameAudio.Play(GameSound.UiTick, 0.35f, 0.9f + i * 0.04f);
                yield return FadeRow(statLabels[i], i < statValues.Length ? statValues[i] : null, 0.14f);
            }

            if (!string.IsNullOrEmpty(record))
            {
                recordBadge?.SetActive(true);
                GameAudio.Play(GameSound.FlowChime, 0.6f);
                yield return PopIn(recordBadge != null ? recordBadge.transform : null, 0.3f);
            }

            // The pay: each part counts up, then the total.
            float total = 0f;
            for (int i = 0; i < _pay.Count && i < payLabels.Length; i++)
            {
                TMP_Text value = i < payValues.Length ? payValues[i] : null;
                yield return FadeRow(payLabels[i], value, 0.1f);
                (string label, float amount, bool minus) row = _pay[i];
                float step = 0f;
                for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.35f)
                {
                    float shown = Mathf.Lerp(0f, row.amount, t);
                    if (value != null)
                    {
                        value.text = PayValue(row, shown);
                    }

                    step += Time.unscaledDeltaTime;
                    if (step > 0.06f)
                    {
                        step = 0f;
                        GameAudio.Play(GameSound.MoneyTick, 0.35f, row.minus ? 0.8f : 1f + t * 0.2f);
                    }

                    totalText.text = Money(total + (row.minus ? -shown : shown));
                    yield return null;
                }

                total += row.minus ? -row.amount : row.amount;
                if (value != null)
                {
                    value.text = PayValue(row, row.amount);
                }
            }

            totalText.text = Money(r.Wage);
            GameAudio.Play(GameSound.Reward, 0.7f);
            yield return PopIn(totalText.transform, 0.25f);

            // The rank: the bar fills with tonight's work; a promotion bursts in.
            float from = Career.ProgressToNext(_before, _rankBefore);
            float to = _rankAfter > _rankBefore ? 1f : Career.ProgressToNext(_after, _rankAfter);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.6f)
            {
                SetRank(_rankBefore, Mathf.Lerp(from, to, 1f - (1f - t) * (1f - t)));
                yield return null;
            }

            if (_rankAfter > _rankBefore)
            {
                SetRank(_rankAfter, Career.ProgressToNext(_after, _rankAfter));
                promotionBanner?.SetActive(true);
                if (promotionText != null)
                {
                    promotionText.text = _rankAfter >= Career.KingRank ? "KRÓL ZMYWAKA!" : $"Awans: {Career.Title(_rankAfter)}!";
                }

                GameAudio.Play(GameSound.Promotion, 0.8f);
                HapticsService.Pulse(HapticPulse.Medium);
                yield return PopIn(promotionBanner != null ? promotionBanner.transform : null, 0.35f);
            }
            else
            {
                SetRank(_rankAfter, Career.ProgressToNext(_after, _rankAfter));
            }

            ShowFinal();
        }

        private void ShowFinal()
        {
            ShiftResult r = _result;
            group.alpha = 1f;
            card.anchoredPosition = Vector2.zero;
            titleText.text = string.IsNullOrEmpty(r.ShiftTitle) ? "Koniec zmiany" : $"Koniec zmiany · {r.ShiftTitle}";
            gradeText.text = ShiftScoring.Letter(r.Grade);
            gradeText.alpha = 1f;
            gradeText.transform.localScale = Vector3.one;
            scoreText.text = $"{r.Score.Total}/100";
            SetFill(scoreFill, r.Score.Total / 100f);
            verdictText.alpha = 1f;
            string record = Record();
            recordBadge?.SetActive(!string.IsNullOrEmpty(record));
            for (int i = 0; i < statLabels.Length; i++)
            {
                bool used = i < _stats.Count;
                SetRow(statLabels[i], i < statValues.Length ? statValues[i] : null, used, used ? _stats[i].label : null, used ? _stats[i].value : null);
                Show(statLabels[i]);
                Show(i < statValues.Length ? statValues[i] : null);
            }

            for (int i = 0; i < payLabels.Length; i++)
            {
                bool used = i < _pay.Count;
                SetRow(payLabels[i], i < payValues.Length ? payValues[i] : null, used, used ? _pay[i].label : null,
                    used ? PayValue(_pay[i], _pay[i].amount) : null);
                Show(payLabels[i]);
                Show(i < payValues.Length ? payValues[i] : null);
            }

            totalText.text = Money(r.Wage);
            totalText.transform.localScale = Vector3.one;
            SetRank(_rankAfter, Career.ProgressToNext(_after, _rankAfter));
            bool promoted = _rankAfter > _rankBefore;
            promotionBanner?.SetActive(promoted);
            if (promoted && promotionText != null)
            {
                promotionText.text = _rankAfter >= Career.KingRank ? "KRÓL ZMYWAKA!" : $"Awans: {Career.Title(_rankAfter)}!";
            }

            if (promotionBanner != null)
            {
                promotionBanner.transform.localScale = Vector3.one;
            }

            if (recordBadge != null)
            {
                recordBadge.transform.localScale = Vector3.one;
            }

            _done = true;
        }

        private static void Show(TMP_Text text)
        {
            if (text != null)
            {
                text.alpha = 1f;
            }
        }

        private void SetRank(int rank, float progress)
        {
            if (rankText != null)
            {
                rankText.text = Career.Title(rank);
            }

            SetFill(rankFill, rank >= Career.KingRank ? 1f : progress);
            if (rankNextText != null)
            {
                rankNextText.text = rank >= Career.KingRank ? "Najwyższa ranga" : $"do: {Career.Title(rank + 1)} · {Mathf.RoundToInt(progress * 100f)}%";
            }
        }

        private static void SetFill(Image fill, float value)
        {
            if (fill != null)
            {
                FillBar.Set(fill, Mathf.Clamp01(value));
            }
        }

        private static float Pop(float t)
        {
            // From big to its size with a small overshoot: it lands.
            if (t < 0.6f)
            {
                return Mathf.Lerp(2.2f, 0.92f, t / 0.6f);
            }

            return Mathf.Lerp(0.92f, 1f, (t - 0.6f) / 0.4f);
        }

        private static IEnumerator Fade(TMP_Text text, float seconds)
        {
            if (text == null)
            {
                yield break;
            }

            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / seconds)
            {
                text.alpha = t;
                yield return null;
            }

            text.alpha = 1f;
        }

        private static IEnumerator FadeRow(TMP_Text label, TMP_Text value, float seconds)
        {
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / seconds)
            {
                if (label != null)
                {
                    label.alpha = t;
                }

                if (value != null)
                {
                    value.alpha = t;
                }

                yield return null;
            }

            Show(label);
            Show(value);
        }

        private static IEnumerator PopIn(Transform target, float seconds)
        {
            if (target == null)
            {
                yield break;
            }

            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / seconds)
            {
                target.localScale = Vector3.one * (t < 0.5f ? Mathf.Lerp(0.6f, 1.12f, t / 0.5f) : Mathf.Lerp(1.12f, 1f, (t - 0.5f) / 0.5f));
                yield return null;
            }

            target.localScale = Vector3.one;
        }
    }
}
