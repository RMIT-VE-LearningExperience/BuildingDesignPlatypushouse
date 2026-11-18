# Platypus House – Teacher VR

This standalone Quest app uses the teacher's `BD1X_AT3_Example File 2.rvt`. Its launcher name is **Platypus House - Teacher VR** and its Android package is `com.platypushouse.teacher.vr`. The tour starts on the driveway at the south stair and continues through the south deck, entrance and living room to the north deck. Places also includes the undercroft.

The VR model uses the teacher's building, terrain, driveway, planting and furnishings. Two centre glazed infills are slid aside in the VR copy for manual teleport. The ground is lowered 2 mm to reduce overlap at slab edges. Unspecified floor finishes use illustrative timber textures. Trees use the shapes exported by Revit. Dense curved fittings and tanks are simplified for standalone rendering.

## Building

1. Obtain access to the teacher's original RVT. The source used for this release has SHA-256 `6f7005458f6de008e7ac56a3ad58934f354c3600b4372cb95f597cbcc7f36b9a`.
2. With Revit 2027.2, a .NET SDK supporting net10.0-windows and uv installed, run `./Tools/Refresh-Teacher.ps1 -Source 'path/to/BD1X_AT3_Example File 2.rvt'`. It opens a temporary copy in a separate Revit process and writes `VR/Assets/Models/TeacherHouse.glb`.
3. Open `VR` with Unity 6000.6.9f1 and Android Build Support. Choose **Teacher Example → Build Quest APK**. The build checks six landings and both deck openings, generates floor maps, bakes daylight, and writes `VR/Builds/TeacherPlatypusHouse.apk`.
4. For the automated tour check, launch Unity in batch mode with `-projectPath VR -buildTarget Android -executeMethod TeacherPreview.Verify`. This captures four views in `VR/Builds/Review` and tests navigation, controller and gaze selection, floor maps and menu focus.

The shared tour controls and CC0 material scans are adapted from the reviewed Platypus House Quest tour. See the [texture credits](VR/README.md).
