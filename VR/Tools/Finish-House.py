"""Prepare metre-scaled UVs, finish slots and separate lightmap UVs for the Quest import.

Run with uv run --script Finish-House.py source.glb source.ifc elements.tsv House.glb.
"""

# /// script
# requires-python = ">=3.10,<3.14"
# dependencies = ["numpy>=1.26,<3", "xatlas==0.0.11"]
# ///
import json
import re
import struct
import sys
from collections import Counter
from pathlib import Path

import numpy as np
import xatlas

source, ifc, products, output = map(Path, sys.argv[1:])
raw = source.read_bytes()
json_size = struct.unpack_from("<I", raw, 12)[0]
model = json.loads(raw[20 : 20 + json_size])
payload = raw[28 + json_size :]
original_accessors = model["accessors"]
original_views = model["bufferViews"]
names = {
    row[0]: row[2]
    for row in (
        line.split("\t", 2)
        for line in products.read_text(encoding="utf-8-sig").splitlines()
    )
}
styles = {
    number: name
    for name, number in re.findall(
        r"IFCSURFACESTYLE\('([^']+)',\.BOTH\.,\(#(\d+)\)\)",
        ifc.read_text(encoding="utf-8", errors="replace"),
    )
}
surfaces = [styles.get(m["name"].split("-")[-1], m["name"]) for m in model["materials"]]


def read(index):
    a = original_accessors[index]
    v = original_views[a["bufferView"]]
    channels = {"VEC3": 3, "SCALAR": 1}[a["type"]]
    dtype = {5126: "<f4", 5125: "<u4", 5123: "<u2"}[a["componentType"]]
    return np.ndarray(
        (a["count"], channels),
        dtype,
        payload,
        v.get("byteOffset", 0) + a.get("byteOffset", 0),
        strides=(
            v.get("byteStride", np.dtype(dtype).itemsize * channels),
            np.dtype(dtype).itemsize,
        ),
    )


def world(node, v):
    transform = np.array(node["matrix"]).reshape(4, 4).T
    return v @ transform[:3, :3].T + transform[:3, 3]


# The first-floor footprint distinguishes room-facing lining from external wall faces,
# including short returns around the pop-outs.
floors = []
for node in model["nodes"]:
    if "Suspended Timber Floor" not in names.get(node["name"], ""):
        continue
    for p in model["meshes"][node["mesh"]]["primitives"]:
        v = world(node, read(p["attributes"]["POSITION"]))
        t = v[read(p["indices"]).reshape(-1, 3)]
        floors.extend(t[np.max(np.abs(t[:, :, 1] - 2.60), axis=1) < 0.01][:, :, [0, 2]])
footprint = np.array(floors)
if not len(footprint):
    raise ValueError("Reviewed first floor could not be identified")


def inside(xz):
    edges = np.roll(footprint, -1, axis=1) - footprint
    diff = xz - footprint
    cross = edges[:, :, 0] * diff[:, :, 1] - edges[:, :, 1] * diff[:, :, 0]
    return np.any(np.all(cross >= -0.00001, axis=1) | np.all(cross <= 0.00001, axis=1))


named = {
    "Concrete, Cast-in-Place gray": "Concrete",
    "Steel ASTM A992": "Structure",
    "Undercroft bracing - steel": "Structure",
    "IfcMember": "Structure",
    "Hardwood - Stair Tread, Slip Resistant P4/R11": "Tread",
    "Undercroft timber battens": "Battens",
    "Rainwater tank - graphite": "Tank",
    "Counter Top": "Bench",
    "Birch": "Birch",
    "Wood - Birch": "Birch",
    "Cherry": "Cherry",
    "Cherry Boards": "Cherry",
    "Linen, White": "WhiteLinen",
    "Linen, Beige": "BeigeLinen",
    "Textile - Bamboo Weave": "Weave",
    "Carpet - Berber - Tan - Stripes": "Carpet",
    "Textile - Slate Blue": "BlueLinen",
    "Leather - Black": "Leather",
    "Steel, Chrome Plated": "Chrome",
    "Steel, Polished": "Chrome",
    "Metal - Chrome": "Chrome",
    "Stainless Steel - Balustrade Rod": "Stainless",
    "Steel - Black Powder Coat": "Frame",
    "Aluminium - Black Anodised": "Frame",
    "Porcelain, Linen": "Porcelain",
    "Porcelain, Ivory": "Porcelain",
    "DefaultMaterial": "Porcelain",
    "Paint - White Door Panel": "Door",
    "Laminate, Ivory, Matte": "Laminate",
    "Laminate, Linen, Matte": "Laminate",
    "Plastic, Opaque Black": "Leather",
    "Glass": "Glass",
    "Platypus - Gypsum Lining": "Plaster",
}


