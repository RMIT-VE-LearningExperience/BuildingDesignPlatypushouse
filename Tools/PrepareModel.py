"""Build the teacher tour GLB from a RevitExport geometry.jsonl directory (metres)."""

# /// script
# requires-python = ">=3.10,<3.14"
# dependencies = ["numpy>=1.26,<3", "xatlas==0.0.11", "fast-simplification>=0.1.12,<0.2"]
# ///
import sys, json, struct
from pathlib import Path
import numpy as np
import xatlas
import fast_simplification
from TourDoors import EXPECTED, open_door

root = Path(__file__).resolve().parents[1]
source = Path(sys.argv[1])
materials = {m["id"]: m for m in json.loads((source / "materials.json").read_text())}
buf = bytearray()
doc = dict(
    asset=dict(version="2.0", generator="COVE Revit teacher tour"),
    scene=0,
    scenes=[dict(nodes=[])],
    nodes=[],
    meshes=[],
    materials=[],
    accessors=[],
    bufferViews=[],
)
slots = {}


def store(a, typ):
    a = np.ascontiguousarray(a, dtype="<u4" if typ == "SCALAR" else "<f4")
    views = doc["bufferViews"]
    views.append(dict(buffer=0, byteOffset=len(buf), byteLength=a.nbytes))
    buf.extend(a.tobytes())
    spec = dict(
        bufferView=len(views) - 1,
        componentType=5125 if typ == "SCALAR" else 5126,
        count=len(a),
        type=typ,
    )
    if typ == "VEC3":
        spec.update(min=a.min(0).tolist(), max=a.max(0).tolist())
    doc["accessors"].append(spec)
    return len(doc["accessors"]) - 1


def finish(m, cat, el):
    n = m.lower()
    if cat == "Planting":
        return "Foliage"
    if n == "default floor":
        return "Deck" if el == 559083 else "Floor"
    if n == "grass":
        return "Grass"
    if n == "earth":
        return "Earth"
    if n == "gravel":
        return "Ground"
    if "glass" in n:
        return "Glass" if "black" not in n else "Leather"
    if "cladding" in n:
        return "Cladding"
    if "roofing" in n:
        return "Roof"
    if "concrete" in n:
        return "Concrete"
    if "aluminium, black" in n:
        return "Frame"
    if "timber" in n:
        return "Tread" if cat == "Stairs" else "Birch"
    if n == "ida - metal" and cat == "Generic Models":
        return "Tank"
    if "laminate" in n:
        return "Laminate"
    if "leather" in n or "black" in n:
        return "Leather"
    if "chrome" in n or "stainless" in n:
        return "Stainless"
    if "porcelain" in n:
        return "Porcelain"
    if "fire " in n or "bezel red" in n:
        return {
            "fire red": "FlameRed",
            "fire yellow": "FlameGold",
            "fire orange": "FlameOrange",
        }.get(n, "FlameRed")
    if any(k in n for k in ("metal", "steel", "zinc", "aluminium", "grey")):
        return "Structure"
    if cat == "Furniture":
        return "WhiteLinen" if "white" in n else "BeigeLinen"
    if cat == "Doors":
        return "Door"
    if cat == "Casework":
        return "Laminate"
    if cat == "Generic Models":
        return "Structure"
    if cat == "Roofs":
        return "Soffit"
    return "Plaster"


def norm(a):
    return a / np.maximum(np.linalg.norm(a, axis=1, keepdims=True), 1e-14)


