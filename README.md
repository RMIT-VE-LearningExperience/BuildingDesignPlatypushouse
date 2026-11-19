# Platypus House – Teacher VR

This standalone Quest app uses the teacher's `BD1X_AT3_Example File 2.rvt`. Its launcher name is **Platypus House - Teacher VR** and its Android package is `com.platypushouse.teacher.vr`. The tour starts on the driveway at the south stair and continues through the south deck, entrance and living room to the north deck. Places also includes the undercroft.

Download the [teacher Quest tour 1.1.0 APK](https://github.com/RMIT-VE-LearningExperience/BuildingDesignPlatypushouse/releases/download/teacher-quest-v1.1.0/TeacherPlatypusHouse.apk), or see the [release notes and review images](https://github.com/RMIT-VE-LearningExperience/BuildingDesignPlatypushouse/releases/tag/teacher-quest-v1.1.0). This version opens the internal doors and adds warmer afternoon daylight. The automated Unity navigation checks passed; a headset runtime check has not been performed for this build.

The VR model uses the teacher's building, terrain, driveway, planting and furnishings. Seven hinged door panels are open at 90 degrees, the laundry's left bypass leaf is parked over its right leaf, and two centre glazed deck infills are slid aside in the VR copy. The original RVT stays unchanged. Door positions are set before the colliders, floor maps and baked lighting are generated. The ground is lowered 2 mm to reduce overlap at slab edges. Unspecified floor finishes use illustrative timber textures. Trees use the shapes exported by Revit. Dense curved fittings and tanks are simplified for standalone rendering.

The daylight setup uses warm afternoon sun at roughly 15 degrees elevation so it reaches beneath the deck overhang, long shadows with softened edges, neutral window fill and six indirect light bounces. This aims for the supplied interior reference's warm sunlight and softly lit pale ceiling; the final result must be reviewed after baking in Unity.

## Building

1. On the `teacher/quest-tour` branch, run `git lfs pull` to download `Source/BD1X_AT3_Example File 2.rvt`. The source used for this release has SHA-256 `6f7005458f6de008e7ac56a3ad58934f354c3600b4372cb95f597cbcc7f36b9a`.
2. With Revit 2027.2, a .NET SDK supporting net10.0-windows and uv installed, run `./Tools/Refresh-Teacher.ps1 -Source './Source/BD1X_AT3_Example File 2.rvt'`. It opens a temporary copy in a separate Revit process and writes `VR/Assets/Models/TeacherHouse.glb`.
3. Open `VR` with an activated Unity 6000.6.9f1 editor and Android Build Support (SDK, NDK and OpenJDK). Choose **Teacher Example → Build Quest APK**. The build checks six landings, eight internal door passages and both deck openings, generates floor maps, bakes daylight, and writes `VR/Builds/TeacherPlatypusHouse.apk` (version 1.1.0, Android version code 2).
4. For the automated tour check, launch Unity in batch mode with `-projectPath VR -buildTarget Android -executeMethod TeacherPreview.Verify`. This captures four views in `VR/Builds/Review` and tests manual teleportation in both directions through all ten openings, controller and gaze selection, floor maps and menu focus.

If a fresh 6000.6.9f1 installation reports missing `MaterialReferenceChanger`, `RenderPipelineConverterVisualElementListFilter`, or rendering-layer migration helpers, close Unity and run `./Tools/Repair-UnityRendering.ps1 -Unity 'path/to/6000.6.9f1'`. It restores omitted editor helper files from pinned official Unity source and repairs access to the existing internal editor methods. This repair was needed on the local Windows installation; it does not change player rendering.

`Tools/TourDoors.py` contains the measured open poses for this teacher source. It rejects unknown door IDs or changed panel dimensions. `TeacherHouse.glb` and `TeacherDoorways.json` are regenerated together. For a geometry-only check without a Unity licence, run `uv run --script Tools/CheckTeacherDoors.py path/to/exported/geometry`. This checks a conservative passage box against the exported triangles and verifies that each closed door obstructs the same route. Unity's capsule and teleport checks are still required before releasing an APK.

The shared tour controls and CC0 material scans are adapted from the reviewed Platypus House Quest tour. See the [texture credits](VR/README.md).
