using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallYonseiSymbol
    {
        public const string DataPath="Assets/Art/Meshes/ConvergenceHall/YonseiOfficialSymbolGeometry.json";
        [Serializable] class Shape {public int x,y,w,h;}
        [Serializable] class Geometry {public int width,height;public string source;public List<Shape> rectangles;}
        public static void AddToLectern(Transform parent)
        {
            var data=JsonUtility.FromJson<Geometry>(AssetDatabase.LoadAssetAtPath<TextAsset>(DataPath).text);
            var white=P.Mat("YonseiSymbolWhite",Color.white,.12f);var blue=P.Mat("YonseiSymbolBlue",new Color(0,.22f,.46f),.12f);
            var discVertices=new List<Vector3>{Vector3.zero};var discNormals=new List<Vector3>{Vector3.back};var discUV=new List<Vector2>{new Vector2(.5f,.5f)};var discTriangles=new List<int>();
            for(int i=0;i<128;i++){float angle=i*Mathf.PI*2/128;var v=new Vector3(Mathf.Sin(angle)*.1125f,Mathf.Cos(angle)*.1125f,0);discVertices.Add(v);discNormals.Add(Vector3.back);discUV.Add(new Vector2(v.x/.225f+.5f,v.y/.225f+.5f));discTriangles.AddRange(new[]{0,i+1,(i+1)%128+1});}
            var discMesh=new Mesh();discMesh.SetVertices(discVertices);discMesh.SetNormals(discNormals);discMesh.SetUVs(0,discUV);discMesh.SetTriangles(discTriangles,0);discMesh.RecalculateBounds();
            var backing=P.Group(parent,"OfficialSymbolBacking");backing.localPosition=new Vector3(0,.76f,-.279f);backing.gameObject.AddComponent<MeshFilter>().sharedMesh=P.SaveMesh("YonseiOfficialSymbolWhiteDisc",discMesh);backing.gameObject.AddComponent<MeshRenderer>().sharedMaterial=white;
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();const float diameter=.218f;
            foreach(var r in data.rectangles)
            {
                float left=(r.x/(float)data.width-.5f)*diameter,right=((r.x+r.w)/(float)data.width-.5f)*diameter;
                float top=(.5f-r.y/(float)data.height)*diameter,bottom=(.5f-(r.y+r.h)/(float)data.height)*diameter;int start=vertices.Count;
                vertices.AddRange(new[]{new Vector3(left,top,0),new Vector3(right,top,0),new Vector3(right,bottom,0),new Vector3(left,bottom,0)});
                normals.AddRange(new[]{Vector3.back,Vector3.back,Vector3.back,Vector3.back});uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});indices.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
            }
            var mesh=new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();
            var symbol=P.Group(parent,"OfficialYonseiSymbol");symbol.localPosition=new Vector3(0,.76f,-.281f);symbol.gameObject.AddComponent<MeshFilter>().sharedMesh=P.SaveMesh("YonseiOfficialSymbol",mesh);symbol.gameObject.AddComponent<MeshRenderer>().sharedMaterial=blue;
        }
    }
}
