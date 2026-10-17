using UnityEngine;
namespace Shinobi {
    [CreateAssetMenu(menuName="Shinobi/Animacje postaci")]
    public sealed class ShinobiAnimationSet : ScriptableObject {
        public AnimationClip idle, walk, run, jump, fall, crouch, crouchWalk, dash, hit, block, guardBreak, focus;
        [Range(0.01f,0.3f)] public float crossFade=0.1f;
    }
}
