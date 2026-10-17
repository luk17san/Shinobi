using UnityEngine;
using UnityEngine.InputSystem;

namespace Shinobi
{
    [CreateAssetMenu(menuName = "Shinobi/Zestaw walki")]
    public sealed class ShinobiCombatProfile : ScriptableObject
    {
        public Key meleeKey = Key.G;
        public Key focusKey = Key.J;
        public Key jumpKey = Key.W;
        public Key crouchKey = Key.S;
        public Key nextWeaponKey = Key.Y;
        public Key throwKey = Key.H;
        [Range(0.1f, 0.5f)] public float dashDoubleTapWindow = 0.25f;
        public bool autoBlock = true;
        [Min(1f)] public float maxStamina = 100f;
        [Min(0f)] public float blockCostMultiplier = 1f;
        [Min(0f)] public float staminaRegenRate = 20f;
        [Min(0f)] public float staminaRegenDelay = 1f;
        [Min(0.01f)] public float guardBreakDuration = 1f;
        [Range(0f, 90f)] public float blockHalfAngle = 60f;
        [Min(0.01f)] public float comboWindow = 0.5f;
        [Min(1f)] public float maxChakra = 100f;
        [Range(0f, 100f)] public float interruptChance = 60f;
        [Range(0f, 100f)] public float chancePerLevel = 15f;
        [Range(0f, 100f)] public float maximumInterruptChance = 95f;
        [Min(0f)] public float drawSeparation = 2f;
        public ShinobiAttackDefinition[] combo = new ShinobiAttackDefinition[3];
        public ShinobiContextCombos contextCombos = new ShinobiContextCombos();
        public ShinobiJutsuDefinition[] jutsu = new ShinobiJutsuDefinition[0];
    }
}