def finish(style, product, normal, center):
    if "Charcoal Steel Cladding" in style:
        if abs(normal[1]) > 0.6:
            return "Flashing"
        return "Plaster" if inside((center + normal * 0.18)[[0, 2]]) else "Cladding"
    if "Basic Roof:" in product:
        if "Awning" in product:
            return "Flashing"
        if normal[1] < -0.5:
            return "Plaster" if "Gable" in product else "Soffit"
        return "Roof" if normal[1] > 0.5 else "Flashing"
    if "Suspended Timber Floor" in product or "Deck Assembly" in product:
        if "Deck Assembly" in product:
            return "Deck" if normal[1] > 0.7 else "Battens"
        return "Floor" if normal[1] > 0.7 else "Battens"
    return named.get(style, "Laminate")


binary = bytearray()
views, accessors, materials, meshes = [], [], [], []
material_ids, cache = {}, {}


def write(values, kind, scalar, target):
    values = np.ascontiguousarray(values, dtype="<f4" if scalar == 5126 else "<u4")
    offset = len(binary)
    binary.extend(values.tobytes())
    v = len(views)
    views.append(
        {
            "buffer": 0,
            "byteOffset": offset,
            "byteLength": len(binary) - offset,
            "target": target,
        }
    )
    a = {"bufferView": v, "componentType": scalar, "count": len(values), "type": kind}
    if kind == "VEC3":
        a.update(min=values.min(axis=0).tolist(), max=values.max(axis=0).tolist())
    accessors.append(a)
    return len(accessors) - 1


def material(name):
    if name not in material_ids:
        material_ids[name] = len(materials)
        materials.append(
            {
                "name": "PH_" + name,
                "doubleSided": True,
                "pbrMetallicRoughness": {"metallicFactor": 0, "roughnessFactor": 0.65},
            }
        )
    return material_ids[name]


