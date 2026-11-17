// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.PlayMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using System;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Kitchen.Dishes;
using Zmywak.Kitchen.Feedback;

namespace Zmywak.Tests.PlayMode
{
    internal sealed class FeedbackRecorder : IDisposable
    {
        public int Claimed;

        public int Scraped;

        public int Rinsing;

        public int Scrubbing;

        public int Cleaned;

        public int Racked;

        public int Broken;

        public FeedbackRecorder()
        {
            KitchenFeedbackEvents.Claimed += OnClaimed;
            KitchenFeedbackEvents.Scraped += OnScraped;
            KitchenFeedbackEvents.Rinsing += OnRinsing;
            KitchenFeedbackEvents.Scrubbing += OnScrubbing;
            KitchenFeedbackEvents.Cleaned += OnCleaned;
            KitchenFeedbackEvents.Racked += OnRacked;
            KitchenFeedbackEvents.Broken += OnBroken;
        }

        public void Dispose()
        {
            KitchenFeedbackEvents.Claimed -= OnClaimed;
            KitchenFeedbackEvents.Scraped -= OnScraped;
            KitchenFeedbackEvents.Rinsing -= OnRinsing;
            KitchenFeedbackEvents.Scrubbing -= OnScrubbing;
            KitchenFeedbackEvents.Cleaned -= OnCleaned;
            KitchenFeedbackEvents.Racked -= OnRacked;
            KitchenFeedbackEvents.Broken -= OnBroken;
        }

        public override string ToString()
        {
            return $"claimed {Claimed}, scraped {Scraped}, rinsing {Rinsing}, scrubbing {Scrubbing}, " + $"cleaned {Cleaned}, racked {Racked}, broken {Broken}";
        }

        private void OnClaimed(DishController dish)
        {
            Claimed++;
        }

        private void OnScraped(DishController dish)
        {
            Scraped++;
        }

        private void OnRinsing(DishController dish)
        {
            Rinsing++;
        }

        private void OnScrubbing(DishController dish, Vector3 point, ToolDefinition tool, float intensity)
        {
            Scrubbing++;
        }

        private void OnCleaned(DishController dish)
        {
            Cleaned++;
        }

        private void OnRacked(DishController dish, Vector3 position)
        {
            Racked++;
        }

        private void OnBroken(Vector3 position)
        {
            Broken++;
        }
    }
}
