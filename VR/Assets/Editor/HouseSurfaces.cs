using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

internal sealed class HouseSurfaceImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Surfaces/", StringComparison.Ordinal)) return;
        var importer = (TextureImporter)assetImporter;
        importer.mipmapEnabled = true;
        importer.filterMode = FilterMode.Trilinear;
        if (assetPath.EndsWith(".hdr")) {
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.maxTextureSize = 2048;
            importer.wrapModeU = TextureWrapMode.Repeat;
            importer.wrapModeV = TextureWrapMode.Clamp;
            return;
        }
        bool normal = assetPath.EndsWith("Normal.jpg");
        bool gloss = assetPath.EndsWith("Gloss.png");
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = !normal && !gloss;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.anisoLevel = 6;
        var android = importer.GetPlatformTextureSettings("Android");
        android.overridden = true;
        android.maxTextureSize = gloss ? 512 : 2048;
        android.format = TextureImporterFormat.ASTC_6x6;
        importer.SetPlatformTextureSettings(android);
    }
}

internal sealed class HouseSurfaces
{
    const string Folder = "Assets/Generated/Finishes/";
    readonly Dictionary<string, Material> palette = new();
    public readonly Material Ground;

    public HouseSurfaces()
    {
        Directory.CreateDirectory(Folder);
        Ground = Surface("Ground","Ground",3f,"CCC8B8",0,.48f,.70f);

        Surface("Grass","Ground",3f,"748650",0,.30f,.45f);
        Surface("Earth",null,1,"776653",0,.18f);
        Surface("Foliage",null,1,"637B4D",0,.14f);
        Surface("Bark",null,1,"6C5742",0,.18f);
        Surface("FlameRed",null,1,"A83D23",0,.28f);
        Surface("FlameOrange",null,1,"DB6B2E",0,.28f);
        Surface("FlameGold",null,1,"E4AA49",0,.28f);
        Surface("Floor","Floor",1.70f,"FFFFFF",0,.95f,.40f);
        Surface("Deck","Deck",1.80f,"E3DFD9",0,.74f,.48f);
        Surface("Tread","Deck",1.80f,"F0E9DC",0,.67f,.45f);
        Surface("Battens","Joinery",1.50f,"F5E5C9",0,.55f,.38f);
        Surface("Concrete","Concrete",1.23f,"CACAC7",0,.70f,.48f);
        Surface("Cladding","Metal",1.12f,"525960",.42f,.83f,.85f);
        Surface("Roof","Metal",1.12f,"6D767E",.42f,.87f,.75f);
        Surface("Flashing",null,1,"555F67",.45f,.40f);
        Surface("Plaster",null,1,"E7E5DC",0,.15f);
        Surface("Soffit",null,1,"CCCDC4",0,.16f);
        Surface("Structure",null,1,"555D62",.48f,.37f);
        Surface("Frame",null,1,"343A40",.55f,.47f);
        Surface("Stainless",null,1,"B9C0C1",.83f,.66f);
        Surface("Chrome",null,1,"CBD0D2",.90f,.82f);
        Surface("Tank",null,1,"515B60",.22f,.36f);
        Surface("Birch","Joinery",1.5f,"FFF2D9",0,.80f,.25f);
        Surface("Cherry","Joinery",1.5f,"DABBA5",0,.78f,.28f);
        Surface("Bench",null,1,"D8D5CA",0,.42f);
        Surface("Door",null,1,"EBE9E0",0,.30f);
        Surface("Laminate",null,1,"DEDACA",0,.38f);
        Surface("Porcelain",null,1,"F2EEE3",0,.72f);
        Surface("WhiteLinen","Linen",.27f,"F0EDE3",0,.32f,.28f);
        Surface("BeigeLinen","Linen",.27f,"C9BBA4",0,.30f,.33f);
        Surface("Weave","Linen",.32f,"BCA47F",0,.34f,.46f);
        Surface("BlueLinen","Linen",.27f,"61768C",0,.32f,.35f);
        Surface("Carpet","Linen",.42f,"B8A68F",0,.22f,.69f);
        Surface("Leather",null,1,"3D3C3A",0,.37f);
        var glass = Surface("Glass",null,1,"B2D0D2",.15f,.93f);
        var tint=glass.GetColor("_BaseColor");tint.a=.045f;
        glass.SetColor("_BaseColor",tint);
        glass.SetFloat("_Surface",1);
        glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        glass.SetOverrideTag("RenderType","Transparent");
        glass.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);
        glass.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
        glass.SetInt("_ZWrite",0);
        glass.renderQueue=(int)RenderQueue.Transparent;
        glass.SetShaderPassEnabled("ShadowCaster",false);
    }

    Material Surface(string name,string scan,float metres,string hex,float metallic,float smoothness,float relief=0)
    {
        string path=Folder+name+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){
            mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat,path);
        }
        ColorUtility.TryParseHtmlString("#"+hex,out var color);
        mat.SetColor("_BaseColor",color);
        mat.SetFloat("_Metallic",metallic);
        mat.SetFloat("_Smoothness",smoothness);
        if(scan!=null){
            mat.SetTexture("_BaseMap",Texture(scan+"Color.jpg"));
            mat.SetTexture("_BumpMap",Texture(scan+"Normal.jpg"));
            mat.SetFloat("_BumpScale",relief);
            mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_MetallicGlossMap",Texture(scan+"Gloss.png"));
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.mainTextureScale=Vector2.one/metres;
        }
        EditorUtility.SetDirty(mat);
        palette.Add(name,mat);
        return mat;
    }

    static Texture2D Texture(string file)
    {
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Surfaces/"+file);
        if(tex==null)throw new BuildFailedException("Missing surface texture "+file);
        return tex;
    }

    public void Apply(MeshRenderer renderer,bool exhibit=false)
    {
        var slots=renderer.sharedMaterials;
        for(int i=0;i<slots.Length;i++){
            string name=slots[i].name;
            if(exhibit) name=name switch {
                "Charcoal steel"=>"Cladding", "Timber framing"=>"Battens", "Plasterboard"=>"Plaster", _=>name
            };
            else if(name.StartsWith("PH_"))name=name.Substring(3);
            if(!palette.TryGetValue(name,out slots[i])) throw new BuildFailedException("No Quest surface for "+name);
        }
        renderer.sharedMaterials=slots;
    }

    // Broad baked emitters approximate diffuse sky entering the glazed bays and open undercroft.
    // They follow the openings and add no per-frame lights to the mobile scene.
    static void SkyOpening(string name,Vector3 position,Vector3 direction,Vector2 size,float intensity)
    {
        var light=new GameObject(name).AddComponent<Light>();
        light.type=LightType.Rectangle;
        light.lightmapBakeType=LightmapBakeType.Baked;
        light.shadows=LightShadows.Soft;
        light.transform.SetPositionAndRotation(position,Quaternion.LookRotation(direction));
        light.areaSize=size; light.range=12;
        light.color=new Color(.98f,.975f,.94f);
        light.intensity=intensity;
    }

    public static void Daylight(Light sun)
    {
        var sky=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Daylight.mat");
        if(sky==null){sky=new Material(Shader.Find("Skybox/Panoramic"));AssetDatabase.CreateAsset(sky,Folder+"Daylight.mat");}
        sky.SetTexture("_MainTex",Texture("Daylight.hdr"));
        sky.SetFloat("_Exposure",1.05f);
        sky.SetFloat("_Rotation",180f);
        sky.SetColor("_Tint",new Color(.53f,.515f,.48f));
        EditorUtility.SetDirty(sky);
        RenderSettings.skybox=sky;
        RenderSettings.ambientMode=AmbientMode.Skybox;
        RenderSettings.ambientIntensity=1.10f;
        RenderSettings.defaultReflectionMode=DefaultReflectionMode.Skybox;
        RenderSettings.defaultReflectionResolution=128;
        RenderSettings.reflectionIntensity=.80f;
        RenderSettings.sun=sun;
        // Low afternoon sun through the north glazing: warm light, long shadows,
        // and neutral sky fill rather than uniformly bright window emitters.
        sun.transform.rotation=Quaternion.LookRotation(new Vector3(.35f,-.259f,-.90f).normalized);
        sun.color=new Color(1,.93f,.80f);
        sun.intensity=3.2f;
        sun.shadowAngle=2;
        DynamicGI.UpdateEnvironment();
        SkyOpening("North glazing daylight",new Vector3(0,3.91f,3.02f),Vector3.back,new Vector2(9.6f,2.12f),3.3f);
        SkyOpening("South glazing daylight",new Vector3(0,3.91f,-2.74f),Vector3.forward,new Vector2(4.15f,2.12f),3.3f);
        SkyOpening("North undercroft skylight",new Vector3(0,1.16f,3.2f),Vector3.back,new Vector2(23,2.10f),2.6f);
        SkyOpening("South undercroft skylight",new Vector3(0,1.16f,-3.42f),Vector3.forward,new Vector2(23,2.10f),2.6f);
        var probe=new GameObject("Living room reflections").AddComponent<ReflectionProbe>();
        probe.transform.position=new Vector3(0,3.96f,0);
        probe.size=new Vector3(11.5f,4.35f,6.3f);
        probe.boxProjection=true;
        probe.resolution=128;
        probe.mode=ReflectionProbeMode.Baked;
        probe.nearClipPlane=.12f;
        probe.farClipPlane=65;
        probe.cullingMask=~(1<<TourPanel.Layer);
    }
}
