using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.Management;

public static class HouseBuild
{
    private const string ScenePath = "Assets/HouseTour.unity";
    private const string GreyboxScenePath = "Assets/Generated/Greybox.unity";
    private const string Generated = "Assets/Generated";

    [MenuItem("Platypus House/Prepare Tour")]
    public static void Prepare() => ConfigureAndCreate(false);

    [MenuItem("Platypus House/Build Quest APK")]
    public static void BuildQuest() => Build(false);

    public static void BuildGreybox() => Build(true);

    public static void BuildBoth()
    {
        Build(true);
        Build(false);
    }

    private static void Build(bool greybox)
    {
        ConfigureAndCreate(greybox);
        Directory.CreateDirectory("Builds");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { greybox ? GreyboxScenePath : ScenePath },
            locationPathName = greybox ? "Builds/Greybox.apk" : "Builds/PlatypusHouse.apk",
            target = BuildTarget.Android,
            options = CommandLineHas("-development") ? BuildOptions.Development : BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException(report.summary.result.ToString());
    }

    private static bool CommandLineHas(string option) => Environment.GetCommandLineArgs().Contains(option);

    private static void ConfigureAndCreate(bool greybox)
    {
        ConfigureAndroid();
        ConfigureLighting(greybox);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.76f,.81f,.86f);
        RenderSettings.ambientEquatorColor = new Color(.66f,.69f,.70f);
        RenderSettings.ambientGroundColor = new Color(.48f,.49f,.45f);
        RenderSettings.ambientLight = new Color(0.68f, 0.69f, 0.72f);

        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = .60f;
        sun.lightmapBakeType = greybox ? LightmapBakeType.Realtime : LightmapBakeType.Baked;
        sun.shadows = LightShadows.Soft;
        sun.shadowAngle = 20f;
        sun.transform.rotation = Quaternion.Euler(42f, -35f, 0f);

        var finishes = greybox ? null : new HouseSurfaces();
        var solid = finishes?.Ground ?? Material("Courtyard", new Color(.30f,.33f,.29f),"Universal Render Pipeline/Lit");
        var ground = Box("Ground", new Vector3(11f, -.127f, -6f), new Vector3(40f,.25f,30f), solid,null);
        ground.layer = HouseTour.Walkable;
        ground.GetComponent<MeshRenderer>().scaleInLightmap=.55f;
        GameObjectUtility.SetStaticEditorFlags(ground,StaticEditorFlags.ContributeGI);
        if (greybox) {
            var upper = Box("Upper floor",new Vector3(11,2.45f,-6),new Vector3(13,.3f,19),solid,null);
            upper.layer = HouseTour.Walkable;
        } else {
            HouseSurfaces.Daylight(sun);
            InstallHouse(finishes);
            // Continuous collision faces follow the open-rod guards from the reviewed IFC.
            Guard(new Vector3(6.24f,3.10f,-2.20f), new Vector3(.04f,1,4.44f));
            Guard(new Vector3(16.48f,3.10f,-2.20f), new Vector3(.04f,1,4.44f));
            Guard(new Vector3(11.36f,3.10f,.015f), new Vector3(10.24f,1,.04f));
            Guard(new Vector3(9.17f,3.10f,-12.68f), new Vector3(.04f,1,4.34f));
            Guard(new Vector3(11.39f,3.10f,-14.845f), new Vector3(4.44f,1,.04f));
            Guard(new Vector3(13.55f,3.10f,-11.955f), new Vector3(.04f,1,2.9f));
        }

