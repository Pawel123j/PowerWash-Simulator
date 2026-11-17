namespace Zmywak.Narrative.Dialogue
{
    /// <summary>How each speaker id is labelled in the dialogue box. The narrator has no label.</summary>
    public static class SpeakerNames
    {
        public const string Narrator = "narrator";

        public static string DisplayName(string speakerId)
        {
            switch (speakerId)
            {
                case Narrator: return string.Empty;
                case "kuba_thought": return "Kuba (myśli)";
                case "kuba": return "Kuba"; // out loud - rare, and in English
                case "declan": return "Declan";
                case "marek": return "Marek";
                case "agnieszka": return "Agnieszka";
                case "darius": return "Darius";
                case "agent": return "Gerry, Emerald Recruitment";
                case "byrne": return "Pani Byrne";
                case "clerk": return "Urzędniczka";
                case "banker": return "Pan z banku";
                case "mum": return "Mama";
                case "siobhan": return "Siobhán";
                case "tomek": return "Tomek";
                case "gavin": return "Gavin";
                case "tunde": return "Tunde";
                case "bartek": return "Bartek";
                case "dad": return "Tata";
                case "thiago": return "Thiago";
                case "oleh": return "Ołeh";
                case "liam": return "Liam";
                default: return speakerId;
            }
        }

        /// <summary>Whose portrait shows for this speaker (Art/Portraits/Portrait_&lt;id&gt;.png): Kuba's own for his
        /// thoughts, none for the narrator.</summary>
        public static string PortraitId(string speakerId)
        {
            switch (speakerId)
            {
                case null:
                case "":
                case Narrator: return null;
                case "kuba_thought": return "kuba";
                default: return speakerId;
            }
        }
    }
}
