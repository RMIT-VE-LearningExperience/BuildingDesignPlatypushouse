"""Check door panel transforms and conservative passage clearance without Unity.

Usage: uv run --script Tools/CheckTeacherDoors.py path/to/exported/geometry
The Unity build additionally checks capsules; TeacherPreview.Verify exercises
manual teleportation in both directions through all ten openings.
"""

# /// script
# requires-python = ">=3.10,<3.14"
# dependencies = ["numpy>=1.26,<3"]
# ///
import copy
import json
import sys
from pathlib import Path

import numpy as np

from TourDoors import EXPECTED, open_door


def intersects_box(triangles, center, half):
    """Triangle/AABB separating-axis test, including coplanar triangles."""
    t = triangles - center
    candidates = np.all(t.min(1) <= half + 1e-7, axis=1) & np.all(
        t.max(1) >= -half - 1e-7, axis=1
    )
    t = t[candidates]
    if not len(t):
        return False
    edges = np.roll(t, -1, axis=1) - t
    axes = [np.cross(edges[:, 0], edges[:, 1])]
    for edge in range(3):
        axes.extend(np.cross(edges[:, edge], basis) for basis in np.eye(3))
    overlap = np.ones(len(t), dtype=bool)
    for axis in axes:
        projection = (t * axis[:, None]).sum(2)
        radius = (np.abs(axis) * half).sum(1)
        overlap &= (projection.min(1) <= radius + 1e-7) & (
            projection.max(1) >= -radius - 1e-7
        )
    return bool(overlap.any())


def blockers(objects, doorway):
    center = np.asarray(doorway["center"])[[0, 2, 1]].copy()
    direction = np.asarray(doorway["direction"])[[0, 2, 1]]
    center[2] += .98
    # Encloses the build's .24 m radius capsule swept 1.16 m through the door.
    half = np.where(direction != 0, .82, .24)
    half[2] = .88
    hits = []
    for obj in objects:
        if obj["category"] in ("Runs", "Supports", "Top Rails"):
            continue
        for part in obj["parts"]:
            v = np.asarray(part["v"])
            tri = np.asarray(part["i"]).reshape(-1, 3)
            if intersects_box(v[tri], center, half):
                hits.append(f"{obj['category']}__{obj['id']}")
                break
    return hits


def main():
    source = Path(sys.argv[1])
    objects = [json.loads(line) for line in (source / "geometry.jsonl").open()]
    closed = copy.deepcopy(objects)
    doorways = []
    for obj in objects:
        if obj["category"] != "Doors":
            continue
        before = [np.asarray(p["v"]) for p in obj["parts"]]
        doorways.append(open_door(obj))
        for original, part in zip(before, obj["parts"]):
            moved = np.asarray(part["v"])
            assert original.shape == moved.shape, "Door vertices were lost"
            np.testing.assert_allclose(original[:, 2], moved[:, 2], atol=1e-8)
            tri = np.asarray(part["i"]).reshape(-1, 3)
            # Every triangle remains rigid, including overlapping laundry edges.
            for a, b in ((0, 1), (1, 2), (2, 0)):
                np.testing.assert_allclose(
                    np.linalg.norm(original[tri[:, a]] - original[tri[:, b]], axis=1),
                    np.linalg.norm(moved[tri[:, a]] - moved[tri[:, b]], axis=1),
                    atol=1e-8,
                )
    assert {d["id"] for d in doorways} == EXPECTED and len(doorways) == 8
    failures = []
    for doorway in doorways:
        assert f"Doors__{doorway['id']}" in blockers(closed, doorway), "Closed-door control did not obstruct passage"
        hits = blockers(objects, doorway)
        if hits:
            failures.append(f"{doorway['id']}: {', '.join(hits)}")
        else:
            print(f"CLEAR {doorway['id']}: 0.48 m wide, 1.76 m high passage")
    if failures:
        raise RuntimeError("Obstructed door approaches:\n" + "\n".join(failures))
    print("TEACHER_DOORS_OK: eight open passages; all closed-door controls obstructed; panel triangles preserved")


if __name__ == "__main__":
    main()