        var tour = CreateRig();
        tour.Stops = new[] {
            Stop("Stair foot", new Vector3(18.30f,-.002f,-14.15f),270),
            Stop("South deck",new Vector3(13.04f,2.6f,-14.15f),336),
            Stop("Entrance",new Vector3(11.36f,2.6f,-9.64f),0),
            Stop("Living room",new Vector3(10.1f,2.6f,-5.4f),70),
            Stop("North deck",new Vector3(8.55f,2.6f,-2.65f),223),
            Stop("Undercroft",new Vector3(9.3f,0,-5.65f),115)
        };
        if(!greybox) {
            ValidateLandings(tour.Stops);
            ValidatePassage("south entrance",11.36f,-10.45f);
            ValidatePassage("north deck",11.36f,-4.49f);
            ValidateLandings(new[]{Stop("West grounds",new Vector3(-6,-.002f,-6),0),
                Stop("North grounds",new Vector3(11,-.002f,5),0), Stop("East grounds",new Vector3(29,-.002f,-6),0),
                Stop("South grounds",new Vector3(11,-.002f,-19),0)});
        }
        if(!greybox)tour.Origin.Camera.clearFlags=CameraClearFlags.Skybox;
        tour.Origin.transform.SetPositionAndRotation(tour.Stops[0].position,tour.Stops[0].rotation);
        tour.MapBounds=new Rect(-9,-21,40,30);
        tour.Plans=new[]{ MakePlan(tour.MapBounds,0,greybox),MakePlan(tour.MapBounds,1,greybox) };
        if (!greybox) {
            var wall=InstanceModel("Wall",null);
            wall.transform.position=new Vector3(7.55f,2.6f,-3.85f);
            foreach(var mesh in wall.GetComponentsInChildren<MeshRenderer>(true)) finishes.Apply(mesh,true);
            var groups=wall.GetComponentsInChildren<Transform>(true);
            tour.Full=groups.Single(g=>g.name=="Full").gameObject;
            tour.Cutaway=groups.Single(g=>g.name=="Cutaway").gameObject;
            tour.Cutaway.SetActive(false);
            // One collider covers the exhibit in all three visual states.
            var exhibit=wall.AddComponent<BoxCollider>();
            var outline=wall.GetComponentsInChildren<Renderer>(true).Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
            exhibit.center=wall.transform.InverseTransformPoint(outline.center);
            exhibit.size=outline.size;
        }
        if (!greybox) {
            var probePoints = new List<Vector3>();
            for(float x=-3;x<=27;x+=2.5f) for(float z=-16;z<=2;z+=2.5f) foreach(float y in new[]{.35f,1.7f,3.1f,4.25f,6.5f}) probePoints.Add(new Vector3(x,y,z));
            new GameObject("Baked daylight probes").AddComponent<LightProbeGroup>().probePositions=probePoints.ToArray();
            var lighting = new LightingSettings { bakedGI = true, realtimeGI = false,
                lightmapResolution = 24, lightmapMaxSize = 2048, lightmapPadding = 4, directSampleCount = 64,
                indirectSampleCount = 256, environmentSampleCount = 128, maxBounces = 4,
                ao=true, aoMaxDistance=.55f, aoExponentIndirect=.28f, aoExponentDirect=0 };
            Lightmapping.lightingSettings=lighting;
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),ScenePath);
            if (!Lightmapping.Bake() || LightmapSettings.lightmaps.Length == 0 || LightmapSettings.lightProbes == null || LightmapSettings.lightProbes.count == 0) throw new BuildFailedException("Daylight bake failed.");
        }
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), greybox ? GreyboxScenePath : ScenePath);
        if (!greybox)
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureAndroid()
    {
        var target = NamedBuildTarget.Android;
        const string settingsKey = XRGeneralSettings.settingsKey;
        if (!EditorBuildSettings.TryGetConfigObject(settingsKey, out XRGeneralSettingsPerBuildTarget perTarget))
        {
            perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(perTarget, "Assets/XR/XRGeneralSettings.asset");
            EditorBuildSettings.AddConfigObject(settingsKey, perTarget, true);
        }
        if (!perTarget.HasSettingsForBuildTarget(BuildTargetGroup.Android))
            perTarget.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
        var settings = perTarget.SettingsForBuildTarget(BuildTargetGroup.Android);
        if (settings.Manager == null)
            perTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);

        settings.InitManagerOnStart = true;
        const string loader = "UnityEngine.XR.OpenXR.OpenXRLoader";
        if (!settings.Manager.activeLoaders.Any(item => item.GetType().FullName == loader)
            && !XRPackageMetadataStore.AssignLoader(settings.Manager, loader, BuildTargetGroup.Android))
            throw new BuildFailedException("Could not configure the Android OpenXR loader.");

        var xr = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        if (xr == null)
            throw new BuildFailedException("Android OpenXR settings were not created.");
        xr.latencyOptimization = OpenXRSettings.LatencyOptimization.PrioritizeInputPolling;
        EditorUtility.SetDirty(xr);
        foreach (var feature in xr.GetFeatures<OpenXRFeature>())
        {
            string type = feature.GetType().Name;
            if(feature is AutomaticDynamicResolutionFeature){
                feature.enabled=true;
                var resolution=new SerializedObject(feature);
                resolution.FindProperty("m_MinResolutionScalar").floatValue=.85f;
                resolution.FindProperty("m_MaxResolutionScalar").floatValue=1.20f;
                resolution.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(feature);
            }
            if (type == "OculusTouchControllerProfile" || type == "MetaQuestTouchPlusControllerProfile"
                || type == "MetaQuestFeature")
            {
                feature.enabled = true;
                EditorUtility.SetDirty(feature);
            }
        }

        PlayerSettings.runInBackground = true;
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/HouseIcon.png");
        if(icon==null) throw new BuildFailedException("Launcher icon missing");
        var foreground=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/LogoForeground.png");
        var background=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/LogoBackground.png");
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown,new[]{icon});
        var kind=UnityEditor.Android.AndroidPlatformIconKind.Adaptive;
        var slots=PlayerSettings.GetPlatformIcons(target,kind);
        foreach(var slot in slots)slot.SetTextures(background,foreground);
        PlayerSettings.SetPlatformIcons(target,kind,slots);
        PlayerSettings.colorSpace=ColorSpace.Linear;
        PlayerSettings.bundleVersion = "1.3.0";
        PlayerSettings.Android.bundleVersionCode = 5;
        PlayerSettings.productName = "Platypus House VR";
        PlayerSettings.SetApplicationIdentifier(target, "com.platypushouse.vr");
        PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureLighting(bool greybox)
    {
        Directory.CreateDirectory(Generated);
        string path = Generated + "/QuestURP.asset";
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
        if (pipeline == null)
        {
            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, Generated + "/QuestRenderer.asset");
            pipeline = UniversalRenderPipelineAsset.Create(renderer);
            pipeline.msaaSampleCount = 4;
            pipeline.shadowDistance = 0;
            AssetDatabase.CreateAsset(pipeline, path);
        }
        var urpSettings=new SerializedObject(pipeline);
        urpSettings.FindProperty("m_ReflectionProbeBoxProjection").boolValue=true;
        urpSettings.ApplyModifiedPropertiesWithoutUndo();
        pipeline.msaaSampleCount=4;
        EditorUtility.SetDirty(pipeline);
        GraphicsSettings.defaultRenderPipeline = pipeline;
        QualitySettings.renderPipeline = pipeline;
        if (!greybox)
        {
            AssetDatabase.ImportAsset("Assets/Models/House.glb", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/Models/Wall.glb", ImportAssetOptions.ForceUpdate);
        }
    }

    private static void InstallHouse(HouseSurfaces finishes)
    {
        var model = InstanceModel("House", null);
        // IfcConvert rotates Z-up geometry to glTF. glTFast flips X for Unity.
        model.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        var input = Path.Combine("Tools","HouseElements.tsv");
        var elements = File.ReadLines(input).Select(line => line.Split('\t')).ToDictionary(r=>r[0]);
        var renderers = model.GetComponentsInChildren<MeshRenderer>();
        var bounds = renderers[0].bounds;
        foreach (var mesh in renderers)
        {
            var node = mesh.transform;
            while(node!=null && !elements.ContainsKey(node.name)) node=node.parent;
            if(node!=null) {
                var entry=elements[node.name];
                if(entry[1]=="IFCDOOR" && entry[2].Contains("Aluminium Sliding Door - 3 Panel")){
                    var filter=mesh.GetComponent<MeshFilter>();
                    filter.sharedMesh=OpenSlider(filter.sharedMesh,node.name,entry[2]);
                }
                bool floor=entry[1]=="IFCSLAB" && (entry[2].StartsWith("Floor:") || entry[2].StartsWith("Foundation Slab:"));
                mesh.gameObject.layer=floor ? HouseTour.Walkable : 0;
                mesh.gameObject.name=entry[2]+" ["+node.name+"]";
                if(entry[2].StartsWith("Undercroft Rainwater Tank:")) {
                    // The probe at the centre of a closed tank is in shadow. Sample the
                    // open undercroft beside its shell to shade the narrow rolled ribs.
                    var anchor=new GameObject("Undercroft daylight sample").transform;
                    anchor.SetParent(mesh.transform,false);
                    var shell=mesh.bounds;
                    anchor.position=new Vector3(shell.center.x,shell.center.y,shell.max.z+.25f);
                    mesh.probeAnchor=anchor;
                }
            }
            finishes.Apply(mesh);
            if(mesh.sharedMaterials.All(m=>m!=null && m.renderQueue<3000)) {
                bool mapped=mesh.GetComponent<MeshFilter>().sharedMesh.HasVertexAttribute(VertexAttribute.TexCoord1);
                mesh.stitchLightmapSeams=mapped;
                mesh.receiveGI = mapped ? ReceiveGI.Lightmaps : ReceiveGI.LightProbes;
                mesh.scaleInLightmap = mapped ? 1 : 0;
                GameObjectUtility.SetStaticEditorFlags(mesh.gameObject, StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic);
            }
            bounds.Encapsulate(mesh.bounds);
            var collider = mesh.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh.GetComponent<MeshFilter>().sharedMesh;
        }
        Debug.Log($"House mesh bounds {bounds.min:F3} to {bounds.max:F3}. Elements: {renderers.Length}");
        if (Mathf.Abs(bounds.max.y - 7.035f) > 0.04f || bounds.size.x < 27f)
            throw new BuildFailedException("House extent differs from the documented full-scale model.");
        var prefabPath = Generated + "/ImportedHouse.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(model, prefabPath, InteractionMode.AutomatedAction);
    }

    // The centre leaf of each three-panel Revit slider runs on its own 18�58 mm track.
    // Translate the original glass and sash together. The outer leaves, jambs and sill stay in place.
    private static Mesh OpenSlider(Mesh source,string guid,string description)
    {
        var slide=UnityEngine.Object.Instantiate(source);
        var vertices=slide.vertices;
        float width=description.Contains("4320 x") ? 4.320f : 4.315f;
        int moved=0;
        for(int i=0;i<vertices.Length;i++){
            var v=vertices[i];
            float x=Mathf.Abs(v.x);
            if(x<width/3f-.03f || x>2f*width/3f+.03f || v.y<.017f || v.y>.059f) continue;
            vertices[i].x+=Mathf.Sign(v.x)*(width/3f-.05f);
            moved++;
        }
        if(moved==0) throw new BuildFailedException("Centre slider leaf not found: "+description);
        slide.vertices=vertices; slide.RecalculateBounds();
        string path=Generated+"/OpenSlider-"+guid+".asset";
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing==null){AssetDatabase.CreateAsset(slide,path);return slide;}
        EditorUtility.CopySerialized(slide,existing);
        UnityEngine.Object.DestroyImmediate(slide);
        return existing;
    }

    private static void ValidatePassage(string name,float x,float z)
    {
        Physics.SyncTransforms();
        var start=new Vector3(x,2.6f,z-.58f);
        if(Physics.CapsuleCast(start+Vector3.up*.34f,start+Vector3.up*1.62f,.24f,Vector3.forward,out var hit,1.16f,HouseTour.WorldMask))
            throw new BuildFailedException(name+" blocked by "+hit.collider.name);
        Debug.Log("Clear manual doorway: "+name);
    }

    private static GameObject InstanceModel(string name, Transform parent)
    {
        string path = $"Assets/Models/{name}.glb";
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null)
            throw new BuildFailedException($"Missing imported model at {path}.");
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        instance.transform.SetParent(parent, false);
        return instance;
    }

    private static HouseTour CreateRig()
    {
        var root = new GameObject("XR Origin");
        var height=new GameObject("Tour height");
        height.transform.SetParent(root.transform,false);
        var offset = new GameObject("Tracking Space");
        offset.transform.SetParent(height.transform, false);
        var head = new GameObject("Main Camera");
        head.tag = "MainCamera";
        head.transform.SetParent(offset.transform, false);
        head.transform.localPosition = Vector3.up * 1.62f;
        var camera = head.AddComponent<Camera>();
        camera.allowDynamicResolution=true;
        camera.nearClipPlane = 0.055f;
        camera.farClipPlane = 80f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.51f, 0.63f, 0.72f);
        head.AddComponent<AudioListener>();
        var pose = head.AddComponent<TrackedPoseDriver>();
        pose.positionInput = new InputActionProperty(new InputAction(binding: "<XRHMD>/centerEyePosition"));
        pose.trackingStateInput = new InputActionProperty(new InputAction(binding: "<XRHMD>/trackingState"));
        pose.rotationInput = new InputActionProperty(new InputAction(binding: "<XRHMD>/centerEyeRotation"));

        var origin = root.AddComponent<XROrigin>();
        origin.Camera = camera;
        origin.CameraYOffset=1.70f;
        origin.CameraFloorOffsetObject = offset;
        origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
        var tour = root.AddComponent<HouseTour>();
        tour.Origin = origin;
        tour.HeightAdjustment=height.transform;
        tour.Beam = new GameObject("Controller aim").AddComponent<LineRenderer>();
        tour.Beam.gameObject.transform.SetParent(offset.transform,false);
        tour.Beam.positionCount = 2;
        tour.Beam.startWidth = 0.008f;
        tour.Beam.endWidth = 0.004f;
        tour.Beam.sharedMaterial = tour.RayMaterial = Material("QuestPointer", Color.white, "Universal Render Pipeline/Unlit");
        return tour;
    }

    private static Transform Stop(string name, Vector3 position, float yaw)
    {
        var stop = new GameObject(name).transform;
        stop.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        return stop;
    }

    private static void ValidateLandings(Transform[] stops)
    {
        Physics.SyncTransforms();
        foreach (var stop in stops)
        {
            var floorProbe = stop.position + Vector3.up * 0.1f;
            if (!Physics.Raycast(floorProbe, Vector3.down, out var floor, 0.14f) || floor.normal.y < 0.9f)
                throw new BuildFailedException($"No level floor at the {stop.name} landing.");
            var lower = stop.position + Vector3.up * 0.38f;
            var upper = stop.position + Vector3.up * 1.55f;
            if (Physics.CheckCapsule(lower, upper, 0.28f))
                throw new BuildFailedException($"Geometry obstructs the {stop.name} landing.");
            if (!HouseTour.ClearFloor(stop.position)) throw new BuildFailedException($"Unsafe floor perimeter at {stop.name}");
            Debug.Log($"Landing checked: {stop.name}, floor at {floor.point.y:F3} m.");
        }
    }

    private static void Guard(Vector3 center, Vector3 size)
    {
        var face=new GameObject("Balustrade collision");
        face.transform.position=center;
        face.AddComponent<BoxCollider>().size=size;
    }

    private static Texture2D MakePlan(Rect bounds,int level,bool greybox)
    {
        const int width=384, height=198;
        var tex=new Texture2D(width,height,TextureFormat.RGB24,false);
        var pixels=new Color32[width*height];
        float floor=level==0?0:2.6f;
        var scan=new Collider[8];
        Physics.SyncTransforms();
        for(int y=0;y<height;y++)for(int x=0;x<width;x++){
            var p=new Vector3(bounds.xMin+(x+.5f)*bounds.width/width,floor,bounds.yMin+(y+.5f)*bounds.height/height);
            bool supported=Physics.Raycast(p+Vector3.up*.1f,Vector3.down,.22f,1<<HouseTour.Walkable);
            int count=Physics.OverlapBoxNonAlloc(p+Vector3.up*.85f,new Vector3(.055f,.1f,.055f),scan,Quaternion.identity,1);
            pixels[y*width+x]=count>0? new Color32(225,232,220,255) : supported?new Color32(81,125,129,255):new Color32(26,48,59,255);
        }
        tex.SetPixels32(pixels);tex.Apply();
        string path=Generated+"/"+(greybox?"Greybox":"House")+"Plan"+level+".png";
        File.WriteAllBytes(path,tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static GameObject Box(string name, Vector3 center, Vector3 size, Material material, Transform parent)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.position = center;
        box.transform.localScale = size;
        box.GetComponent<Renderer>().sharedMaterial = material;
        return box;
    }

    private static Material Material(string name, Color color, string shader = "Universal Render Pipeline/Unlit")
    {
        string path = $"{Generated}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find(shader)) { color = color };
            AssetDatabase.CreateAsset(material, path);
        }
        return material;
    }
}

