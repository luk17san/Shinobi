using UnityEngine;
namespace Shinobi
{
    [CreateAssetMenu(menuName="Shinobi/Efekty walki")]
    public sealed class ShinobiFeedbackProfile : ScriptableObject
    {
        public bool showEffects=true;
        public bool showLabels=true;
        public Color hitColor=new Color(1f,0.32f,0.18f);
        public Color blockColor=new Color(0.35f,0.85f,1f);
        public Color guardBreakColor=new Color(1f,0.7f,0.12f);
        public Color swingColor=new Color(1f,0.93f,0.64f);
        public Color jutsuColor=new Color(0.25f,0.8f,1f);
        public Color dashColor=new Color(0.85f,0.94f,1f);
        [Range(0.1f,1f)] public float effectDuration=0.3f;
        [Range(0.02f,0.15f)] public float lineWidth=0.055f;
        [Range(0.3f,2f)] public float labelDuration=0.7f;
    }
}
