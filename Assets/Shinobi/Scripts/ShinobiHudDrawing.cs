using UnityEngine;
using UnityEngine.UIElements;
namespace Shinobi
{
    internal static class ShinobiHudDrawing
    {
        public static void Frame(MeshGenerationContext context)
        {
            var p=context.painter2D; float w=context.visualElement.layout.width,h=context.visualElement.layout.height;
            if(float.IsNaN(w)||w<12||h<12) return;
            p.BeginPath(); p.MoveTo(new Vector2(8,1)); p.LineTo(new Vector2(w-12,1)); p.LineTo(new Vector2(w-1,12)); p.LineTo(new Vector2(w-1,h-8)); p.LineTo(new Vector2(w-8,h-1)); p.LineTo(new Vector2(9,h-1)); p.LineTo(new Vector2(1,h-9)); p.LineTo(new Vector2(1,9)); p.ClosePath();
            p.fillColor=new Color(0.035f,0.09f,0.105f,0.95f); p.Fill(); p.strokeColor=new Color(0.22f,0.12f,0.06f); p.lineWidth=4; p.Stroke(); p.strokeColor=new Color(0.82f,0.66f,0.34f); p.lineWidth=1.5f; p.Stroke();
            p.BeginPath(); p.MoveTo(new Vector2(4,12)); p.LineTo(new Vector2(4,h-14)); p.LineTo(new Vector2(10,h-19)); p.LineTo(new Vector2(10,15)); p.ClosePath(); p.fillColor=new Color(0.47f,0.12f,0.11f); p.Fill();
            p.BeginPath(); p.MoveTo(new Vector2(3,8)); p.LineTo(new Vector2(18,8)); p.MoveTo(new Vector2(w-19,h-7)); p.LineTo(new Vector2(w-4,h-7)); p.lineWidth=3; p.strokeColor=new Color(0.45f,0.28f,0.1f); p.Stroke();
        }
        public static void Icon(MeshGenerationContext context,string kind)
        {
            var p=context.painter2D; float w=context.visualElement.layout.width,h=context.visualElement.layout.height;
            if(float.IsNaN(w)||w<1||h<1) return;
            Vector2 P(float x,float y)=>new Vector2(x*w,y*h);
            p.lineWidth=1.5f; p.strokeColor=new Color(1,0.88f,0.62f); p.fillColor=p.strokeColor;
            p.BeginPath();
            if(kind=="heart")
            {
                p.MoveTo(P(.5f,.88f)); p.LineTo(P(.08f,.42f)); p.LineTo(P(.08f,.2f)); p.LineTo(P(.27f,.09f)); p.LineTo(P(.5f,.28f)); p.LineTo(P(.73f,.09f)); p.LineTo(P(.92f,.2f)); p.LineTo(P(.92f,.42f)); p.ClosePath(); p.fillColor=new Color(1,.3f,.36f); p.Fill();
            }
            else if(kind=="chakra")
            {
                p.MoveTo(P(.5f,.02f)); p.BezierCurveTo(P(.48f,.35f),P(.1f,.46f),P(.17f,.73f)); p.BezierCurveTo(P(.23f,1.02f),P(.79f,1.02f),P(.83f,.72f)); p.BezierCurveTo(P(.9f,.45f),P(.62f,.3f),P(.5f,.02f)); p.ClosePath(); p.fillColor=new Color(.18f,.81f,.95f); p.Fill();
            }
            else if(kind=="stamina")
            {
                p.MoveTo(P(.49f,.04f)); p.LineTo(P(.12f,.55f)); p.LineTo(P(.48f,.55f)); p.LineTo(P(.29f,.97f)); p.LineTo(P(.91f,.39f)); p.LineTo(P(.57f,.39f)); p.LineTo(P(.79f,.04f)); p.ClosePath(); p.fillColor=new Color(1,.8f,.25f); p.Fill();
            }
            else if(kind=="speed")
            {
                p.MoveTo(P(.05f,.22f)); p.LineTo(P(.63f,.22f)); p.MoveTo(P(.02f,.43f)); p.LineTo(P(.45f,.43f)); p.MoveTo(P(.08f,.65f)); p.LineTo(P(.35f,.65f)); p.MoveTo(P(.48f,.1f)); p.LineTo(P(.84f,.45f)); p.LineTo(P(.49f,.9f)); p.lineWidth=3; p.Stroke();
            }
            else
            {
                // Two stylized hands meeting in a seal, drawn independently from its keycap.
                p.MoveTo(P(.1f,.88f)); p.LineTo(P(.25f,.55f)); p.LineTo(P(.38f,.45f)); p.LineTo(P(.4f,.07f)); p.LineTo(P(.49f,.07f)); p.LineTo(P(.5f,.56f));
                p.MoveTo(P(.9f,.88f)); p.LineTo(P(.75f,.55f)); p.LineTo(P(.62f,.45f)); p.LineTo(P(.6f,.07f)); p.LineTo(P(.51f,.07f)); p.LineTo(P(.5f,.7f)); p.LineTo(P(.32f,.9f));
                p.MoveTo(P(.22f,.7f)); p.LineTo(P(.37f,.65f)); p.LineTo(P(.62f,.9f)); p.Stroke();
            }
        }
    }
}
