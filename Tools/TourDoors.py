"""Open the measured door panels in the teacher example's VR copy (Revit metres).

These IDs describe the LFS source documented in README.md. Its Doors geometry
contains panels only; the surrounding walls and curtain mullions stay in place.
Fail on changed door geometry rather than moving an unidentified frame or fitting.
"""

import numpy as np


# Panel width axis, hinge end, rotation about Revit Z. Swing into the room,
# alongside the adjacent wall, leaving the original opening clear.
SWINGS = {
    596600: (0, "max", 90),
    598509: (1, "min", -90),
    598586: (1, "min", 90),
    601763: (1, "min", 90),
    601957: (1, "max", -90),
    607827: (0, "min", 90),
    607891: (0, "max", -90),
}
LAUNDRY = 598938
EXPECTED = set(SWINGS) | {LAUNDRY}


def open_door(obj):
    """Move panels before UV generation, light baking and collider creation.

    Return the original clear passage in Unity XYZ for the build checks.
    """
    eid = obj["id"]
    if eid not in EXPECTED:
        raise ValueError(f"Unreviewed teacher door {eid}; check its open pose first")
    vertices = np.vstack([part["v"] for part in obj["parts"]])
    low, high = vertices.min(0), vertices.max(0)
    span = high - low
    axis = SWINGS[eid][0] if eid in SWINGS else 0
    expected_width = 1.01 if eid in SWINGS else 2.02
    if not (
        abs(span[axis] - expected_width) < .025
        and .025 < span[1 - axis] < .10
        and abs(span[2] - 2.34) < .025
    ):
        raise ValueError(f"Door {eid} no longer matches the reviewed panels: {span}")
    center = (low + high) * .5
    if eid == LAUNDRY:
        # Two overlapping bypass leaves: park the left leaf over the right leaf,
        # on a separate track. Open the left half, retaining both original panels.
        center[0] = low[0] + .51
        for part in obj["parts"]:
            v = np.asarray(part["v"], dtype=float)
            tri = np.asarray(part["i"], dtype=int).reshape(-1, 3)
            # The leaves overlap at their meeting edges, but occupy separate Y
            # tracks. Splitting at X would tear their overlapping end faces.
            moving_faces = v[tri, 1].mean(1) > (low[1] + high[1]) * .5
            moving = np.unique(tri[moving_faces])
            fixed = np.unique(tri[~moving_faces])
            if not len(moving) or not len(fixed) or np.intersect1d(moving, fixed).size:
                raise ValueError("Laundry leaves cannot be separated safely")
            v[moving, 0] += 1.02
            v[moving, 1] += .05
            part["v"] = v.tolist()
    else:
        _, hinge_end, degrees = SWINGS[eid]
        pivot = center.copy()
        pivot[axis] = low[axis] if hinge_end == "min" else high[axis]
        angle = np.deg2rad(degrees)
        c, s = np.cos(angle), np.sin(angle)
        rotation = np.array([[c, -s], [s, c]])
        for part in obj["parts"]:
            v = np.asarray(part["v"], dtype=float)
            v[:, :2] = (v[:, :2] - pivot[:2]) @ rotation.T + pivot[:2]
            part["v"] = v.tolist()
    return dict(
        id=eid,
        center=[float(center[0]), float(low[2] - .01), float(center[1])],
        direction=[0, 0, 1] if axis == 0 else [1, 0, 0],
    )
