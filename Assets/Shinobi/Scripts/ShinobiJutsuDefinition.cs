using UnityEngine;

namespace Shinobi
{
    public enum ShinobiSeal { Up, Down, Left, Right }
    [CreateAssetMenu(menuName = "Shinobi/Jutsu")]
    public sealed class ShinobiJutsuDefinition : ScriptableObject
    {
        public ShinobiSeal[] sequence = { ShinobiSeal.Up, ShinobiSeal.Right, ShinobiSeal.Down };
        [Min(1)] public int level = 1;
        [Min(0f)] public float chakraCost = 20f;
        [Min(0f)] public float damage = 30f;
        [Min(0f)] public float windup = 0.3f;
        [Min(0f)] public float recovery = 0.3f;
        [Min(0.1f)] public float radius = 2f;
        [Min(0f)] public float forwardOffset = 2f;
        public AnimationClip animation;
        public GameObject effect;
    }
}
