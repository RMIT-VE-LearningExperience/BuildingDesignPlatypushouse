using System;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

public sealed class HouseTour : MonoBehaviour
{
    public XROrigin Origin;
    public LineRenderer Beam;
    public Transform[] Stops;
    public Transform[] StairLandings;
    public GameObject Full, Cutaway;
    public Texture2D[] Plans;
    public Rect MapBounds;
    public Material RayMaterial;
    public const int Walkable = 8;
    public const int WorldMask = ~(1 << TourPanel.Layer | 1 << 2);
    const float Radius = .24f;

    sealed class Hand : IDisposable
    {
        readonly InputAction aim, trigger, primary, stick;
        public Hand(string side)
        {
            string root = "<XRController>{" + side + "Hand}/";
            aim = Action(root+"aimPose",InputActionType.Value);
            trigger = Action(root+"triggerPressed",InputActionType.Button);
            primary = Action(root+"primaryButton",InputActionType.Button);
            stick = Action(root+"thumbstick",InputActionType.Value);
        }
        static InputAction Action(string path, InputActionType type)
        {
            var a = new InputAction(type:type,binding:path); a.Enable(); return a;
        }
        public PoseState Pose => aim.ReadValue<PoseState>();
        public bool Select => trigger.WasPressedThisFrame();
        public bool Places => primary.WasPressedThisFrame();
        public Vector2 Axis => stick.ReadValue<Vector2>();
        public void Dispose() { aim.Dispose(); trigger.Dispose(); primary.Dispose(); stick.Dispose(); }
    }

    Hand left,right,active,teleportHand;
    TourPanel menu,wall;
    readonly TourPanel[] stairSigns = new TourPanel[2];
    Transform head;
    TextMeshProUGUI playerMarker;
    LineRenderer ring;
    CanvasGroup shade;
    int wallState, mapLevel;
    bool stickLatched, vignette, started;
    float fade;
    static readonly string[] States = {"Finished","Cutaway","Framing"};
    static readonly string[] Notes = {
        "Steel cladding · profile provisional",
        "20 mm cladding zone / 90 mm frame / 10 mm lining",
        "90 mm timber studs and plates"
    };
    readonly Vector3[] arc = new Vector3[49];

