namespace TargetToss.Core
{
    /// <summary>The menu choices, kept on the device between sessions.</summary>
    public sealed class GameSettings
    {
        const string Prefix = "targettoss.settings.";
        public const int MaxVolume = 10;

        public GameMode Mode = GameMode.Timed;
        public Difficulty Difficulty = Difficulty.Normal;
        /// <summary>Master volume in tenths, 0..10.</summary>
        public int Volume = 8;
        public bool Muted;
        public bool ScreenShake = true;
        /// <summary>Gamepad cursor speed in steps 1..5.</summary>
        public int CursorSpeed = 3;

        public void CycleMode(int step) => Mode = (GameMode)Wrap((int)Mode + step, 2);

        public void CycleDifficulty(int step) => Difficulty = (Difficulty)Wrap((int)Difficulty + step, 3);

        public void StepVolume(int step) => Volume = Clamp(Volume + step, 0, MaxVolume);

        public void StepCursorSpeed(int step) => CursorSpeed = Clamp(CursorSpeed + step, 1, 5);

        static int Wrap(int value, int count) => ((value % count) + count) % count;

        static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;

        /// <summary>Reads the settings, falling back to the defaults for missing or broken values.</summary>
        public static GameSettings Load(IRecordStore store)
        {
            var defaults = new GameSettings();
            int mode = store.GetInt(Prefix + "mode", (int)defaults.Mode);
            int difficulty = store.GetInt(Prefix + "difficulty", (int)defaults.Difficulty);
            return new GameSettings
            {
                Mode = mode >= 0 && mode < 2 ? (GameMode)mode : defaults.Mode,
                Difficulty = difficulty >= 0 && difficulty < 3 ? (Difficulty)difficulty : defaults.Difficulty,
                Volume = Clamp(store.GetInt(Prefix + "volume", defaults.Volume), 0, MaxVolume),
                Muted = store.GetInt(Prefix + "muted", 0) == 1,
                ScreenShake = store.GetInt(Prefix + "shake", 1) == 1,
                CursorSpeed = Clamp(store.GetInt(Prefix + "cursor", defaults.CursorSpeed), 1, 5),
            };
        }

        public void Save(IRecordStore store)
        {
            store.SetInt(Prefix + "mode", (int)Mode);
            store.SetInt(Prefix + "difficulty", (int)Difficulty);
            store.SetInt(Prefix + "volume", Volume);
            store.SetInt(Prefix + "muted", Muted ? 1 : 0);
            store.SetInt(Prefix + "shake", ScreenShake ? 1 : 0);
            store.SetInt(Prefix + "cursor", CursorSpeed);
            store.Save();
        }
    }
}
