# Platypus House VR

The Quest 3 tour starts in the undercroft. Point a controller at **Explore the house** and squeeze the trigger. Use either thumbstick to move. Push forward, aim at the floor and release over a valid landing ring. Push sideways for a 30° snap turn.

Press **A** or **X** to open **Places**. Choose a named stop to travel directly, switch between the ground and first-floor plans, or use **Recenter view**. The map marks your position and facing direction on the current floor. Optional turn shading is available here. The signs at the south staircase link its two landings. At the north deck, select **Finished**, **Cutaway** or **Framing** to inspect the wall.

## Build the APK

1. Open this folder in Unity 6000.6.9f1 with Android Build Support, SDK, NDK and OpenJDK.
2. Choose **Platypus House → Prepare Tour** to regenerate and inspect the scene after a model change.
3. Choose **Platypus House → Build Quest APK**. This rebuilds the scene, bakes its lighting and writes `Builds/PlatypusHouse.apk`. The batch-mode entry point is `-buildTarget Android -executeMethod HouseBuild.BuildQuest`.
4. For managed devices, distribute the release APK through the existing Platypus House app in Meta Horizon Device Manager. The pilot uses the **Platypus House VR – Bldg Dsgn 10** supplemental preset. Android updates require the same application ID and signer and a higher version code.

## Update the model

- Run `./Tools/Import-House.ps1 -IfcConvert "C:/path/to/IfcConvert.exe"` from this directory. By default it imports the repository's IFC. Use `-Source` to select another export. The script records the source SHA-256 and converter in `Tools/HouseSource.json` and preserves the Revit product IDs in `Tools/HouseElements.tsv`. Building-local placement keeps provisional site north separate from navigation.
- The wall sample is authored in `Tools/NorthWall.blend` and exported as `Assets/Models/Wall.glb`. Keep the Framing, Full and Cutaway groups. To regenerate the sample, run Blender with `-b --python Tools/wall.py`.
- The 120 mm wall build-up uses the model notes and sheet A08. The cladding profile, stud spacing and profile proportions are illustrative.

## Headset acceptance

Run undercroft → north deck → living room → undercroft twice. Try each hand, both stair signs, each wall state, a snap turn in each direction and the map. Check that the visible reticle selects the indicated button, labels are readable at normal distance and the rooms remain evenly lit. Test untethered and measure frame timing against 13.9 ms per frame at 72 Hz.
