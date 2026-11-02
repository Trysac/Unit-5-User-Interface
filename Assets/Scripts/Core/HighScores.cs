using System.Collections.Generic;

namespace TargetToss.Core
{
    /// <summary>Where records and settings are kept (PlayerPrefs in the game, memory in tests).</summary>
    public interface IRecordStore
    {
        int GetInt(string key, int fallback);
        void SetInt(string key, int value);
        void Save();
    }

    public sealed class MemoryRecordStore : IRecordStore
    {
        readonly Dictionary<string, int> values = new Dictionary<string, int>();

        public int GetInt(string key, int fallback) => values.TryGetValue(key, out int value) ? value : fallback;
        public void SetInt(string key, int value) => values[key] = value;
        public void Save() => SaveCount++;

        /// <summary>How many times the records were flushed.</summary>
        public int SaveCount { get; private set; }
    }

    /// <summary>What a finished round changed in the records.</summary>
    public readonly struct RecordUpdate
    {
        public readonly bool NewBest;
        public readonly bool NewBestCombo;
        public readonly int PreviousBest;

        public RecordUpdate(bool newBest, bool newBestCombo, int previousBest)
        {
            NewBest = newBest;
            NewBestCombo = newBestCombo;
            PreviousBest = previousBest;
        }
    }

    /// <summary>
    /// The best score for each mode and difficulty (six tables), the best combo in any round and
    /// how many rounds were played, on the device.
    /// </summary>
    public sealed class HighScores
    {
        const string Prefix = "targettoss.records.";

        readonly IRecordStore store;

        public HighScores(IRecordStore store) => this.store = store;

        static string BestKey(GameMode mode, Difficulty difficulty) => $"{Prefix}best.{mode}.{difficulty}".ToLowerInvariant();

        public int Best(GameMode mode, Difficulty difficulty) => store.GetInt(BestKey(mode, difficulty), 0);

        public int BestCombo => store.GetInt(Prefix + "combo", 0);

        public int RoundsPlayed => store.GetInt(Prefix + "rounds", 0);

        /// <summary>Records a finished round and saves the store once.</summary>
        public RecordUpdate Submit(GameMode mode, Difficulty difficulty, int points, int bestCombo)
        {
            int previous = Best(mode, difficulty);
            bool newBest = points > previous;
            if (newBest)
            {
                store.SetInt(BestKey(mode, difficulty), points);
            }
            bool newCombo = bestCombo > BestCombo;
            if (newCombo)
            {
                store.SetInt(Prefix + "combo", bestCombo);
            }
            store.SetInt(Prefix + "rounds", RoundsPlayed + 1);
            store.Save();
            return new RecordUpdate(newBest, newCombo, previous);
        }
    }
}
