using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Controls;
using Touch = UnityEngine.XR.OpenXR.Features.Interactions.MetaQuestTouchPlusControllerProfile.QuestTouchPlusController;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class TeacherTourCheck {
    const string Key="TeacherTourCheck";
    static TeacherTourCheck(){EditorApplication.update+=Tick;}
    public static void Begin(){
        EditorSceneManager.OpenScene("Assets/HouseTour.unity");
        SessionState.SetBool(Key,true);
        EditorApplication.isPlaying=true;
    }
    static object Run(HouseTour t,string name,params object[] args) =>
        typeof(HouseTour).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(t,args);
    static void Check(bool v,string what){if(!v)throw new Exception(what);}
    static void Click(TourPanel menu,string text){
        var b=menu.GetComponentsInChildren<BoxCollider>().Single(x=>x.name==text);
        Physics.SyncTransforms();
        var ray=new Ray(b.transform.position-b.transform.forward*0.25f,b.transform.forward);
        Check(Physics.Raycast(ray,out var h,.3f) && h.collider==b && menu.Point(h.collider),"Ray misses "+text);
        menu.Press(b);
    }
    static void Labels(TourPanel p){
        Canvas.ForceUpdateCanvases();
        foreach(var s in p.GetComponentsInChildren<TextMeshProUGUI>()){
            s.ForceMeshUpdate();
            Check(s.font!=null && s.textInfo.characterCount>0 && !s.isTextOverflowing,"Unusable text: "+s.text);
        }
    }
    static void Tick(){
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||Time.frameCount<8)return;
        SessionState.SetBool(Key,false);
        try{
            var t=UnityEngine.Object.FindAnyObjectByType<HouseTour>();
            Check(t && t.Plans.Length==2 && t.Plans.All(x=>x),"Missing tour or maps");
            Run(t,"OnApplicationFocus",true);Run(t,"OnApplicationPause",false);Run(t,"OnDisplayFocus",true);Run(t,"Update");
            t.Origin.Camera.transform.localPosition=new Vector3(0,1.65f,0);
            Run(t,"Welcome");
            var menu=GameObject.Find("Places").GetComponent<TourPanel>(); Labels(menu);
            Check(menu.GetComponentsInChildren<TextMeshProUGUI>().Any(x=>x.text=="Built by Digital Media Design Team - COVE RMIT"),"Missing team credit");
            var head=t.Origin.Camera.transform;
            void At(int idx){
                var h=head.position; var s=t.Stops[idx].position;
                Check(Mathf.Abs(t.Origin.transform.position.y-s.y)<.03f && Vector2.Distance(new Vector2(h.x,h.z),new Vector2(s.x,s.z))<.04f,"Bad stop "+idx+" actual "+h+" wanted "+s);
            }
            void Dwell(TourPanel panel,string label){
                var b=panel.GetComponentsInChildren<BoxCollider>().Single(c=>c.name==label);
                head.LookAt(b.transform.position);
                Physics.SyncTransforms();
                Check(Physics.Raycast(head.position,head.forward,out var hit,8f) && hit.collider==b, "Wayfinder occluded: "+label+" by "+hit.collider?.name);
                for(int i=0;i<600 && b && b.gameObject.activeInHierarchy && Vector3.Distance(head.position,b.transform.position)<3;i++)Run(t,"Update");
            }
            At(0); Dwell(menu,"Go up to south deck"); At(1);
            foreach(var step in new[]{(1,"Enter the house"),(2,"Explore living room"),(3,"Visit north deck")}){
                Run(t,"Update");
                var sign=GameObject.Find("Wayfinder - "+t.Stops[step.Item1].name).GetComponent<TourPanel>();
                Labels(sign); Dwell(sign,step.Item2); At(step.Item1+1);
            }
            Run(t,"PlacePanel");Run(t,"Places",0);Labels(menu);
            Click(menu,"1   Stair foot");At(0);
            Run(t,"PlacePanel");Run(t,"Places",1);Labels(menu);
            Click(menu,"2   South deck");At(1);
            Run(t,"OnDisplayFocus",false); Run(t,"Update"); Check(!t.Beam.enabled && !GameObject.Find("Aim reticle").GetComponent<LineRenderer>().enabled,"Pointer active under system UI");
            Run(t,"OnDisplayFocus",true);Run(t,"Update");
            Run(t,"SetTourHeight");Check(Mathf.Abs(head.position.y-t.Origin.transform.position.y-1.70f)<.01f,"Height calibration");
            foreach(var z in new[]{-2.85f,3.14f}){
                var start=new Vector3(-1.56f,2.6f,z-.65f);
                Run(t,"Land",start);head.localRotation=Quaternion.identity;
                bool crossed=false;
                foreach(float slope in new[]{-.5f,-.7f,-1f,-1.2f,-1.5f}){
                    var ray=new Ray(start+Vector3.up*1.2f,new Vector3(0,slope,1).normalized);
                    var args=new object[]{ray,Vector3.zero};
                    if((bool)Run(t,"TraceArc",args) && ((Vector3)args[1]).z>z+.25f){crossed=true;break;}
                }
                Check(crossed,"No manual teleport through opening at "+z);
            }
            InputSystem.RegisterLayout<Touch>();
            foreach(var side in new[]{"RightHand","LeftHand"}){
                head.localRotation=Quaternion.identity;
                Run(t,"PlacePanel"); Run(t,"Places",0);
                Physics.SyncTransforms();
                var close=menu.GetComponentsInChildren<BoxCollider>().Single(b=>b.name=="Close");
                var space=t.Origin.CameraFloorOffsetObject.transform;
                var pos=head.position-head.up*.16f+head.right*(side=="RightHand"?.12f:-.12f);
                var aim=Quaternion.LookRotation(close.transform.position-pos);
                var device=InputSystem.AddDevice<Touch>();
                InputSystem.SetDeviceUsage(device,side);
                using(StateEvent.From(device,out var e)){
                    device.pointerPosition.WriteValueIntoEvent(space.InverseTransformPoint(pos),e);
                    device.pointerRotation.WriteValueIntoEvent(Quaternion.Inverse(space.rotation)*aim,e);
                    device.isTracked.WriteValueIntoEvent(1f,e);
                    device.triggerPressed.WriteValueIntoEvent(1f,e);
                    InputState.Change(device,e);
                }

                Run(t,"Update");
                Check(t.Beam.enabled && Vector3.Distance(t.Beam.GetPosition(0),pos)<.01f,"No aim beam "+side);
                Check(!menu.gameObject.activeSelf,"Trigger did not close menu "+side);
                InputSystem.RemoveDevice(device);
            }
            foreach(var stop in t.Stops)Check(HouseTour.ClearFloor(stop.position),stop.name);
            Debug.Log("TEACHER_ROUTE_OK: six landings, four signs, manual teleport through both glazed bays, left and right controller selection, gaze, map, focus and height");
            EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(2);}
    }
}