counts = Counter()
for node in model["nodes"]:
    if "mesh" not in node:
        continue
    original = node["mesh"]
    product = names.get(node["name"], "")
    architecture = product.startswith(
        (
            "Basic Wall:",
            "Basic Roof:",
            "Floor:",
            "Foundation Slab:",
            "M_Footing-Rectangular:",
        )
    )
    key = (original, node["name"] if architecture else product.split(":")[0])
    if key in cache:
        node["mesh"] = cache[key]
        continue
    positions, normals, triangles, labels = [], [], [], []
    for prim in model["meshes"][original]["primitives"]:
        pos = read(prim["attributes"]["POSITION"])
        nor = read(prim["attributes"]["NORMAL"])
        tri = read(prim["indices"]).reshape(-1, 3).astype(np.int32)
        if architecture:
            pts = world(node, pos)[tri]
            directions = np.cross(pts[:, 1] - pts[:, 0], pts[:, 2] - pts[:, 0])
            directions /= np.maximum(
                np.linalg.norm(directions, axis=1, keepdims=True), 1e-12
            )
            labels.extend(
                finish(surfaces[prim["material"]], product, n, c)
                for n, c in zip(directions, pts.mean(axis=1))
            )
        else:
            labels.extend(
                [finish(surfaces[prim["material"]], product, np.zeros(3), np.zeros(3))]
                * len(tri)
            )
        triangles.append(tri + sum(map(len, positions)))
        positions.append(pos)
        normals.append(nor)
    pos, nor, tri = (
        np.concatenate(positions),
        np.concatenate(normals),
        np.concatenate(triangles),
    )
    tags = np.array(labels)
    uv2 = None
    bake_surface = architecture or product.startswith(
        (
            "M_Counter Top:",
            "Table-",
            "Outdoor Table:",
            "Area Rug:",
            "Sofa:",
            "Assembled Stair:",
            "Undercroft Timber Screen:",
        )
    )
    if bake_surface:
        atlas = xatlas.Atlas()
        atlas.add_mesh(pos.astype(np.float32), tri.astype(np.uint32))
        pack = xatlas.PackOptions()
        pack.resolution = 256
        pack.padding = 4
        pack.rotate_charts = True
        atlas.generate(pack_options=pack)
        remap, indices, uv2 = atlas[0]
        pos, nor, tri = pos[remap], nor[remap], indices.astype(np.int32)
        # xatlas preserves face order. Guard that contract before assigning material slots.
        if not np.array_equal(remap[tri], np.concatenate(triangles)):
            raise ValueError("Lightmap unwrap reordered triangles")
    sample = (
        world(node, pos) if architecture or "Stair" in product else pos[:, [0, 2, 1]]
    )
    components = []
    for tag in dict.fromkeys(labels):
        subset = tri[tags == tag]
        faces = sample[subset]
        face_normal = np.abs(
            np.cross(faces[:, 1] - faces[:, 0], faces[:, 2] - faces[:, 0])
        )
        projections = np.argmax(face_normal, axis=1)
        # Split UV seams at corners and keep smooth vertex normals on rounded fittings.
        pairs = np.stack((subset.flatten(), np.repeat(projections, 3)), axis=1)
        unique, inverse = np.unique(pairs, axis=0, return_inverse=True)
        source_ids, axis = unique.T
        coords = sample[source_ids]
        uv = np.stack(
            (
                np.where(axis == 0, coords[:, 2], coords[:, 0]),
                np.where(axis == 1, coords[:, 2], coords[:, 1]),
            ),
            axis=1,
        )
        if tag in ("Floor", "Tread", "Birch", "Cherry"):
            uv = uv[:, ::-1].copy()
        attrs = {
            "POSITION": write(pos[source_ids], "VEC3", 5126, 34962),
            "NORMAL": write(nor[source_ids], "VEC3", 5126, 34962),
            "TEXCOORD_0": write(uv, "VEC2", 5126, 34962),
        }
        # Normal maps need a tangent frame even though the source IFC has none.
        indices = inverse.reshape(-1, 3)
        corners = pos[source_ids][indices]
        delta = uv[indices][:, 1:] - uv[indices][:, :1]
        edges = corners[:, 1:] - corners[:, :1]
        det = delta[:, 0, 0] * delta[:, 1, 1] - delta[:, 1, 0] * delta[:, 0, 1]
        reciprocal = np.divide(
            1.0, det, out=np.zeros_like(det), where=np.abs(det) > 1e-12
        )
        tangent = (
            edges[:, 0] * delta[:, 1, 1, None] - edges[:, 1] * delta[:, 0, 1, None]
        ) * reciprocal[:, None]
        bitangent = (
            edges[:, 1] * delta[:, 0, 0, None] - edges[:, 0] * delta[:, 1, 0, None]
        ) * reciprocal[:, None]
        axis_u, axis_v = np.zeros((len(unique), 3)), np.zeros((len(unique), 3))
        for j in range(3):
            np.add.at(axis_u, indices[:, j], tangent)
            np.add.at(axis_v, indices[:, j], bitangent)
        n = nor[source_ids]
        axis_u -= n * np.sum(n * axis_u, axis=1, keepdims=True)
        lengths = np.linalg.norm(axis_u, axis=1, keepdims=True)
        missing = lengths[:, 0] < 1e-8
        axis_u[missing] = np.cross(
            n[missing], np.eye(3)[np.argmin(np.abs(n[missing]), axis=1)]
        )
        axis_u /= np.maximum(np.linalg.norm(axis_u, axis=1, keepdims=True), 1e-8)
        handed = np.where(np.sum(np.cross(n, axis_u) * axis_v, axis=1) < 0, -1, 1)
        attrs["TANGENT"] = write(np.column_stack((axis_u, handed)), "VEC4", 5126, 34962)
        if uv2 is not None:
            attrs["TEXCOORD_1"] = write(uv2[source_ids], "VEC2", 5126, 34962)
        components.append(
            {
                "attributes": attrs,
                "indices": write(inverse.reshape(-1), "SCALAR", 5125, 34963),
                "material": material(tag),
                "mode": 4,
            }
        )
        counts[tag] += len(subset)
    node["mesh"] = cache[key] = len(meshes)
    meshes.append(
        {"name": str(original) + "-" + str(len(meshes)), "primitives": components}
    )
model.update(
    meshes=meshes,
    materials=materials,
    bufferViews=views,
    accessors=accessors,
    buffers=[{"byteLength": len(binary)}],
)
header = json.dumps(model, separators=(",", ":")).encode()
header += b" " * ((-len(header)) % 4)
blob = (
    struct.pack(
        "<4sIII4s", b"glTF", 2, 28 + len(header) + len(binary), len(header), b"JSON"
    )
    + header
    + struct.pack("<I4s", len(binary), b"BIN\0")
    + binary
)
output.write_bytes(blob)
print("Quest finish triangles:", dict(sorted(counts.items())))
