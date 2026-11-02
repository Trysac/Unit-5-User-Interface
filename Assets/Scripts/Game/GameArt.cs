using UnityEngine;

namespace TargetToss.Game
{
    /// <summary>The generated material and the synthesized audio, wired up by ProjectBuilder.</summary>
    public sealed class GameArt : ScriptableObject
    {
        [Header("Materials")]
        public Material Glow;

        [Header("Music")]
        public AudioClip TitleMusic;
        public AudioClip PlayMusic;

        [Header("Targets")]
        public AudioClip Pop;
        public AudioClip Bonus;
        public AudioClip Bad;
        public AudioClip Bomb;
        public AudioClip Whiff;
        public AudioClip Fell;
        public AudioClip SlowTime;
        public AudioClip Magnet;
        public AudioClip PowerEnd;

        [Header("Round")]
        public AudioClip Multiplier;
        public AudioClip ComboBreak;
        public AudioClip LifeLost;
        public AudioClip Tick;
        public AudioClip TimeUp;
        public AudioClip NewRecord;
        public AudioClip RoundOver;
        public AudioClip Go;

        [Header("Menus")]
        public AudioClip Move;
        public AudioClip Select;
        public AudioClip Pause;
    }
}
