using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Zmywak.Kitchen.Radio;

namespace Zmywak.EditorTools
{
    /// <summary>
    /// Writes each kitchen radio station (<see cref="RadioMusic"/>) to a WAV file, to listen to outside the game:
    /// Zmywak > Dev > Export radio WAVs, or -executeMethod Zmywak.EditorTools.RadioPreview.ExportWavs [-radioOut folder].
    /// </summary>
    internal static class RadioPreview
    {
        [MenuItem("Zmywak/Dev/Export radio WAVs")]
        public static void ExportWavs()
        {
            string[] args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-radioOut");
            string folder = at >= 0 && at + 1 < args.Length ? args[at + 1] : Path.Combine(Application.dataPath, "..", "Logs", "Radio");
            Directory.CreateDirectory(folder);
            foreach (RadioStation station in new[] { RadioStation.Trad, RadioStation.Pop, RadioStation.News })
            {
                string path = Path.Combine(folder, $"radio_{station.ToString().ToLowerInvariant()}.wav");
                WriteWav(path, RadioMusic.Generate(station), RadioMusic.SampleRate);
                Debug.Log($"Zmywak: radio station {station} written to {path}");
            }
        }

        private static void WriteWav(string path, float[] samples, int sampleRate)
        {
            using var stream = new FileStream(path, FileMode.Create);
            using var writer = new BinaryWriter(stream);
            int bytes = samples.Length * 2;
            writer.Write("RIFF".ToCharArray());
            writer.Write(36 + bytes);
            writer.Write("WAVEfmt ".ToCharArray());
            writer.Write(16);
            writer.Write((short)1);          // PCM
            writer.Write((short)1);          // mono
            writer.Write(sampleRate);
            writer.Write(sampleRate * 2);    // bytes a second
            writer.Write((short)2);          // block align
            writer.Write((short)16);         // bits a sample
            writer.Write("data".ToCharArray());
            writer.Write(bytes);
            foreach (float sample in samples)
            {
                writer.Write((short)Mathf.Clamp(Mathf.RoundToInt(sample * 32767f), short.MinValue, short.MaxValue));
            }
        }
    }
}
