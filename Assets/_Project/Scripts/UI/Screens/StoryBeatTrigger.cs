using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Core.Services;
using Zmywak.Meta.Progress;
using Zmywak.Narrative.Dialogue;
using Zmywak.Narrative.Progress;
using Zmywak.Narrative.Story;

namespace Zmywak.UI.Screens
{
    /// <summary>
    /// Coming home from a shift, plays the story's next due scene (<see cref="StoryBeatScheduler"/>) over the hub,
    /// hiding the hub's actions until it ends. The chapters are told in order: the first one not yet completed is
    /// the one being told, and the next begins the day after its last scene. What the story does to Kuba's life -
    /// money, stats - goes to the week; the scene is then marked as seen, and a chapter's last scene completes it.
    /// </summary>
    public class StoryBeatTrigger : MonoBehaviour
    {
        [Tooltip("In the order they are told.")]
        [SerializeField] private List<StoryChapter> chapters = new List<StoryChapter>();
        [SerializeField] private DialogueRunner dialogueRunner;
        [SerializeField] private GameObject hubActionsPanel;
        [SerializeField] private GameObject dialoguePanel;
        [Tooltip("Shows which chapter Kuba is in.")]
        [SerializeField] private TMP_Text chapterText;

        /// <summary>
        /// How long the hub's actions take to come back after a scene, taking no taps until they are there. The scene's
        /// "Dalej" sits over "Idź do pracy": a player tapping quickly through the last lines went straight to work and
        /// lost the evening (seen on the emulator).
        /// </summary>
        public const float ActionsReturnSeconds = 0.5f;

        private NarrativeProgressController _narrative;
        private MetaProgressController _meta;
        private StoryChapter _playingChapter;
        private CanvasGroup _actionsGroup;
        private float _actionsReturnedAt = -1f;

        /// <summary>The scene playing right now, or null.</summary>
        public StoryBeat PlayingBeat { get; private set; }

        /// <summary>Whether the hub's actions answer a touch (not while a scene plays, nor while they come back after it).</summary>
        public bool ActionsTakeTaps => PlayingBeat == null && (_actionsGroup == null || _actionsGroup.blocksRaycasts);

        /// <summary>The chapter being told: the first not yet completed (the last, once all are).</summary>
        public StoryChapter CurrentChapter
        {
            get
            {
                StoryChapter last = null;
                foreach (StoryChapter chapter in chapters)
                {
                    if (chapter == null)
                    {
                        continue;
                    }

                    last = chapter;
                    if (_narrative == null || !_narrative.HasFlag(chapter.CompletedFlag))
                    {
                        return chapter;
                    }
                }

                return last;
            }
        }

        private void Start()
        {
            ServiceLocator.TryGet(out _narrative);
            ServiceLocator.TryGet(out _meta);
            ShowChapterTitle();

            StoryChapter chapter = CurrentChapter;
            bool told = chapter != null && _narrative != null && _narrative.HasFlag(chapter.CompletedFlag);
            StoryBeat beat = _narrative != null && chapter != null && !told
                ? StoryBeatScheduler.NextBeat(chapter.Beats, _meta != null ? _meta.DayNumber : 1, _narrative.HasSeenBeat, _narrative.HasFlag)
                : null;
            if (beat == null || dialogueRunner == null)
            {
                dialoguePanel?.SetActive(false);
                return;
            }

            // A chapter that begins years later moves the calendar there before its first scene - and Kuba to wherever
            // the story has taken him by then.
            if (_meta != null && chapter.TryGetStartDate(out DateTime starts) && _meta.SkipTo(starts))
            {
                _meta.PostNotice($"Czas płynie. Jest {starts.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)}.");
                if (chapter.TryGetRelocation(_narrative.HasFlag, out Place place))
                {
                    _meta.MoveTo(place);
                }
            }

            PlayingBeat = beat;
            _playingChapter = chapter;
            dialogueRunner.OnDialogueEnded += HandleDialogueEnded;
            dialogueRunner.CanSpend = euros => _meta == null || _meta.CanSpareEuros(euros); // the story keeps the rent, too
            hubActionsPanel?.SetActive(false);
            dialoguePanel?.SetActive(true);
            dialogueRunner.StartDialogue(beat.graph, new StoryDialogueTransaction(beat.beatId,
                beat.endsChapter ? chapter.CompletedFlag : null, _meta, _narrative));
        }

        private void OnDestroy()
        {
            if (dialogueRunner != null)
            {
                dialogueRunner.OnDialogueEnded -= HandleDialogueEnded;
            }
        }

        private void HandleDialogueEnded()
        {
            dialogueRunner.OnDialogueEnded -= HandleDialogueEnded;

            StoryBeat beat = PlayingBeat;
            StoryChapter chapter = _playingChapter;
            PlayingBeat = null;
            _playingChapter = null;
            if (beat.endsChapter && chapter != null)
            {
                StoryChapter next = CurrentChapter;
                bool more = next != null && next != chapter;
                _meta?.PostNotice(more ? $"{chapter.Title} - koniec. Od jutra {next.Title}." : $"{chapter.Title} - koniec. Ciąg dalszy nastąpi.");
            }

            hubActionsPanel?.SetActive(true);
            dialoguePanel?.SetActive(false);
            ShowChapterTitle();
            BringActionsBack();
        }

        /// <summary>The actions fade back in, and take taps once they are there (<see cref="ActionsReturnSeconds"/>).</summary>
        private void BringActionsBack()
        {
            if (hubActionsPanel == null)
            {
                return;
            }

            if (_actionsGroup == null && !hubActionsPanel.TryGetComponent(out _actionsGroup))
            {
                _actionsGroup = hubActionsPanel.AddComponent<CanvasGroup>();
            }

            _actionsGroup.alpha = 0f;
            _actionsGroup.interactable = false;
            _actionsGroup.blocksRaycasts = false;
            _actionsReturnedAt = Time.unscaledTime;
        }

        private void Update()
        {
            if (_actionsReturnedAt < 0f || _actionsGroup == null)
            {
                return;
            }

            float shown = Mathf.Clamp01((Time.unscaledTime - _actionsReturnedAt) / ActionsReturnSeconds);
            _actionsGroup.alpha = shown;
            if (shown >= 1f)
            {
                _actionsGroup.interactable = true;
                _actionsGroup.blocksRaycasts = true;
                _actionsReturnedAt = -1f;
            }
        }

        private void ShowChapterTitle()
        {
            StoryChapter chapter = CurrentChapter;
            if (chapterText == null || chapter == null)
            {
                return;
            }

            bool completed = _narrative != null && _narrative.HasFlag(chapter.CompletedFlag);
            chapterText.text = completed ? $"{chapter.Title} (ukończony)" : chapter.Title;
        }
    }
}
