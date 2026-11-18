# Platypus House VR

The Quest tour starts at the foot of the south stair. Follow the signs up to the south deck, through the open entrance, into the living room and out to the north deck wall exhibit. Select **Explore from here** to move independently. Both central deck sliders stay open. The ground under and around the house is walkable and sits 2 mm below the concrete.

Point a controller and squeeze the trigger to select. Push a thumbstick forward, aim at the floor and release over a valid landing ring. Push sideways for a 30� snap turn. Without tracked controllers, look at a button for about a second. Look down and back up to reopen Places.

Press **A** or **X** for **Places**. The map shows destinations on each floor, including the undercroft. Use **Set eye to 1.70 m** or **Use tracked height** to switch between an adjusted and a physical eye level. A low initial viewpoint is adjusted for the tour. At the north deck, select **Finished**, **Cutaway** or **Framing**.

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

Start at the stair foot, follow the forward route and manually teleport through both open sliders. Explore all four sides of the site. Check the pointer on both hands, gaze selection, eye height, all three wall states, and text at normal distance. Open the Meta menu and resume. Verify that head tracking remains stable and that tour buttons ignore system-menu input. Test untethered and measure frame timing against 13.9 ms at 72 Hz.
