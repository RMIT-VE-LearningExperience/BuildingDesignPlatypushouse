using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Text.Json;

[Transaction(TransactionMode.Manual)]
public class TeacherExport : IExternalCommand
{
    static double[] Metres(XYZ p) => new[]{p.X*.3048,p.Y*.3048,p.Z*.3048};
    public Result Execute(ExternalCommandData command, ref string message, ElementSet elements)
    {
        var doc=command.Application.ActiveUIDocument.Document;
        string folder=Environment.GetEnvironmentVariable("TEACHER_EXPORT_DIR") ?? throw new Exception("Set TEACHER_EXPORT_DIR");
        try {
            var locations=new FilteredElementCollector(doc).WhereElementIsNotElementType()
                .Where(e=>e is Level || e is Autodesk.Revit.DB.Architecture.Room || e.Category?.Id.Value==(long)BuiltInCategory.OST_Doors)
                .Select(e=>new {name=e.Name,category=e.Category?.Name,id=e.Id.Value,point=e is Level l?new[]{0d,0d,l.Elevation*.3048}:e.Location is LocationPoint lp?Metres(lp.Point):null});
            File.WriteAllText(Path.Combine(folder,"locations.json"),JsonSerializer.Serialize(locations));
            var mats=new FilteredElementCollector(doc).OfClass(typeof(Material)).Cast<Material>().Select(m=>new {id=m.Id.Value,name=m.Name,rgb=new[]{m.Color.Red,m.Color.Green,m.Color.Blue}.Select(c=>(int)c).ToArray(),transparency=m.Transparency});
            File.WriteAllText(Path.Combine(folder,"materials.json"),JsonSerializer.Serialize(mats));
            using var tr=new Transaction(doc,"Temporary teacher tour export view");tr.Start();
            var viewType=new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().First(x=>x.ViewFamily==ViewFamily.ThreeDimensional);
            var view=View3D.CreateIsometric(doc,viewType.Id); view.DetailLevel=ViewDetailLevel.Fine; doc.Regenerate();
            var options=new Options{View=view,IncludeNonVisibleObjects=false};
            using var writer=new StreamWriter(Path.Combine(folder,"geometry.jsonl"));
            foreach(var e in new FilteredElementCollector(doc,view.Id).WhereElementIsNotElementType()){
                if(e.Category?.CategoryType!=CategoryType.Model)continue;
                var g=e.get_Geometry(options); if(g==null)continue;
                var batches=new Dictionary<long,(List<double[]> verts,List<int> tris)>();
                void Append(Mesh mesh,Transform t,ElementId mat){
                    var id=mat?.Value??-1;
                    if(!batches.TryGetValue(id,out var b)){b=(new(),new());batches[id]=b;}
                    int offset=b.verts.Count;
                    foreach(var point in mesh.Vertices)b.verts.Add(Metres(t.OfPoint(point)));
                    for(int j=0;j<mesh.NumTriangles;j++){var tri=mesh.get_Triangle(j);foreach(int k in t.HasReflection?new[]{0,2,1}:new[]{0,1,2})b.tris.Add(offset+(int)tri.get_Index(k));}
                }
                void Visit(GeometryElement geom,Transform t){
                    foreach(var o in geom){
                        if(o is GeometryInstance instance){var sub=instance.GetSymbolGeometry();if(sub!=null)Visit(sub,t.Multiply(instance.Transform));}
                        else if(o is Solid solid)foreach(Face face in solid.Faces){var mesh=face.Triangulate(.45);if(mesh!=null&&mesh.NumTriangles>0)Append(mesh,t,face.MaterialElementId);}
                        else if(o is Mesh mesh && mesh.NumTriangles>0)Append(mesh,t,mesh.MaterialElementId);
                    }
                }
                Visit(g,Transform.Identity);
                if(batches.Count>0)writer.WriteLine(JsonSerializer.Serialize(new {id=e.Id.Value,category=e.Category.Name,name=e.Name,type=doc.GetElement(e.GetTypeId())?.Name,parts=batches.Select(b=>new {mat=b.Key,v=b.Value.verts,i=b.Value.tris})}));
            }
            writer.Flush();tr.RollBack();
            File.WriteAllText(Path.Combine(folder,"result.txt"),"OK");return Result.Succeeded;
        }catch(Exception ex){File.WriteAllText(Path.Combine(folder,"result.txt"),ex.ToString());message=ex.Message;return Result.Failed;}
    }
}