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
    private const string Generated = "Assets/Generated";

    [MenuItem("Teacher Example/Prepare Tour")]
    public static void Prepare() => ConfigureAndCreate();

    [MenuItem("Teacher Example/Build Quest APK")]
    public static void BuildQuest()
    {
        ConfigureAndCreate();
        Directory.CreateDirectory("Builds");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/TeacherPlatypusHouse.apk",
            target = BuildTarget.Android,
            options = CommandLineHas("-development") ? BuildOptions.Development : BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException(report.summary.result.ToString());
    }

    private static bool CommandLineHas(string option) => Environment.GetCommandLineArgs().Contains(option);

    private static void ConfigureAndCreate()
    {
        ConfigureAndroid();
        ConfigureLighting();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var sun=new GameObject("Sun").AddComponent<Light>();
        sun.type=LightType.Directional;
        sun.lightmapBakeType=LightmapBakeType.Baked;
        sun.shadows=LightShadows.Soft;
        HouseSurfaces.Daylight(sun);
        InstallHouse(new HouseSurfaces());
        Physics.SyncTransforms();
        var tour = CreateRig();
        tour.Stops = new[] {
            GroundStop("Stair foot",6.65f,-6.55f,270),
            Stop("South deck",new Vector3(1.33f,2.6f,-6.43f),322),
            Stop("Entrance",new Vector3(-1.55f,2.6f,-2.03f),30),
            Stop("Living room",new Vector3(1.52f,2.6f,1.75f),231),
            Stop("North deck",new Vector3(-1.36f,2.6f,4.72f),60),
            GroundStop("Undercroft",-.4f,2.24f,150)
        };
        ValidateLandings(tour.Stops);
        ValidatePassage("south entrance",-1.56f,-2.85f);
        ValidatePassage("north deck",-1.56f,3.14f);
        tour.Origin.Camera.clearFlags=CameraClearFlags.Skybox;
        tour.Origin.transform.SetPositionAndRotation(tour.Stops[0].position,tour.Stops[0].rotation);
        tour.MapBounds=new Rect(-24,-14,48,28);
        tour.Plans=new[]{MakePlan(tour.MapBounds,0),MakePlan(tour.MapBounds,1)};
        {
            var probePoints = new List<Vector3>();
            for(float x=-21;x<=21;x+=2.5f) for(float z=-11;z<=11;z+=2.5f) foreach(float y in new[]{.35f,1.7f,3.1f,4.25f,6.5f}) probePoints.Add(new Vector3(x,y,z));
            // Give each tank a measured sample in the open aisle. Interpolation from
            // the coarse grid can otherwise include probes buried inside adjacent shells.
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                if (renderer.probeAnchor != null) probePoints.Add(renderer.probeAnchor.position);
            new GameObject("Baked daylight probes").AddComponent<LightProbeGroup>().probePositions=probePoints.ToArray();
            var lighting = new LightingSettings { bakedGI = true, realtimeGI = false,
                lightmapResolution = 24, lightmapMaxSize = 2048, lightmapPadding = 4, directSampleCount = 64,
                indirectSampleCount = 256, environmentSampleCount = 128, maxBounces = 4,
                ao=true, aoMaxDistance=.55f, aoExponentIndirect=.28f, aoExponentDirect=0 };
            Lightmapping.lightingSettings=lighting;
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),ScenePath);
            if (!Lightmapping.Bake() || LightmapSettings.lightmaps.Length == 0 || LightmapSettings.lightProbes == null || LightmapSettings.lightProbes.count == 0) throw new BuildFailedException("Daylight bake failed.");
        }
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
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
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.productName = "Platypus House - Teacher VR";
        PlayerSettings.SetApplicationIdentifier(target, "com.platypushouse.teacher.vr");
        PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureLighting()
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
        AssetDatabase.ImportAsset("Assets/Models/TeacherHouse.glb", ImportAssetOptions.ForceUpdate);
    }

    private static void InstallHouse(HouseSurfaces finishes)
    {
        var model=InstanceModel("TeacherHouse",null);
        var renderers=model.GetComponentsInChildren<MeshRenderer>();
        if(renderers.Length<500)throw new BuildFailedException("Incomplete teacher model export");
        foreach(var mesh in renderers) {
            string category=mesh.name.Split(new[]{"__"},StringSplitOptions.None)[0];
            bool walkable=category=="Floors" || category=="Toposolid" || category=="Sub-divisions" || category=="Stairs";
            mesh.gameObject.layer=walkable ? HouseTour.Walkable : 0;
            if(category=="Toposolid")mesh.transform.position+=Vector3.down*.002f;
            finishes.Apply(mesh);
            var geometry=mesh.GetComponent<MeshFilter>().sharedMesh;
            bool mapped=geometry.HasVertexAttribute(VertexAttribute.TexCoord1);
            mesh.receiveGI=mapped ? ReceiveGI.Lightmaps : ReceiveGI.LightProbes;
            mesh.scaleInLightmap=mapped ? (category=="Toposolid"?.45f:1f) : 0;
            if(mesh.sharedMaterials.All(m=>m.renderQueue<3000))
                GameObjectUtility.SetStaticEditorFlags(mesh.gameObject,StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic);
            if(mesh.sharedMaterials.Any(m=>m.name=="Tank")) {
                var sample=new GameObject("Tank daylight sample").transform;
                sample.SetParent(mesh.transform,false);
                var box=mesh.bounds;
                sample.position=new Vector3(box.center.x,box.center.y,box.max.z+.35f);
                mesh.probeAnchor=sample;
            }
            mesh.gameObject.AddComponent<MeshCollider>().sharedMesh=geometry;
        }
        Debug.Log($"Teacher model loaded: {renderers.Length} renderers");
    }

    private static Transform GroundStop(string name,float x,float z,float yaw)
    {
        if(!Physics.Raycast(new Vector3(x,1,z),Vector3.down,out var hit,2.5f,1<<HouseTour.Walkable))
            throw new BuildFailedException("No teacher site surface at "+name);
        return Stop(name,hit.point,yaw);
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

    private static Texture2D MakePlan(Rect bounds,int level)
    {
        const int width=384, height=198;
        var tex=new Texture2D(width,height,TextureFormat.RGB24,false);
        var pixels=new Color32[width*height];
        float floor=level==0?0:2.6f;
        var scan=new Collider[8];
        Physics.SyncTransforms();
        for(int y=0;y<height;y++)for(int x=0;x<width;x++){
            var p=new Vector3(bounds.xMin+(x+.5f)*bounds.width/width,floor,bounds.yMin+(y+.5f)*bounds.height/height);
            bool supported=Physics.Raycast(p+Vector3.up*(level==0?.8f:.1f),Vector3.down,level==0?1.9f:.22f,1<<HouseTour.Walkable);
            int count=Physics.OverlapBoxNonAlloc(p+Vector3.up*.85f,new Vector3(.055f,.1f,.055f),scan,Quaternion.identity,1);
            pixels[y*width+x]=count>0? new Color32(225,232,220,255) : supported?new Color32(81,125,129,255):new Color32(26,48,59,255);
        }
        tex.SetPixels32(pixels);tex.Apply();
        string path=Generated+"/"+"TeacherPlan"+level+".png";
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
