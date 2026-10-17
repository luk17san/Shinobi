using UnityEngine;

namespace Shinobi
{
    public enum ShinobiAttackContext { Standing, Crouching, Airborne, LongJump }

    [CreateAssetMenu(menuName = "Shinobi/Combo")]
    public sealed class ShinobiComboDefinition : ScriptableObject
    {
        public string displayName = "Nowe combo";
        [TextArea] public string description;
        [Min(0.01f)] public float comboWindow = 0.5f;
        public ShinobiAttackDefinition[] attacks = new ShinobiAttackDefinition[0];
        public bool IsUsable => attacks != null && attacks.Length > 0
            && System.Array.TrueForAll(attacks, a => a != null && a.attack != null);
    }

    [System.Serializable]
    public sealed class ShinobiContextCombos
    {
        public ShinobiComboDefinition standing, crouching, airborne, longJump;
        public ShinobiComboDefinition Get(ShinobiAttackContext context)
        {
            switch (context) {
                case ShinobiAttackContext.Crouching: return crouching;
                case ShinobiAttackContext.Airborne: return airborne;
                case ShinobiAttackContext.LongJump: return longJump;
                default: return standing;
            }
        }
    }
}