    void OnEnable() { left = new Hand("Left"); right = new Hand("Right"); active = right; }
    void OnDisable() { left?.Dispose(); right?.Dispose(); teleportHand = null; }
    void Start()
    {
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
        for(int i=0;i<StairLandings.Length;i++){
            int destination=1-i;
            var here=StairLandings[i];
            var sign=TourPanel.Create(i==0?"Stair foot":"Deck landing",new Vector2(640,200));
            stairSigns[i]=sign;
            sign.transform.position=here.position + new Vector3(i==0?-.4f:.4f,1.35f,.48f);
            sign.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(sign.transform.position-here.position,Vector3.up));
            sign.Text("South stair",0,55,570,65,35);
            sign.Button(i==0?"Go up to south deck":"Go down to ground",0,-40,570,()=>Move(StairLandings[destination],true));
        }
        if(Full!=null) {
            wall=TourPanel.Create("North deck wall exhibit",new Vector2(1120,405));
            wall.transform.position=new Vector3(7.55f,3.95f,-3.18f);
            wall.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(wall.transform.position-Stops[1].position,Vector3.up));
            DrawWall();
        }
    }

    void DrawWall()
    {
        wall.Clear();
        Full.SetActive(wallState==0); Cutaway.SetActive(wallState==1);
        wall.Text("North deck  /  Wall construction",0,141,1030,63,43);
        for(int i=0;i<3;i++){
            int index=i;
            wall.Button((i==wallState?"• ":"")+States[i],(i-1)*340,45,322,()=>{wallState=index;DrawWall();},i==wallState);
        }
        wall.Text(Notes[wallState],0,-78,1030,100,36);
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
    }

    void Welcome()
    {
        PlacePanel(); menu.Clear();
        menu.Text("Platypus House",0,242,1050,95,69);
        menu.Text("You are in the undercroft",0,139,1050,78,47);
        menu.Text("Push the stick forward, aim at the floor and release to move.\nPoint and squeeze the trigger to select.\nPress A or X for the Places map.",0,-35,1050,218,38);
        menu.Button("Explore the house",0,-250,560,CloseMenu);
    }

    void CloseMenu(){ menu.gameObject.SetActive(false); playerMarker=null; }

    void Places(int level)
    {
        menu.Clear(); mapLevel=level;
        menu.Text("Places", -260,291,535,88,58);
        menu.Button("Close",405,283,220,CloseMenu);
        menu.Button(level==0?"• Ground":"Ground",-380,181,254,()=>Places(0),level==0);
        menu.Button(level==1?"• First floor":"First floor",-98,181,282,()=>Places(1),level==1);
        var panelMap=menu.Picture(Plans[level],new Vector2(-237,-24),new Vector2(560,289));
        for(int i=0;i<Stops.Length;i++){
            if((i==0?0:1)!=level)continue;
            var location=Stops[i].position;
            float x=(Mathf.InverseLerp(MapBounds.xMin,MapBounds.xMax,location.x)-.5f)*560;
            float y=(Mathf.InverseLerp(MapBounds.yMin,MapBounds.yMax,location.z)-.5f)*289;
            menu.Text((i+1).ToString(),x,y,31,31,25,true,panelMap);
        }
        playerMarker=menu.Text("▲",0,0,46,46,37,true,panelMap);
        playerMarker.color=new Color32(255,202,70,255);
        menu.Text(level==CurrentLevel()?"▲  You are here":"You are on the other level",-237,-202,560,50,29,true);
        for(int i=0;i<Stops.Length;i++){ int stop=i; menu.Button((i+1)+"   "+Stops[i].name,322,99-i*105,394,()=>Move(Stops[stop],true)); }
        menu.Button(vignette?"Turn shade: on":"Turn shade: off",-298,-285,430,()=>{vignette=!vignette;Places(level);},vignette);
        menu.Button("Recenter view",233,-285,422,Recenter);
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
        playerMarker.rectTransform.anchoredPosition=new Vector2((u-.5f)*560,(v-.5f)*289);
        playerMarker.rectTransform.localRotation=Quaternion.Euler(0,0,-head.eulerAngles.y);
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
        if(!started && head.localPosition.sqrMagnitude>.01f){started=true; Welcome();}
        if(!started)return;
        fade=Mathf.MoveTowards(fade,0,Time.unscaledDeltaTime*4); shade.alpha=fade;
        UpdateMarker();
        if(left.Select || left.Places || left.Axis.sqrMagnitude>.4f) active=left;
        else if(right.Select || right.Places || right.Axis.sqrMagnitude>.4f) active=right;
        if(teleportHand!=null)active=teleportHand;
        var pose=active.Pose;
        if(!pose.isTracked && teleportHand==null){active=active==right?left:right;pose=active.Pose;}
        if(!pose.isTracked){Beam.enabled=ring.enabled=false;teleportHand=null;menu.Point(null);wall?.Point(null);return;}
        var tracking=Origin.CameraFloorOffsetObject.transform;
        var ray=new Ray(tracking.TransformPoint(pose.position),tracking.rotation*pose.rotation*Vector3.forward);
        if(active.Places){teleportHand=null;if(menu.gameObject.activeSelf && playerMarker!=null)CloseMenu();else{PlacePanel();Places(CurrentLevel());}}
        Vector2 stick=active.Axis;
        if(!menu.gameObject.activeSelf && teleportHand==null && !stickLatched && stick.y>.7f)teleportHand=active;
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
        bool onWall=wall!=null && wall.Point(collider);
        TourPanel stair=null;
        foreach(var sign in stairSigns) if(sign!=null && sign.Point(collider)) stair=sign;
        Color color=onMenu||onWall||stair!=null?Color.cyan:Color.white;
        Beam.enabled=true; Beam.positionCount=2; Beam.SetPosition(0,ray.origin);Beam.SetPosition(1,ray.GetPoint(hit?target.distance:4));
        Beam.startColor=Beam.endColor=color;
        ring.enabled=hit;
        if(hit) Ring(target.point+target.normal*.004f,target.normal,Mathf.Clamp(target.distance*.009f,.009f,.032f),color);
        if(active.Select){if(onMenu)menu.Press(collider);else if(onWall)wall.Press(collider);else stair?.Press(collider);}
    }

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
        Beam.startColor=Beam.endColor=valid?Color.cyan:Color.gray;
        menu.Point(null); wall?.Point(null); return valid;
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

    void Ring(Vector3 p,Vector3 normal,float radius,Color color)
    {
        var rot=Quaternion.FromToRotation(Vector3.up,normal);
        ring.enabled=true;ring.startColor=ring.endColor=color;
        for(int i=0;i<32;i++){
            float t=i*Mathf.PI/16;
            ring.SetPosition(i,p+rot*new Vector3(Mathf.Cos(t)*radius,0,Mathf.Sin(t)*radius));
        }
    }
}