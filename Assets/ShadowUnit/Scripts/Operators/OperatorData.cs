using UnityEngine;

namespace ShadowUnit
{
    public enum OperatorClass { Assault, Support, Recon }

    /// <summary>
    /// One playable operator: identity, base stats, one active ability and one passive perk.
    /// Assets live in Resources/ShadowUnit/Operators (made by Shadow Unit > Create Base Assets).
    /// Abilities are described here and implemented in Phase 3 (AbilityController).
    /// </summary>
    [CreateAssetMenu(fileName = "NewOperator", menuName = "Shadow Unit/Operator Data")]
    public class OperatorData : ScriptableObject
    {
        [Header("Identity")]
        public string codeName = "Operator";
        public OperatorClass operatorClass = OperatorClass.Assault;
        [TextArea(2, 5)] public string backstory;
        [Tooltip("Accent colour for menus and the HUD.")]
        public Color accent = Color.white;

        [Header("Base stats")]
        public float maxHealth = 100f;
        public float maxArmour = 50f;
        [Tooltip("Multiplies walk, sprint and crouch speed.")]
        public float moveSpeedMultiplier = 1f;
        [Tooltip("Seconds without damage before health regenerates.")]
        public float regenDelay = 5f;
        [Tooltip("Multiplies the distance at which enemy bots can spot this operator.")]
        public float spotRangeMultiplier = 1f;
        [Tooltip("Name of the starting primary weapon (a WeaponData asset).")]
        public string startingWeapon = "VK-7 Carbine";

        [Header("Active ability")]
        public string abilityName;
        [TextArea(2, 4)] public string abilityDescription;
        public float abilityCooldown = 30f;
        [Tooltip("Seconds the effect lasts (0 = instant).")]
        public float abilityDuration;

        [Header("Passive perk")]
        public string passiveName;
        [TextArea(2, 4)] public string passiveDescription;

        [Header("Balance")]
        [TextArea(1, 3)] public string strength;
        [TextArea(1, 3)] public string weakness;

        /// <summary>All operator assets, in class order (Assault, Support, Recon).</summary>
        public static OperatorData[] LoadAll()
        {
            OperatorData[] all = Resources.LoadAll<OperatorData>("ShadowUnit/Operators");
            System.Array.Sort(all, (a, b) => a.operatorClass.CompareTo(b.operatorClass));
            return all;
        }
    }
}
