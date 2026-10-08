using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    // Photo reference: armless nesting chair, positive Z faces the teaching wall.
    public static class ConvergenceHallChairGeometry
    {
        const string Mats="Assets/Art/Materials/ConvergenceHall/";
        const float BackLowering=.085f;
        sealed class Shape
        {
            public readonly List<Vector3> v=new List<Vector3>();
            public readonly List<Vector2> uv=new List<Vector2>();
            public readonly List<int> t=new List<int>();
            public int Add(Vector3 p,Vector2 tex){v.Add(p);uv.Add(tex);return v.Count-1;}
            public void Quad(int a,int b,int c,int d){t.AddRange(new[]{a,b,c,a,c,d});}
            public void Save(Transform parent,string name,Material mat)
            {
                var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                var go=P.Group(parent,name);go.gameObject.AddComponent<MeshFilter>().sharedMesh=P.SaveMesh("Interior_"+name,mesh);go.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;
            }
        }
        static List<Vector2> Outline(float width,float depth,float radius)
        {
            var points=new List<Vector2>();
            for(int corner=0;corner<4;corner++)
            {
                float cx=(corner==0||corner==3?1:-1)*(width/2-radius),cy=(corner<2?1:-1)*(depth/2-radius);
                for(int j=0;j<12;j++){float a=(corner*90+j*7.5f)*Mathf.Deg2Rad;points.Add(new Vector2(cx+Mathf.Cos(a)*radius,cy+Mathf.Sin(a)*radius));}
            }
            return points;
        }
        static void Cushion(Transform p,string name,float width,float depth,float height,float centre,Material mat)
        {
            var shape=new Shape();var points=Outline(width,depth,.065f);int n=points.Count;
            // Concentric bevel rings replace the old flat-sided extrusion.
            float[] scale={.20f,.78f,.94f,1f,.985f,.88f,.2f};
            float[] levels={height*.52f,height*.50f,height*.37f,0,-height*.29f,-height*.48f,-height*.5f};
            for(int ring=0;ring<scale.Length;ring++)foreach(var a in points)
            {
                float x=a.x*scale[ring],z=a.y*scale[ring];
                shape.Add(new Vector3(x,centre+levels[ring],z),new Vector2(x*36,z*36));
            }
            for(int r=0;r<scale.Length-1;r++)for(int j=0;j<n;j++)shape.Quad(r*n+j,r*n+(j+1)%n,(r+1)*n+(j+1)%n,(r+1)*n+j);
            int top=shape.Add(new Vector3(0,centre+height*.52f,0),Vector2.zero),bottom=shape.Add(new Vector3(0,centre-height*.5f,0),Vector2.zero);
            for(int j=0;j<n;j++){shape.t.AddRange(new[]{top,(j+1)%n,j});int end=(scale.Length-1)*n;shape.t.AddRange(new[]{bottom,end+j,end+(j+1)%n});}
            shape.Save(p,name,mat);
        }
        // A little horizontal wrap and vertical lumbar bow, instead of a flat mesh rectangle.
        static Vector3 BackPoint(Vector2 a,float dz=0)
        {
            float h=Mathf.InverseLerp(.605f,.955f,a.y);
            return new Vector3(a.x,a.y-BackLowering,-.225f-.055f*h+.058f*Mathf.Pow(a.x/.225f,2)+.018f*Mathf.Sin(h*Mathf.PI)+dz);
        }
        static void Back(Transform p,Material black,Material woven)
        {
            var outer=Outline(.45f,.350f,.035f);var inner=Outline(.393f,.272f,.026f);int n=outer.Count;
            var ring=new Shape();
            for(int band=0;band<4;band++)for(int j=0;j<n;j++)
            {
                var a=band%2==0?outer[j]:inner[j];a.y+=band%2==0?.780f:.772f;
                ring.Add(BackPoint(a,band<2?.012f:-.012f),Vector2.zero);
            }
            for(int j=0;j<n;j++)
            {
                int next=(j+1)%n;ring.Quad(j,next,n+next,n+j);ring.Quad(2*n+j,3*n+j,3*n+next,2*n+next);
                ring.Quad(j,2*n+j,2*n+next,next);ring.Quad(n+j,n+next,3*n+next,3*n+j);
            }
            ring.Save(p,"PhotoChairBackFrame",black);
            // Fill the rounded opening with a curved fine-grid sheet; boundary follows the frame.
            var sheet=new Shape();const int rows=18,cols=24;
            for(int row=0;row<=rows;row++)for(int col=0;col<=cols;col++)
            {
                // Tuck the sheet 3mm into the rim so curved tessellation cannot leave a slit.
                float u=col/(float)cols,h=row/(float)rows,y=.633f+h*.278f;
                float corner=Mathf.Max(0,Mathf.Abs(y-.772f)-(.139f-.026f));
                float half=.1995f-.026f+Mathf.Sqrt(Mathf.Max(0,.026f*.026f-corner*corner));
                float x=(u*2-1)*half;
                sheet.Add(BackPoint(new Vector2(x,y)),new Vector2(u*4f,h*3f));
            }
            for(int row=0;row<rows;row++)for(int col=0;col<cols;col++)
            {int a=row*(cols+1)+col;sheet.Quad(a,a+1,a+cols+2,a+cols+1);}
            sheet.Save(p,"PhotoChairWovenBack",woven);
        }
        static Vector3 Catmull(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float u)
            =>.5f*((2*b)+(-a+c)*u+(2*a-5*b+4*c-d)*u*u+(-a+3*b-3*c+d)*u*u*u);
        static void BentTube(Transform p,string name,Vector3[] controls,float diameter,Material mat)
        {
            var path=new List<Vector3>();
            for(int k=0;k<controls.Length-1;k++)for(int j=0;j<6;j++)
                path.Add(Catmull(controls[Mathf.Max(k-1,0)],controls[k],controls[k+1],controls[Mathf.Min(k+2,controls.Length-1)],j/6f));
            path.Add(controls[controls.Length-1]);var shape=new Shape();const int sides=10;
            for(int k=0;k<path.Count;k++)
            {
                var tangent=(path[Mathf.Min(k+1,path.Count-1)]-path[Mathf.Max(k-1,0)]).normalized;
                var a=Vector3.Cross(tangent,Vector3.right).normalized;var b=Vector3.Cross(tangent,a).normalized;
                for(int j=0;j<sides;j++){float angle=j*Mathf.PI*2/sides;shape.Add(path[k]+(a*Mathf.Cos(angle)+b*Mathf.Sin(angle))*diameter/2,new Vector2(j/(float)sides,k/(float)path.Count));}
            }
            for(int k=0;k<path.Count-1;k++)for(int j=0;j<sides;j++)shape.Quad(k*sides+j,k*sides+(j+1)%sides,(k+1)*sides+(j+1)%sides,(k+1)*sides+j);
            shape.Save(p,name,mat);
        }
        static void Caster(Transform p,string name,float x,float z,Material black,Material silver)
        {
            P.Cylinder(p,name+"Stem",new Vector3(x,.070f,z),new Vector3(.021f,.014f,.021f),black,Quaternion.identity);
            // Twin rubber wheels, with a recessed grey hub, rather than a silver box fork.
            for(int s=-1;s<=1;s+=2)
            {
                float side=x+s*.015f;
                P.Cylinder(p,name+"Tyre"+s,new Vector3(side,.032f,z+.005f),new Vector3(.054f,.008f,.054f),black,Quaternion.Euler(0,0,90));
                P.Cylinder(p,name+"Hub"+s,new Vector3(side+s*.0082f,.032f,z+.005f),new Vector3(.038f,.0015f,.038f),silver,Quaternion.Euler(0,0,90));
            }
            P.Box(p,name+"Fork",new Vector3(x,.051f,z),new Vector3(.018f,.034f,.037f),black);
        }
        static void Fabrics(Material blue,Material woven)
        {
            blue.SetColor("_BaseColor",new Color(.035f,.35f,.50f));blue.SetFloat("_Smoothness",.08f);
            var knit=AssetDatabase.LoadAssetAtPath<Texture2D>(Mats+"PhotoChairKnit.asset");
            if(!knit)
            {
                knit=new Texture2D(128,128,TextureFormat.RGBA32,true){name="PhotoChairKnit",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};var pixels=new Color[128*128];
                for(int y=0;y<128;y++)for(int x=0;x<128;x++){float value=((x+y%2)%4==0||y%4==0)?.83f:.98f;pixels[y*128+x]=new Color(value,value,value,1);}
                knit.SetPixels(pixels);knit.Apply();AssetDatabase.CreateAsset(knit,Mats+"PhotoChairKnit.asset");
            }
            blue.SetTexture("_BaseMap",knit);EditorUtility.SetDirty(blue);
            var grid=AssetDatabase.LoadAssetAtPath<Texture2D>(Mats+"PhotoChairFineMesh.asset");
            if(!grid)
            {
                grid=new Texture2D(512,512,TextureFormat.RGBA32,true){name="PhotoChairFineMesh",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};var pixels=new Color[512*512];
                for(int y=0;y<512;y++)for(int x=0;x<512;x++)pixels[y*512+x]=new Color(.85f,.85f,.85f,(x%8<2||y%8<3)?1:0);
                grid.SetPixels(pixels);grid.Apply();AssetDatabase.CreateAsset(grid,Mats+"PhotoChairFineMesh.asset");
            }
            woven.SetTexture("_BaseMap",grid);woven.SetFloat("_AlphaClip",1);woven.SetFloat("_Cutoff",.35f);woven.SetFloat("_Cull",0);woven.EnableKeyword("_ALPHATEST_ON");woven.renderQueue=2450;
            // The actual fine weave has tiny holes. Disable coarse perforated shadows at room scale.
            woven.SetShaderPassEnabled("ShadowCaster",false);EditorUtility.SetDirty(woven);
        }
        public static void Build(Transform p,Material steel,Material black,Material blue,Material woven)
        {
            Fabrics(blue,woven);Cushion(p,"PhotoChairCushion",.445f,.425f,.06f,.445f,blue);Cushion(p,"PhotoChairSeatShell",.430f,.400f,.016f,.408f,black);Back(p,black,woven);
            for(int side=-1;side<=1;side+=2)
            {
                float x=side*.210f;
                // Rear upright bends into the diagonal front leg, with a continuous elbow.
                BentTube(p,"PhotoChairUprightFrontLeg"+side,new[]{new Vector3(x,.70f-BackLowering,-.213f),new Vector3(x,.48f,-.193f),new Vector3(x,.377f,-.162f),new Vector3(x,.330f,-.098f),new Vector3(side*.230f,.120f,.207f),new Vector3(side*.230f,.076f,.231f)},.028f,steel);
                BentTube(p,"PhotoChairCrossRearLeg"+side,new[]{new Vector3(x,.408f,.160f),new Vector3(x,.377f,.102f),new Vector3(side*.222f,.143f,-.210f),new Vector3(side*.230f,.101f,-.253f),new Vector3(side*.230f,.076f,-.260f)},.028f,steel);
                P.Cylinder(p,"PhotoChairBackPivot"+side,new Vector3(side*.222f,.707f-BackLowering,-.210f),new Vector3(.060f,.012f,.060f),black,Quaternion.Euler(0,0,90));
                P.Cylinder(p,"PhotoChairPivotInset"+side,new Vector3(side*.235f,.707f-BackLowering,-.210f),new Vector3(.043f,.001f,.043f),black,Quaternion.Euler(0,0,90));
                Caster(p,"PhotoChairFrontCaster"+side,side*.230f,.231f,black,steel);Caster(p,"PhotoChairRearCaster"+side,side*.230f,-.260f,black,steel);
            }
            P.Tube(p,"PhotoChairUnderSeatBrace",new Vector3(-.205f,.394f,.080f),new Vector3(.205f,.394f,.080f),.020f,steel);
            var hit=p.gameObject.AddComponent<BoxCollider>();hit.center=new Vector3(0,.485f,-.02f);hit.size=new Vector3(.55f,.97f,.62f);p.gameObject.layer=LayerMask.NameToLayer("WheelchairObstacle");
        }
    }
}
