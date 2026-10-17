using UnityEngine;
using UnityEngine.Rendering;
namespace Shinobi
{
    public enum ShinobiEffectKind { Hit, Block, GuardBreak, Swing, Jutsu, Dash, Clash }
    // Short-lived visuals only: no colliders, hit detection or changes to global time.
    public sealed class ShinobiCombatEffect : MonoBehaviour
    {
        public ShinobiEffectKind Kind { get; private set; }
        private float elapsed, lifetime, labelLifetime;
        private LineRenderer line;
        private Material material;
        private TextMesh label;
        private Color color;
        private Vector3 labelOrigin;
        public void Initialize(ShinobiEffectKind kind, Vector3[] points, Color tint, ShinobiFeedbackProfile profile, string text)
        {
            Kind=kind; color=tint; lifetime=Mathf.Max(0.1f,profile.effectDuration); labelLifetime=Mathf.Max(lifetime,profile.labelDuration);
            if(profile.showEffects)
            {
                line=gameObject.AddComponent<LineRenderer>(); line.useWorldSpace=false;
                line.positionCount=points.Length; line.SetPositions(points); line.widthMultiplier=profile.lineWidth;
                material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                material.SetColor("_BaseColor",tint); material.SetFloat("_Surface",1); material.SetFloat("_ZWrite",0);
                material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.SetOverrideTag("RenderType","Transparent"); material.renderQueue=3100;
                line.sharedMaterial=material; line.shadowCastingMode=ShadowCastingMode.Off; line.receiveShadows=false;
            }
            if(profile.showLabels && !string.IsNullOrEmpty(text))
            {
                var go=new GameObject("Label"); go.transform.SetParent(transform,false); go.transform.localPosition=new Vector3(0,kind==ShinobiEffectKind.GuardBreak?0.85f:0.95f,-0.1f);
                label=go.AddComponent<TextMesh>(); label.text=text; label.anchor=TextAnchor.MiddleCenter; label.alignment=TextAlignment.Center;
                label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize=48; label.characterSize=0.1f; label.color=color;
                label.GetComponent<MeshRenderer>().sharedMaterial=label.font.material;
                labelOrigin=go.transform.localPosition;
            }
        }
        private void Update()
        {
            elapsed+=Time.deltaTime;
            if(line!=null)
            {
                float t=Mathf.Clamp01(elapsed/lifetime); var tint=color; tint.a*=1-t;
                material.SetColor("_BaseColor",tint); line.enabled=t<1;
            }
            if(label!=null)
            {
                label.transform.localPosition=labelOrigin+Vector3.up*elapsed*0.6f;
                var tint=color; tint.a*=1-Mathf.Clamp01(elapsed/labelLifetime); label.color=tint;
            }
            if(elapsed>=(label!=null?labelLifetime:lifetime)) Destroy(gameObject);
        }
        private void OnDestroy() { if(material!=null) Destroy(material); }
    }
}

