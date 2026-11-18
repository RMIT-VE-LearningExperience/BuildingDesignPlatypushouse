using System;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR;

public sealed class HouseTour : MonoBehaviour
{
    public XROrigin Origin;
    public LineRenderer Beam;
    public Transform[] Stops;
    public Transform HeightAdjustment;

    public Texture2D[] Plans;
    public Rect MapBounds;
    public Material RayMaterial;
    public const int Walkable = 8;
    public const int WorldMask = ~(1 << TourPanel.Layer | 1 << 2);
    const float Radius = .24f;

    sealed class Hand
    {
        readonly XRNode node;
        XRController controller;
        UnityEngine.XR.InputDevice native;
        Vector3Control aimPosition;
        QuaternionControl aimRotation;
        ButtonControl trigger,primary,aimTracked;
        Vector2Control stick;
        bool triggerHeld, primaryHeld;
        public bool Select { get; private set; }
        public bool Places { get; private set; }
        public Vector2 Axis { get; private set; }
        static readonly InputFeatureUsage<Vector3> NativePointerPosition = new("PointerPosition");
        static readonly InputFeatureUsage<Quaternion> NativePointerRotation = new("PointerRotation");

        public Hand(XRNode node) { this.node=node; }

        public void Sample()
        {
            var current = node==XRNode.LeftHand ? XRController.leftHand : XRController.rightHand;
            if(current!=controller){
                controller=current;
                aimPosition=current?.TryGetChildControl<Vector3Control>("pointerPosition");
                aimRotation=current?.TryGetChildControl<QuaternionControl>("pointerRotation");
                aimTracked=current?.TryGetChildControl<ButtonControl>("pointer/isTracked");
                trigger=current?.TryGetChildControl<ButtonControl>("triggerPressed");
                primary=current?.TryGetChildControl<ButtonControl>("primaryButton");
                stick=current?.TryGetChildControl<Vector2Control>("thumbstick");
            }
            if(!native.isValid) native=InputDevices.GetDeviceAtXRNode(node);
            bool pressed=trigger?.isPressed ?? false;
            bool open=primary?.isPressed ?? false;
            if(trigger==null) native.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton,out pressed);
            if(primary==null) native.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton,out open);
            Vector2 axis=default;
            if(stick!=null) axis=stick.ReadValue(); else native.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis,out axis);
            Select=pressed && !triggerHeld; Places=open && !primaryHeld; Axis=axis;
            triggerHeld=pressed; primaryHeld=open;
        }

        public bool TryRay(Transform space, out Ray ray)
        {
            Vector3 position=aimPosition?.ReadValue() ?? default;
            Quaternion rotation=aimRotation?.ReadValue() ?? default;
            bool valid=controller!=null && (controller.isTracked.isPressed || (aimTracked?.isPressed ?? false)) && ValidPose(position,rotation);
            if (!valid) {
                var device = native;
                valid = device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out bool tracked) && tracked &&
                        device.TryGetFeatureValue(NativePointerPosition, out position) &&
                        device.TryGetFeatureValue(NativePointerRotation, out rotation) && ValidPose(position, rotation);
            }
            ray = valid ? new Ray(space.TransformPoint(position),space.rotation*rotation*Vector3.forward) : default;
            return valid;
        }
        static bool ValidPose(Vector3 p,Quaternion q) =>
            float.IsFinite(p.sqrMagnitude) && p.sqrMagnitude > .000001f && Mathf.Abs(Quaternion.Dot(q,q)-1f)<.05f;
    }

    Hand left,right,active,teleportHand;
    TourPanel menu;
    readonly TourPanel[] routeSigns = new TourPanel[4];
    XRDisplaySubsystem display;
    bool displayFocused=true, applicationFocused=true, paused, consumeResumeInput;
    float launchTime;
    Transform head;
    TextMeshProUGUI playerMarker;
    LineRenderer ring;
    CanvasGroup shade;
    int mapLevel;
    bool stickLatched, vignette, started;
    float fade, dwellTime, lookDownTime;
    Collider dwellTarget;
    bool gazeMode, lookDownLatched;
    static readonly Vector2 MapSize=new Vector2(416,312);
    const float DwellSeconds = 1.1f;
    MaterialPropertyBlock pointerColors;
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    readonly Vector3[] arc = new Vector3[49];

    void OnEnable() { left = new Hand(XRNode.LeftHand); right = new Hand(XRNode.RightHand); active = right; }
    void OnDisable() {
        teleportHand = null;
        if(display!=null) display.displayFocusChanged-=OnDisplayFocus;
        display=null;
    }
    void ObserveDisplay(){
        if(display!=null) return;
        var displays=new System.Collections.Generic.List<XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(displays);
        display=displays.Find(d=>d.running);
        if(display!=null) display.displayFocusChanged+=OnDisplayFocus;
    }
    void OnDisplayFocus(bool focused){displayFocused=focused;ResetInput();}
    void OnApplicationFocus(bool focused){applicationFocused=focused;ResetInput();}
    void OnApplicationPause(bool isPaused){paused=isPaused;ResetInput();}
    void ResetInput(){
        teleportHand=null; dwellTarget=null; dwellTime=lookDownTime=fade=0;
        stickLatched=lookDownLatched=consumeResumeInput=true;
        if(Beam!=null) Beam.enabled=false;
        if(ring!=null) ring.enabled=false;
        if(shade!=null) shade.alpha=0;
    }
    void Start()
    {
        launchTime=Time.realtimeSinceStartup;
        ObserveDisplay();
        pointerColors=new MaterialPropertyBlock();
        Application.targetFrameRate=72;
        head=Origin.Camera.transform;
        menu=TourPanel.Create("Places",new Vector2(1150,740));
        ring=new GameObject("Aim reticle").AddComponent<LineRenderer>();
        ring.sharedMaterial=RayMaterial;
        ring.startWidth=ring.endWidth=.007f; ring.loop=true; ring.positionCount=32;
        var overlay=new GameObject("Comfort fade",typeof(Canvas),typeof(CanvasGroup));
        var canvas=overlay.GetComponent<Canvas>();
        canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=Origin.Camera; canvas.planeDistance=.07f;
        var mask=new GameObject("Vignette",typeof(RectTransform),typeof(UnityEngine.UI.RawImage));
        var rect=(RectTransform)mask.transform; rect.SetParent(overlay.transform,false);
        rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
        var pixels=new Texture2D(64,64,TextureFormat.RGBA32,false);
        for(int y=0;y<64;y++) for(int x=0;x<64;x++) {
            float r=new Vector2(x-31.5f,y-31.5f).magnitude/31.5f;
            pixels.SetPixel(x,y,new Color(0,0,0,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,1,r))));
        }
        pixels.Apply(); mask.GetComponent<UnityEngine.UI.RawImage>().texture=pixels;
        shade=overlay.GetComponent<CanvasGroup>(); shade.alpha=0;
        menu.gameObject.SetActive(false);
        string[] next={"Go up to south deck","Enter the house","Explore living room","Visit north deck"};
        for(int i=0;i<routeSigns.Length;i++){
            int destination=i+1;
            var here=Stops[i];
            var sign=TourPanel.Create("Wayfinder - "+here.name,new Vector2(720,222));
            routeSigns[i]=sign;
            var direction=here.forward*1.05f+here.right*.42f;
            sign.transform.SetPositionAndRotation(here.position+direction+Vector3.up*1.42f,Quaternion.LookRotation(direction));
            sign.Text(here.name,0,58,640,68,38);
            sign.Button(next[i],0,-45,646,()=>Move(Stops[destination],true));
        }
    }

    void ShowNearbySign()
    {
        int nearest=-1;
        float closest=2.5f*2.5f;
        if(!menu.gameObject.activeSelf){
            var feet=new Vector3(head.position.x,Origin.transform.position.y,head.position.z);
            for(int i=0;i<Stops.Length;i++){
                var delta=Stops[i].position-feet;
                if(Mathf.Abs(delta.y)>.4f || delta.sqrMagnitude>=closest)continue;
                closest=delta.sqrMagnitude; nearest=i;
            }
        }
        for(int i=0;i<routeSigns.Length;i++){
            bool visible=i==nearest;
            if(routeSigns[i].gameObject.activeSelf!=visible)routeSigns[i].gameObject.SetActive(visible);
        }
    }

    void PlacePanel()
    {
        var f=Vector3.ProjectOnPlane(head.forward,Vector3.up).normalized;
        if(f.sqrMagnitude<.5f) f=Origin.transform.forward;
        // Avoid placing the menu behind the wall when it is summoned in a tight room.
        float distance=1.48f;
        if(Physics.SphereCast(head.position,.14f,f,out var obstruction,distance,WorldMask))
            distance=Mathf.Max(.48f,obstruction.distance-.09f);
        menu.transform.localScale=Vector3.one*(.001f*distance/1.48f);
        menu.transform.SetPositionAndRotation(head.position+f*distance-Vector3.up*.07f,Quaternion.LookRotation(f));
        menu.gameObject.SetActive(true);
        ShowNearbySign();
    }

    void Welcome()
    {
        PlacePanel(); menu.Clear();
        menu.Text("Platypus House  /  Teacher model",0,284,1050,80,57);
        menu.Text("Built by Digital Media Design Team - COVE RMIT",0,201,1050,55,34);
        menu.Text("Start at the south stair",0,102,1050,74,48);
        menu.Text("Follow the signs through the open door. Push the stick forward and release to move. A or X opens Places.\nWithout a controller, look at a button and hold.",0,-38,1050,182,36);
        menu.Button("Go up to south deck",-233,-239,558,()=>Move(Stops[1],true));
        menu.Button("Explore from here",300,-239,445,CloseMenu);
    }

    void CloseMenu(){ menu.gameObject.SetActive(false); playerMarker=null; }

    void Places(int level)
    {
        menu.Clear(); mapLevel=level;
        menu.Text("Places", -260,291,535,88,58);
        menu.Button("Close",405,283,220,CloseMenu);
        menu.Button(level==0?"• Ground":"Ground",-380,181,254,()=>Places(0),level==0);
        menu.Button(level==1?"• First floor":"First floor",-98,181,282,()=>Places(1),level==1);
        var panelMap=menu.Picture(Plans[level],new Vector2(-237,-24),MapSize);
        for(int i=0;i<Stops.Length;i++){
            if((Stops[i].position.y>1.3f?1:0)!=level)continue;
            var location=Stops[i].position;
            float x=(Mathf.InverseLerp(MapBounds.xMin,MapBounds.xMax,location.x)-.5f)*MapSize.x;
            float y=(Mathf.InverseLerp(MapBounds.yMin,MapBounds.yMax,location.z)-.5f)*MapSize.y;
            menu.Text((i+1).ToString(),x,y,31,31,25,true,panelMap);
        }
        playerMarker=menu.Text("▲",0,0,46,46,37,true,panelMap);
        playerMarker.color=new Color32(255,202,70,255);
        menu.Text(level==CurrentLevel()?"▲  You are here":"You are on the other level",-237,-202,560,50,29,true);
        int row=0;
        for(int i=0;i<Stops.Length;i++){
            if((Stops[i].position.y>1.3f?1:0)!=level)continue;
            int stop=i;
            menu.Button((i+1)+"   "+Stops[i].name,322,142-row++*94,394,()=>Move(Stops[stop],true));
        }
        menu.Button(vignette?"Shade on":"Shade off",-395,-285,280,()=>{vignette=!vignette;Places(level);},vignette);
        menu.Button("Recenter", -72,-285,268,Recenter);
        bool adjusted=Mathf.Abs(HeightAdjustment.localPosition.y)>.005f;
        menu.Button(adjusted?"Use tracked height":"Set eye to 1.70 m",288,-285,406,()=>{
            if(adjusted) HeightAdjustment.localPosition=Vector3.zero;
            else SetTourHeight();
            PlacePanel(); Places(level);
        });
        UpdateMarker();
    }

    int CurrentLevel()=>Origin.transform.position.y>1.3f?1:0;
    void UpdateMarker()
    {
        if(playerMarker==null)return;
        bool same=mapLevel==CurrentLevel();
        playerMarker.gameObject.SetActive(same);
        if(!same)return;
        float u=Mathf.InverseLerp(MapBounds.xMin,MapBounds.xMax,head.position.x);
        float v=Mathf.InverseLerp(MapBounds.yMin,MapBounds.yMax,head.position.z);
        playerMarker.rectTransform.anchoredPosition=new Vector2((u-.5f)*MapSize.x,(v-.5f)*MapSize.y);
        playerMarker.rectTransform.localRotation=Quaternion.Euler(0,0,-head.eulerAngles.y);
    }

    void SetTourHeight(){
        float tracked=head.position.y-Origin.transform.position.y-HeightAdjustment.localPosition.y;
        HeightAdjustment.localPosition=Vector3.up*Mathf.Clamp(1.70f-tracked,-.5f,1.2f);
    }

    void Recenter()
    {
        var systems=new System.Collections.Generic.List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(systems);
        foreach(var system in systems) if(system.running) system.TryRecenter();
        // Stage-space runtimes may decline TryRecenter. The panel still resets to current eye level.
        PlacePanel();
    }

    void Move(Transform stop,bool turn)
    {
        if(!ClearFloor(stop.position)) return;
        if(turn) Origin.MatchOriginUpCameraForward(Vector3.up,stop.forward);
        Land(stop.position); CloseMenu();
    }

    void Land(Vector3 p)
    {
        float eye=head.position.y-Origin.transform.position.y;
        Origin.MoveCameraToWorldLocation(p+Vector3.up*eye);
        Origin.transform.position += Vector3.up*(p.y-Origin.transform.position.y);
    }

    public static bool ClearFloor(Vector3 p)
    {
        for(int i=0;i<5;i++){
            var offset=i==0?Vector3.zero:Quaternion.Euler(0,(i-1)*90,0)*Vector3.forward*Radius;
            if(!Physics.Raycast(p+offset+Vector3.up*.11f,Vector3.down,out var h,.22f,WorldMask) ||
                h.collider.gameObject.layer!=Walkable || h.normal.y<.9f)return false;
        }
        return !Physics.CheckCapsule(p+Vector3.up*(Radius+.06f),p+Vector3.up*1.58f,Radius,WorldMask);
    }

    void Update()
    {
        if(!applicationFocused || !displayFocused || paused)return;
        if(!started){
            bool tracking=InputDevices.GetDeviceAtXRNode(XRNode.Head).TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked,out var tracked) && tracked;
            if(Application.platform==RuntimePlatform.Android && !tracking && Time.realtimeSinceStartup-launchTime<2.5f)return;
            ObserveDisplay(); started=true;
            if(head.position.y-Origin.transform.position.y<1.65f)SetTourHeight();
            Move(Stops[0],true); Welcome();
        }
        fade=Mathf.MoveTowards(fade,0,Time.unscaledDeltaTime*4); shade.alpha=fade;
        ShowNearbySign(); UpdateMarker();
        left.Sample(); right.Sample();
        if(consumeResumeInput){consumeResumeInput=false;return;}
        if(left.Select || left.Places || left.Axis.sqrMagnitude>.4f) active=left;
        else if(right.Select || right.Places || right.Axis.sqrMagnitude>.4f) active=right;
        if(teleportHand!=null)active=teleportHand;
        var trackingSpace=Origin.CameraFloorOffsetObject.transform;
        bool controller=active.TryRay(trackingSpace,out var ray);
        var other=active==right ? left : right;
        if(teleportHand==null && other.TryRay(trackingSpace,out var otherRay) &&
           (!controller || (!active.Select && !active.Places && active.Axis.sqrMagnitude<.1f &&
            !AimsAtButton(ray) && AimsAtButton(otherRay)))){
            active=other; ray=otherRay; controller=true;
        }
        gazeMode=!controller;
        if(gazeMode){
            teleportHand=null;
            ray=new Ray(head.position,head.forward);
            bool down=head.forward.y<-.75f;
            if(!down){lookDownLatched=false;lookDownTime=0;}
            else if(!menu.gameObject.activeSelf && !lookDownLatched){
                lookDownTime+=Time.unscaledDeltaTime;
                if(lookDownTime>=DwellSeconds){
                    lookDownLatched=true; PlacePanel(); Places(CurrentLevel());
                }
            }
        }else{lookDownTime=0;lookDownLatched=false;dwellTime=0;dwellTarget=null;}
        if(active.Places){teleportHand=null;if(menu.gameObject.activeSelf && playerMarker!=null)CloseMenu();else{PlacePanel();Places(CurrentLevel());}}
        Vector2 stick=active.Axis;
        if(controller && !menu.gameObject.activeSelf && teleportHand==null && !stickLatched && stick.y>.7f)teleportHand=active;
        if(teleportHand!=null){
            bool ok=TraceArc(ray,out var point);
            if(stick.y<.35f){
                if(ok && stick.y>-.4f)Land(point);
                teleportHand=null; stickLatched=true;
            }
            return;
        }
        if(!stickLatched && Mathf.Abs(stick.x)>.75f){
            Origin.RotateAroundCameraUsingOriginUp(30*Mathf.Sign(stick.x)); stickLatched=true;
            if(vignette)fade=.82f;
        }
        if(stick.sqrMagnitude<.1f)stickLatched=false;
        bool hit=Physics.Raycast(ray,out var target,9f,~(1<<2));
        var collider=hit?target.collider:null;
        bool onMenu=menu.gameObject.activeSelf && menu.Point(collider);

        TourPanel stair=null;
        foreach(var sign in routeSigns) if(sign!=null && sign.Point(collider)) stair=sign;
        Color color=onMenu||stair!=null?Color.cyan:Color.white;
        Beam.enabled=controller; Beam.positionCount=2; Beam.SetPosition(0,ray.origin);Beam.SetPosition(1,ray.GetPoint(hit?target.distance:4));
        PaintPointer(Beam,color);
        bool button=onMenu || stair!=null;
        float radius=Mathf.Clamp((hit ? target.distance : 1.5f)*.009f,.009f,.032f);
        bool dwellClick=false;
        if(gazeMode){
            var hovered=button ? collider : null;
            if(hovered!=dwellTarget){dwellTarget=hovered;dwellTime=0;}
            if(hovered!=null && dwellTime<DwellSeconds){
                dwellTime+=Time.unscaledDeltaTime;
                dwellClick=dwellTime>=DwellSeconds;
            }
            radius*=1f+.65f*Mathf.Clamp01(dwellTime/DwellSeconds);
        }
        if(hit) Ring(target.point+target.normal*.008f,target.normal,radius,color);
        else Ring(ray.GetPoint(1.5f),-ray.direction,radius,color);
        if(active.Select || dwellClick){
            if(onMenu)menu.Press(collider);else stair?.Press(collider);
        }
    }

    static bool AimsAtButton(Ray ray) =>
        Physics.Raycast(ray,out var hit,9f,~(1<<2)) && hit.collider.gameObject.layer==TourPanel.Layer;

    bool TraceArc(Ray ray,out Vector3 point)
    {
        point=ray.origin; arc[0]=point;
        var v=ray.direction*6.5f;
        bool valid=false; int count=1; ring.enabled=false;
        for(int i=1;i<arc.Length;i++){
            float t=i*.045f;
            var next=ray.origin+v*t+Vector3.down*(4.9f*t*t);
            if(Physics.Linecast(point,next,out var h,WorldMask)){
                point=h.point; arc[count++]=point;
                valid=h.normal.y>.9f && h.collider.gameObject.layer==Walkable && ClearFloor(point);
                if(valid && !HasFloorPath(head.position,point))valid=false;
                if(valid && Mathf.Abs(point.y-Origin.transform.position.y)>.45f) valid=false;
                if(valid && Physics.Linecast(head.position,point+Vector3.up*.55f,out _,WorldMask)) valid=false;
                Ring(point+h.normal*.012f,h.normal,.17f,valid?Color.cyan:new Color(1,.35f,.15f));
                break;
            }
            point=next; arc[count++]=point;
        }
        Beam.enabled=true; Beam.positionCount=count;
        for(int i=0;i<count;i++)Beam.SetPosition(i,arc[i]);
        PaintPointer(Beam,valid?Color.cyan:Color.gray);
        menu.Point(null);  return valid;
    }

    static bool HasFloorPath(Vector3 start,Vector3 end)
    {
        var from=new Vector3(start.x,end.y,start.z);
        int steps=Mathf.CeilToInt(Vector3.Distance(from,end)/.35f);
        for(int i=1;i<steps;i++){
            var p=Vector3.Lerp(from,end,(float)i/steps);
            if(!Physics.Raycast(p+Vector3.up*.1f,Vector3.down,.22f,1<<Walkable))return false;
        }
        return true;
    }

    void PaintPointer(LineRenderer line,Color color)
    {
        pointerColors.SetColor(BaseColor,color);
        line.SetPropertyBlock(pointerColors);
    }

    void Ring(Vector3 p,Vector3 normal,float radius,Color color)
    {
        var rot=Quaternion.FromToRotation(Vector3.up,normal);
        ring.enabled=true;PaintPointer(ring,color);
        for(int i=0;i<32;i++){
            float t=i*Mathf.PI/16;
            ring.SetPosition(i,p+rot*new Vector3(Mathf.Cos(t)*radius,0,Mathf.Sin(t)*radius));
        }
    }
}