counts = {}
total = 0
doorways = []
for line in (source / "geometry.jsonl").open():
    obj = json.loads(line)
    cat = obj["category"]
    eid = obj["id"]
    if cat in ("Runs", "Supports", "Top Rails"):
        continue  # Parent stairs and railings include these faces.
    if cat == "Doors":
        doorways.append(open_door(obj))
    parts = []
    faces = []
    tags = []
    offset = 0
    for part in obj["parts"]:
        v = np.asarray(part["v"])
        tri = np.array(part["i"], dtype=np.int32).reshape(-1, 3)
        name = materials.get(part["mat"], {}).get("name", "")
        tag = finish(name, cat, eid)
        if eid in (
            570090,
            574035,
        ):  # Park one glazed infill alongside the adjacent bay for the VR route.
            v[:, 0] += 1.20
            v[:, 1] += 0.10 if eid == 570090 else -0.10
        if len(tri) > 15000:
            shared, ix = np.unique(v.round(6), axis=0, return_inverse=True)
            v, tri = fast_simplification.simplify(
                shared, ix[tri], target_reduction=0.70
            )
        v = v[:, [0, 2, 1]].copy()
        v[:, 0] *= -1  # Revit XYZ to glTF, then glTFast converts to Unity.
        p = v[tri]
        ok = (
            np.linalg.norm(np.cross(p[:, 1] - p[:, 0], p[:, 2] - p[:, 0]), axis=1)
            > 1e-10
        )
        tri = tri[ok]
        if not len(tri):
            continue
        parts.append(v)
        faces.append(tri + offset)
        offset += len(v)
        tags.extend([tag] * len(tri))
    if not parts:
        continue
    v = np.vstack(parts)
    tri = np.vstack(faces)
    tags = np.asarray(tags)
    p = v[tri]
    f = np.cross(p[:, 1] - p[:, 0], p[:, 2] - p[:, 0])
    n = np.zeros_like(v)
    for j in range(3):
        np.add.at(n, tri[:, j], f)
    n = norm(n)
    dominant = np.argmax(np.abs(f), axis=1)
    if cat == "Planting":
        trunk = p.mean(axis=1)[:, 1] < v[:, 1].min() + 0.20 * np.ptp(v[:, 1])
        tags = np.where(trunk, "Bark", tags)
    bake = cat in {
        "Walls",
        "Floors",
        "Roofs",
        "Ceilings",
        "Doors",
        "Toposolid",
        "Sub-divisions",
        "Stairs",
        "Casework",
        "Structural Foundations",
    }
    lm = None
    if bake:
        atlas = xatlas.Atlas()
        atlas.add_mesh(v.astype("float32"), tri.astype("uint32"))
        opts = xatlas.PackOptions()
        opts.resolution = 256
        opts.padding = 4
        atlas.generate(pack_options=opts)
        remap, new_tri, lm = atlas[0]
        if not np.array_equal(remap[new_tri], tri):
            raise RuntimeError("Unexpected lightmap face reordering")
        v, n, tri = v[remap], n[remap], new_tri
    pairs, inv = np.unique(
        np.stack((tri.ravel(), np.repeat(dominant, 3)), axis=1),
        axis=0,
        return_inverse=True,
    )
    ids, axis = pairs.T
    pos = v[ids]
    nor = n[ids]
    uv = np.stack(
        (
            np.where(axis == 0, pos[:, 2], pos[:, 0]),
            np.where(axis == 1, pos[:, 2], pos[:, 1]),
        ),
        axis=1,
    )
    # Tangents and handedness follow the two projected texture axes.
    u = np.eye(3)[np.where(axis == 0, 2, 0)]
    t = norm(u - nor * (u * nor).sum(1)[:, None])
    vv = np.eye(3)[np.where(axis == 1, 2, 1)]
    sign = np.where((np.cross(nor, t) * vv).sum(1) < 0, -1, 1)
    attrs = dict(
        POSITION=store(pos, "VEC3"),
        NORMAL=store(nor, "VEC3"),
        TANGENT=store(np.column_stack([t, sign]), "VEC4"),
        TEXCOORD_0=store(uv, "VEC2"),
    )
    if lm is not None:
        attrs["TEXCOORD_1"] = store(lm[ids], "VEC2")
    primitives = []
    for tag in np.unique(tags):
        if tag not in slots:
            slots[tag] = len(doc["materials"])
            doc["materials"].append(
                dict(
                    name="PH_" + tag,
                    pbrMetallicRoughness=dict(metallicFactor=0, roughnessFactor=0.7),
                )
            )
        primitives.append(
            dict(
                attributes=attrs,
                material=slots[tag],
                indices=store(inv.reshape(-1, 3)[tags == tag].ravel(), "SCALAR"),
            )
        )
    name = f"{cat}__{eid}"
    i = len(doc["meshes"])
    doc["scenes"][0]["nodes"].append(i)
    doc["nodes"].append(dict(name=name, mesh=i))
    doc["meshes"].append(dict(name=name, primitives=primitives))
    counts[cat] = counts.get(cat, 0) + 1
    total += len(tri)
if {door["id"] for door in doorways} != EXPECTED or len(doorways) != len(EXPECTED):
    raise RuntimeError("Incomplete teacher door export; eight door instances are required")
doc["buffers"] = [dict(byteLength=len(buf))]
header = json.dumps(doc, separators=(",", ":")).encode()
header += b" " * (-len(header) % 4)
out = root / "VR/Assets/Models/TeacherHouse.glb"
out.parent.mkdir(parents=True, exist_ok=True)
out.write_bytes(
    struct.pack(
        "<4sIII4s", b"glTF", 2, 28 + len(header) + len(buf), len(header), b"JSON"
    )
    + header
    + struct.pack("<I4s", len(buf), b"BIN\0")
    + buf
)
out.with_name("TeacherDoorways.json").write_text(json.dumps(dict(doors=doorways), indent=2))
print(f"{len(doc['nodes'])} elements, {total} triangles", counts)
print(f"Opened {len(doorways)} internal door instances and two deck glazing panels")
