namespace Zmywak.Core.Audio
{
    /// <summary>
    /// The game's sounds outside the sink (the kitchen's own are Kitchen.Feedback's): the interface, the dialogue, the
    /// end of a shift and its result, the music of the menu and the sounds of Kuba's room. Recorded CC0 takes where
    /// there are some (<see cref="GameSoundBank"/>), synthesised otherwise (<see cref="GameSynth"/>).
    /// </summary>
    public enum GameSound
    {
        // The interface.
        UiClick,
        UiConfirm,
        UiBack,
        UiError,
        UiToggle,
        UiTick,
        UiOpen,

        // The dialogue.
        DialogueBlip,
        PageTurn,

        // Money, the result, the kitchen's big moments.
        Reward,
        MoneyTick,
        GradeGood,
        GradePoor,
        Promotion,
        ShiftEnd,
        FlowChime,
        FullRack,
        WrongTool,

        // Loops: the menu's music, the room's ambience.
        MenuMusic,
        AmbRain,
        AmbCity,
        AmbRoom,

        // The building around the room, now and then.
        Creak,
        Door,
        Footsteps,
        Thunder
    }

    public static class GameSounds
    {
        /// <summary>Played as loops (built to wrap without a click).</summary>
        public static bool IsLoop(GameSound sound) =>
            sound == GameSound.MenuMusic || sound == GameSound.AmbRain || sound == GameSound.AmbCity || sound == GameSound.AmbRoom;

        /// <summary>Music (the music level) rather than an effect (the effects level).</summary>
        public static bool IsMusic(GameSound sound) => sound == GameSound.MenuMusic;
    }
}
