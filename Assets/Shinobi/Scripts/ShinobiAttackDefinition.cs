using UnityEngine;

namespace Shinobi
{
    [CreateAssetMenu(menuName = "Shinobi/Atak wrecz")]
    public sealed class ShinobiAttackDefinition : ScriptableObject
    {
        public ShinobiAttack attack = new ShinobiAttack();
        public AnimationClip animation;
        public GameObject effect;
    }
}
