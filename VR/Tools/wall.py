"""Build the editable north wall teaching sample in Blender."""
from pathlib import Path
import bpy

root = Path(__file__).resolve().parents[1]
bpy.ops.wm.read_factory_settings(use_empty=True)

def group(name):
    obj = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(obj)
    return obj

def material(name, color, metallic=0):
    result = bpy.data.materials.new(name)
    result.use_nodes = True
    shader = result.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1)
    shader.inputs["Metallic"].default_value = metallic
    return result

def box(name, parent, center, size, surface):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.parent = parent
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(surface)

steel = material("Charcoal steel", (0.10, 0.13, 0.15), 0.55)
timber = material("Timber framing", (0.56, 0.29, 0.10))
plaster = material("Plasterboard", (0.82, 0.79, 0.71))

frame = group("Framing")
for height, label in [(0.0225, "Bottom plate"), (2.3325, "Lower top plate"), (2.3775, "Upper top plate")]:
    box(label, frame, (0, 0, height), (1.2, 0.09, 0.045), timber)
for x in (-0.5775, -0.2, 0.2, 0.5775):
    box("Stud", frame, (x, 0, 1.1775), (0.045, 0.09, 2.265), timber)

for state, span, x in [("Full", 1.2, 0), ("Cutaway", 0.54, -0.33)]:
    skin = group(state)
    box("Steel cladding", skin, (x, -0.055, 1.2), (span, 0.02, 2.4), steel)
    box("Plasterboard", skin, (x, 0.05, 1.2), (span, 0.01, 2.4), plaster)
    for index in range(round(span / 0.06)):
        rib_x = x - span / 2 + 0.03 + index * 0.06
        box("Cladding rib", skin, (rib_x, -0.063, 1.2), (0.018, 0.004, 2.4), steel)

bpy.ops.export_scene.gltf(filepath=str(root / "Assets/Models/Wall.glb"), export_format="GLB")
bpy.data.objects["Cutaway"].hide_set(True)
for child in bpy.data.objects["Cutaway"].children:
    child.hide_set(True)
bpy.ops.wm.save_as_mainfile(filepath=str(root / "Tools/NorthWall.blend"))

