using System;
using UnityEngine;

namespace Shinobi
{
    [Serializable]
    public sealed class ShinobiAttack
    {
        [Min(1)] public int level = 1;
        [Min(0f)] public float pushDistance;
        [Min(0.1f)] public float hitHeight = 1.2f;
        [Min(0.1f)] public float hitDepth = 0.8f;
        public float hitCenterHeight = 1f;
        [Min(1f)] public float damage = 20f;
        [Min(0f)] public float windup = 0.12f;
        [Min(0.01f)] public float activeDuration = 0.08f;
        [Min(0.01f)] public float recovery = 0.25f;
        [Min(0.1f)] public float reach = 1.3f;

        public ShinobiAttack Snapshot()
        {
            return new ShinobiAttack {
                level = Mathf.Max(1, level), pushDistance = Mathf.Max(0f, pushDistance),
                hitHeight = Mathf.Max(0.1f, hitHeight), hitDepth = Mathf.Max(0.1f, hitDepth), hitCenterHeight = hitCenterHeight,
                damage = Mathf.Max(1f, damage), windup = Mathf.Max(0f, windup),
                activeDuration = Mathf.Max(0.01f, activeDuration), recovery = Mathf.Max(0.01f, recovery),
                reach = Mathf.Max(0.1f, reach)
            };
        }
    }
}
