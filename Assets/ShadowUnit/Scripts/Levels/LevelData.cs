using System;
using UnityEngine;

namespace ShadowUnit
{
    public enum ObjectiveType { Rescue, Destroy, StealIntel, Defend, Escort, Reach }

    [Serializable]
    public class LevelObjective
    {
        public ObjectiveType type;
        [TextArea(1, 3)] public string description;
    }

    /// <summary>
    /// One campaign level: where it happens, why, what to do and how hard it is.
    /// Assets live in Resources/ShadowUnit/Levels (made by Shadow Unit > Create Base Assets).
    /// Spawn and objective positions are added when each level is greyboxed (LevelManager, roadmap Step 8).
    /// </summary>
    [CreateAssetMenu(fileName = "NewLevel", menuName = "Shadow Unit/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Identity")]
        public int number = 1;
        public string levelName = "Level";
        [Tooltip("Map location from docs/WORLD.md, e.g. Market District.")]
        public string location;
        [TextArea(2, 5)] public string storyReason;

        [Header("Objectives")]
        public LevelObjective[] objectives = new LevelObjective[0];
        [Tooltip("Where the squad leaves once the objectives are done.")]
        public string extractionPoint;

        [Header("Enemies")]
        public int enemyCount = 10;
        [TextArea(1, 3)] public string enemyTypes;
        [Tooltip("Empty when the level has no boss.")]
        public string boss;
        [TextArea(1, 3)] public string bossNotes;

        [Header("Pacing")]
        [Range(1, 5)] public int difficulty = 1;
        public float estimatedMinutes = 6f;
        [TextArea(1, 3)] public string wowMoment;

        [Header("Layout (20 x 20 sketch)")]
        [Tooltip("S spawn, O objective, # cover/walls, E enemies, B boss, X extraction, ~ water, . open ground.")]
        [TextArea(20, 22)] public string asciiLayout;

        public bool HasBoss => !string.IsNullOrEmpty(boss);

        /// <summary>All level assets in campaign order.</summary>
        public static LevelData[] LoadAll()
        {
            LevelData[] all = Resources.LoadAll<LevelData>("ShadowUnit/Levels");
            Array.Sort(all, (a, b) => a.number.CompareTo(b.number));
            return all;
        }
    }
}
