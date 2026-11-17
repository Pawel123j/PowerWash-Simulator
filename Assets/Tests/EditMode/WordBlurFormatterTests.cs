// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using NUnit.Framework;
using Zmywak.Narrative.LanguageBarrier;

namespace Zmywak.Tests
{
    public class WordBlurFormatterTests
    {
        private static string Blurred(string word)
        {
            return "<font=\"LiberationSans SDF - Blur\">" + WordBlurFormatter.Silhouette(word) + "</font>";
        }

        [Test]
        public void KnownWord_IsLeftAlone()
        {
            VocabularyTracker vocab = new VocabularyTracker(new string[1] { "kitchen" });
            Assert.AreEqual("kitchen", WordBlurFormatter.Format("kitchen", vocab));
        }

        [Test]
        public void UnknownWord_StaysWhereItIs_BlurredOutOfReading()
        {
            VocabularyTracker vocab = new VocabularyTracker();
            Assert.AreEqual(Blurred("grand") + "!", WordBlurFormatter.Format("grand!", vocab));
            StringAssert.DoesNotContain("grand", WordBlurFormatter.Format("grand!", vocab), "The word itself must not be on screen");
        }

        [Test]
        public void TheSilhouette_KeepsTheLengthAndTheOutline_NotTheLetters()
        {
            string shape = WordBlurFormatter.Silhouette("Polish");
            Assert.AreEqual(6, shape.Length);
            Assert.IsTrue(char.IsUpper(shape[0]), "A capital stays a capital");
            Assert.AreEqual('l', shape[2], "An ascender stays tall");
            Assert.AreEqual('h', shape[5]);
            Assert.AreEqual('q', WordBlurFormatter.Silhouette("lady")[3], "A descender still hangs");
            Assert.AreNotEqual("Polish", shape);
            Assert.AreEqual(shape, WordBlurFormatter.Silhouette("Polish"), "The same word always looks the same");
        }

        [Test]
        public void Punctuation_StaysSharp()
        {
            VocabularyTracker vocab = new VocabularyTracker();
            Assert.AreEqual("\"" + Blurred("fella") + ",\"", WordBlurFormatter.Format("\"fella,\"", vocab));
        }

        [Test]
        public void MixedSentence_OnlyBlursUnknownWords()
        {
            VocabularyTracker vocab = new VocabularyTracker(new string[2] { "the", "i" });
            string result = WordBlurFormatter.Format("the new kitchen", vocab);
            Assert.AreEqual("the " + Blurred("new") + " " + Blurred("kitchen"), result);
        }

        [Test]
        public void EmptyOrNullText_ReturnsAsIs()
        {
            VocabularyTracker vocab = new VocabularyTracker();
            Assert.AreEqual(string.Empty, WordBlurFormatter.Format(string.Empty, vocab));
            Assert.IsNull(WordBlurFormatter.Format(null, vocab));
        }
    }
}
