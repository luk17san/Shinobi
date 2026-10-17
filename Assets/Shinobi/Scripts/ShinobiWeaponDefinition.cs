using UnityEngine;

namespace Shinobi
{
    [CreateAssetMenu(menuName = "Shinobi/Bron")]
    public sealed class ShinobiWeaponDefinition : ScriptableObject
    {
        public string displayName = "Nowa bron";
        public ShinobiAttackDefinition[] combo = new ShinobiAttackDefinition[0];
        public ShinobiContextCombos contextCombos = new ShinobiContextCombos();
        [Min(0.01f)] public float comboWindow = 0.5f;
        public GameObject visualPrefab;
        public Texture2D hudIcon;
        public Vector3 localPosition;
        public Vector3 localEulerAngles;
        public Vector3 localScale = Vector3.one;
        [Header("Rzut")]
        public bool throwable;
        public ShinobiThrowSettings throwing = new ShinobiThrowSettings();

        public bool IsUsable
        {
            get
            {
                if (contextCombos != null && contextCombos.standing != null && contextCombos.standing.IsUsable) return true;
                if (combo == null || combo.Length == 0) return false;
                foreach (var definition in combo) if (definition == null || definition.attack == null) return false;
                return true;
            }
        }
    }

    [System.Serializable]
    public sealed class ShinobiThrowSettings
    {
        [Min(1f)] public float speed = 18f;
        [Min(.1f)] public float range = 15f;
        [Min(0f)] public float damage = 20f;
        [Min(.05f)] public float cooldown = .7f;
        [Min(0f)] public float windup = .24f;
        [Min(.01f)] public float recovery = .26f;
        [Range(.01f,.3f)] public float collisionRadius = .07f;
        public bool spin;
        public bool explosive;
        [Min(.1f)] public float explosionRadius = 1.5f;
        [Min(0f)] public float explosionDamage = 40f;
        public AnimationClip animation;
        public GameObject explosionEffect;
        public ShinobiThrowSettings Snapshot() => (ShinobiThrowSettings)MemberwiseClone();
    }
}

