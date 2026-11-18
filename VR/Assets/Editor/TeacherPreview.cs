using System.IO;
using System.Linq;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class TeacherPreview {
 public static void Capture(){
    EditorSceneManager.OpenScene("Assets/HouseTour.unity");
    var surfaces=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
    var withLightmaps=surfaces.Where(r=>r.lightmapIndex>=0 && r.lightmapIndex<LightmapSettings.lightmaps.Length).ToArray();
    foreach(var name in new[]{"Plaster","Floor","Roof","Concrete"}){
        if(!withLightmaps.Any(r=>r.sharedMaterials.Any(m=>m && m.name==name)))
            throw new System.Exception("Missing baked surface: "+name);
    }
    var probes=Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None);
    if(!probes.Any(p=>p.bakedTexture && p.boxProjection)) throw new System.Exception("Missing baked room reflection");
    Debug.Log("SURFACES_OK "+withLightmaps.Length+" lightmapped renderers, "+LightmapSettings.lightmaps.Length+" atlases, "+LightmapSettings.lightProbes.count+" light probes");
    var cam=Camera.main;
    cam.transform.SetParent(null,true);
    Directory.CreateDirectory("Builds/Review");
    var names=new[]{"South-stair","Living-room","North-deck","Rainwater-tanks"};
    var points=new[]{new Vector3(11,1.74f,-13),new Vector3(-1.47f,4.30f,-1.80f),new Vector3(-2.75f,4.30f,6.55f),new Vector3(-.25f,1.55f,5.75f)};
    var targets=new[]{new Vector3(.1f,3.5f,-3.1f),new Vector3(2.65f,3.6f,.75f),new Vector3(1.5f,3.5f,2.9f),new Vector3(0,1.05f,.25f)};
    cam.fieldOfView=72;cam.aspect=1.6f;
    for(int i=0;i<names.Length;i++){
      cam.transform.position=points[i];cam.transform.LookAt(targets[i]);
      var rt=RenderTexture.GetTemporary(1152,720,24);cam.targetTexture=rt;cam.Render();cam.Render();
      RenderTexture.active=rt;var tex=new Texture2D(1152,720,TextureFormat.RGB24,false);
      tex.ReadPixels(new Rect(0,0,1152,720),0,0);tex.Apply();File.WriteAllBytes("Builds/Review/"+names[i]+".jpg",tex.EncodeToJPG(94));
      cam.targetTexture=null;RenderTexture.active=null;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);
    }
 }
 public static void Verify(){Capture();TeacherTourCheck.Begin();}
}
