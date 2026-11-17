using System;
using System.Collections.Generic;
using UnityEngine;
using Zmywak.Core.Save;
using Zmywak.Core.Services;
using Zmywak.Narrative.Dialogue;
using Zmywak.Narrative.LanguageBarrier;
using Zmywak.Narrative.SlangDictionary;

namespace Zmywak.Narrative.Progress
{
    /// <summary>
    /// Persistent (DontDestroyOnLoad) home for everything narrative that survives between scenes and
    /// play sessions: known vocabulary, unlocked slang, story flags (the choices Kuba made) and which story
    /// scenes have played. Shares the same save file as <see cref="Zmywak.Meta.Progress.MetaProgressController"/>
    /// via a read-modify-write on every save, so neither controller clobbers the other's fields.
    /// </summary>
    public class NarrativeProgressController : MonoBehaviour, IStoryState
    {
        /// <summary>The Chapter 1 opening; saves from before story scenes only recorded "intro seen".</summary>
        public const string ArrivalBeatId = "ch1_arrival";

        [SerializeField]
        private List<string> startingVocabulary = new List<string>
        {
            "you", "i", "the", "a", "yes", "no", "ok", "chef", "hello", "thank", "please"
        };

        private readonly HashSet<string> _flags = new HashSet<string>();
        private readonly HashSet<string> _seenBeats = new HashSet<string>();
        private int _lifeGeneration; // SaveSystem.LifeGeneration when this life was loaded (see there)
        private bool _lifeBegun; // false until there is a story to keep: a save loaded, or the first thing learnt or decided

        public VocabularyTracker Vocabulary { get; private set; }
        public SlangCollectionController Slang { get; private set; }
        public bool HasSeenIntro => _seenBeats.Contains(ArrivalBeatId);
        public IReadOnlyCollection<string> Flags => _flags;

        public event Action OnProgressChanged;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            ServiceLocator.Register(this);

            Vocabulary = new VocabularyTracker(startingVocabulary);
            Slang = new SlangCollectionController();

            _lifeGeneration = SaveSystem.LifeGeneration;
            LoadFromDisk();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<NarrativeProgressController>();
        }

        private void LoadFromDisk()
        {
            SaveData data = SaveSystem.Load();
            _lifeBegun = data != null;
            ReadSnapshot(data);
        }

        private void ReadSnapshot(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            if (data.knownVocabulary != null)
            {
                foreach (string word in data.knownVocabulary)
                {
                    Vocabulary.Learn(word);
                }
            }

            if (data.unlockedSlangIds != null)
            {
                foreach (string slangId in data.unlockedSlangIds)
                {
                    Slang.Unlock(slangId);
                }
            }

            if (data.storyFlags != null)
            {
                _flags.UnionWith(data.storyFlags);
            }

            if (data.seenStoryBeats != null)
            {
                _seenBeats.UnionWith(data.seenStoryBeats);
            }
        }

        // Every change is saved as it happens; these also flush when the phone backgrounds the app - on mobile
        // OnApplicationQuit often never comes, the app is simply killed while paused.
        private void OnApplicationPause(bool paused)
        {
            if (paused && _lifeBegun) // the app sent away must not make a save of a game nobody has started
            {
                Save();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _lifeBegun)
            {
                Save();
            }
        }

        private void Save()
        {
            _lifeBegun = true; // everything that changes the story saves here: from now on there is a story to keep
            if (_lifeGeneration != SaveSystem.LifeGeneration)
            {
                return; // its slot was deleted or another was picked since: this life must not write itself back
            }

            SaveData data = SaveSystem.Load() ?? new SaveData();
            WriteSnapshot(data);
            SaveSystem.Save(data);
        }

        /// <summary>Writes into a shared draft without IO or events. Used by the dialogue transaction.</summary>
        public void WriteSnapshot(SaveData data)
        {
            data.knownVocabulary = new List<string>(Vocabulary.KnownWords);
            data.unlockedSlangIds = new List<string>(Slang.UnlockedIds);
            data.storyFlags = new List<string>(_flags);
            data.seenStoryBeats = new List<string>(_seenBeats);
            data.hasSeenIntro = HasSeenIntro;
        }

        /// <summary>Publish only after all narrative and economic consequences are durable together.</summary>
        public void AcceptSnapshot(SaveData data, bool notify = true)
        {
            _lifeBegun = true;
            ReadSnapshot(data);
            if (notify) NotifySnapshotAccepted();
        }

        public void NotifySnapshotAccepted() => OnProgressChanged?.Invoke();

        public bool CanSaveCurrentLife => _lifeGeneration == SaveSystem.LifeGeneration;

        public void LearnWord(string word)
        {
            Vocabulary.Learn(word);
            OnProgressChanged?.Invoke();
            Save();
        }

        public void UnlockSlang(string slangId)
        {
            Slang.Unlock(slangId);
            OnProgressChanged?.Invoke();
            Save();
        }

        public bool HasFlag(string flag) => !string.IsNullOrEmpty(flag) && _flags.Contains(flag);

        public void SetFlag(string flag)
        {
            if (string.IsNullOrEmpty(flag) || !_flags.Add(flag))
            {
                return;
            }

            OnProgressChanged?.Invoke();
            Save();
        }

        public bool HasSeenBeat(string beatId) => _seenBeats.Contains(beatId);

        public void MarkBeatSeen(string beatId)
        {
            if (string.IsNullOrEmpty(beatId) || !_seenBeats.Add(beatId))
            {
                return;
            }

            OnProgressChanged?.Invoke();
            Save();
        }
    }
}
