# Platypus House — build notes

How the Revit model in this repo is put together, written so the house can be rebuilt in Unity or Unreal for Quest 3 without re-measuring the drawings.

## Files

| File | What it is |
|---|---|
| `BD1X_AT3_Example File 1.pdf` | The source drawing set (12 A3 sheets, A01–A12). The model is built from this. |
| `Platypus House.rvt` | Revit 2027 model (Git LFS). Will not open in Revit 2026 or earlier. |
| `Platypus House.ifc` | IFC 2x3 Coordination View 2.0 export of the `.rvt`, for older Revit and other tools. Re-export after every model change. |
| `Platypus House - Sheets A01-A12.pdf` | The model's drawing set, A3, exported from Revit (sheet set "All Sheets"). |

## Coordinates and units

- Units are millimetres. Revit and the IFC are Z-up, right-handed.
- Origin is the intersection of grid 1 and grid A at Sub Floor level (ground, z = 0).
- +X runs east along the long axis of the house (grid 1 to 8). +Y is north, so the house sits at negative Y. Plan north on the sheets is up the page.
- For Unity (Y-up, left-handed, metres) map Revit (x, y, z) mm to Unity (x, z, y) / 1000. For Unreal (Z-up, left-handed, centimetres) map to (x, -y, z) / 10.

## Levels

| Level | z (mm) | Notes |
|---|---|---|
| Sub Floor | 0 | Ground / top of slab and pad footings |
| First Floor | 2600 | Finished floor level of house and decks |
| Roof Pitching Point | 5000 | Top of wall plate; 2400 floor-to-plate |
| Ridge | 7035 | Top of gable roof. A Revit level for the 2035 and 7035 dimensions on the elevations and sections; not a building storey, so the IFC still has two storeys. |

## Grids

| Grid | x (mm) | | Grid | y (mm) |
|---|---|---|---|---|
| 1 | 0 | | A | 0 |
| 2 | 3090 | | B | -1500 |
| 3 | 6180 | | C | -4470 |
| 4 | 9110 | | D | -5025 |
| 5 | 13610 | | E | -9905 |
| 6 | 16540 | | F | -10460 |
| 7 | 19630 | | G | -13405 |
| 8 | 22720 | | | |

Numbered grids sit on wall and column centrelines. C and F are the centrelines of the 90 mm stud in the long north and south walls; D and E are the two undercroft column rows.

## Overall form

- Main body: outer faces x -1495 to 24215, y -10505 to -4425 (25 710 x 6080).
- End bays project 800: west bay (built-in robes) to x -2295, east bay (window seat) to x 25015, both y -9465 to -5465.
- South pop-outs project 810 to y -11315: desk (grid 1–2), store (grid 3–4), pantry and ensuite vanity (grid 5–7).
- North deck: x 6180 to 16540, y -4425 to 75, under a 1° canopy roof on four SHS posts.
- South deck: x 9110 to 13610, y -10505 to -14905, with a 14-riser stair running east from its south-east corner.
- Overall length 27 310; overall height 7035.
- No flat ceiling. The roof is a raked ceiling (plasterboard under the rafters, A08 roof detail), and all 13 internal partitions are attached to the underside of the gable roof, as sections AA and BB show. Their tops run from 5646 at the lowest to 6769 under the ridge. External walls stop at the 5000 pitching point.

## Construction build-ups (Revit types)

| Type | Layers, outside to inside / top to bottom |
|---|---|
| `Platypus - 120 mm Steel Clad Timber Wall` | 20 charcoal steel cladding, 90 timber stud, 10 plasterboard. The drawings dimension the 90 stud only. |
| `Platypus - 90 mm Internal Partition` | 10 plasterboard, 70 stud, 10 plasterboard |
| `Platypus - 300 mm Suspended Timber Floor` | 20 tongue-and-groove boards on 280 framing (drawings: 200 Hy-span joists at 450 on 300 LVL beams) |
| `Platypus - 300 mm Deck Assembly` | 18 decking on 282 framing |
| `Platypus - Corrugated Gable Roof` | 30 corrugated steel and battens, 190 rafters, 10 plasterboard; 30° pitch, no eave overhang |
| `Platypus - Corrugated Deck Canopy` | 26 corrugated steel, 190 framing, 6 cement sheet; 1° fall to the north |
| `Platypus - 20 mm Sheet Steel Awning` | 20 steel sheet at z 4976–5010, 800 wide shading blade around the eaves |
| `Conc 200mm` foundation slab | 200 concrete, x 3090–19630, y -9905 to -5025, top at z 0 |

Walls run from First Floor to 5000 (2400 high). The two gable end walls continue from 5000 up to the underside of the roof.

## Structure and undercroft

- 22 columns, `SHS 90 x 90 x 4`: twelve under the house on rows D and E at grids 2–7 (z 0–2300), six under the decks, and four deck canopy posts continuing 2600–5000.
- 24 pad footings, 600 x 600 x 600, top at z 0, one under each column and brace foot.
- Steel cross bracing (50 mm flat, modelled as generic elements): bays D/3–4, D/5–6, E/3–4, E/5–6, 3/D–E, 6/D–E and G/4–5.
- Each cantilevered end is carried by a V-brace frame: two longitudinal rakers, two transverse V braces, a 90 x 90 SHS beam and two hang-downs.
- Three rainwater tanks, 3050 diameter x 2150 high, centred on y -7465 at x 7950, 11360 and 14650.
- Two slatted U-shaped timber screens, 2200 high, at grids 2–3 and 6–7.
- Stair (`Platypus - Steel Carriage Stair, Open Risers`): 14 risers at 185.7, 13 goings at 250, 1500 wide, x 13610–16860 at y -14905 to -13405. Open risers with 65 mm hardwood treads (`Hardwood - Stair Tread, Slip Resistant P4/R11`), so the open gap between treads is 120.7 mm, inside the 125 mm limit in the A01 general notes. Two steel carriages (open stringers) set 150 in from each edge.
- Balustrades (`Platypus - 1000 mm Balustrade - Horizontal Rails`) on both decks and both sides of the stair, as drawn on A05/A06 and in the A12 render: 40 mm round top rail at 1000, nine 12 mm stainless rods at 100 mm centres (100 to 900), 25 mm black square posts at 1150 centres. The largest opening is 88 mm.

## Rooms (First Floor)

| No. | Name | Schedule area |
|---|---|---|
| 01 | BED 1 | 15 m² |
| 02 | BED 2 | 17 m² |
| 03 | LDRY | 5 m² |
| 04 | BATH | 8 m² |
| 05 | HALL | 3 m² |
| 06 | LIVING | 66 m² |
| 07 | WIR | 6 m² |
| 08 | ENSUITE | 10 m² |
| 09 | MASTER BED | 33 m² |
| 10 | NORTH DECKING | 47 m² |
| 11 | SOUTH DECKING | 20 m² |

Areas are the figures printed on sheet A04; the areas read back from the model are in the change log (review pass). The two deck rooms are bounded by room separation lines on the deck edges.

## Doors

All doors slide, as sheet A04 draws them. The families were built for this model (see "Window and door families" below).

| Mark | Family : type | Location | Panel side, opening direction |
|---|---|---|---|
| 01 | `Platypus - Wall Slide Door : 910 x 2340` | Bed 1, grid 2 wall at the north end | Bed 1 side, slides south |
| 02 | `Platypus - Wall Slide Door : 910 x 2340` | Between Bed 1 and Bed 2, hard against the grid 2 wall | Bed 1 side, slides west |
| 03 | `Platypus - Wall Slide Door : 910 x 2340` | Bed 2, grid 2 wall at the south end | Bed 2 side, slides north |
| 04 | `Platypus - Wall Slide Door : 910 x 2340` | Bath, south wall onto the hall | Hall side, slides east |
| 05 | `Platypus - Wall Slide Door - Bypass Pair : 2000 x 2340` | Laundry cupboard, facing north | Lobby side, two panels on two tracks |
| 06 | `Platypus - Wall Slide Door : 910 x 2340` | Ensuite from living, grid 6 wall | Ensuite side, slides north |
| 07 | `Platypus - Wall Slide Door : 910 x 2340` | Walk-in robe, north wall | Living side, slides east |
| 08 | `Platypus - Wall Slide Door : 910 x 2340` | Ensuite from master bed, grid 7 wall | Master bed side, slides north |
| N1 | `Platypus - Aluminium Sliding Door - 2 Panel : 2665 x 2380` | North deck wall, x 6345 to 9010 | Two leaves on two tracks |
| N2 | `Platypus - Aluminium Sliding Door - 3 Panel : 4315 x 2380` | North deck wall, x 9200 to 13515 | Three leaves |
| N3 | `Platypus - Aluminium Sliding Door - 2 Panel : 2700 x 2380` | North deck wall, x 13700 to 16400 | Two leaves |
| S1 | `Platypus - Aluminium Sliding Door - 3 Panel : 4320 x 2380` | South deck wall, x 9200 to 13520 | Three leaves |

- Internal doors are 35 mm panels (the thickness in the A04 door schedule) hanging 15 mm off the wall face from a 40 x 45 track that runs the full travel. Each is set 50 % open (instance parameter `Open Percent`), which is how A04 draws them; set 100 for fully open in VR. The panel overlaps the opening by 50 each side and 40 at the head.
- Door 05's two panels are shown closed; A04 draws both slid to the east.
- Door 03 is still 910 wide in an 885 hall, so Revit reports "Insert conflicts with joined Wall" (see the teacher note).
- Deck doors fill each bay of the deck walls between the canopy posts, full height from 2600 to 4980.

## Joinery and fixtures (First Floor)

Plain blocks, all built from the `M_Counter Top` casework family with one `Platypus ...` type per item. They export to IFC as `IfcFurnishingElement`. Sizes and positions are scaled from sheet A04, which gives no joinery dimensions, so treat them as plus or minus 50 mm.

| Item | Type | Plan extent (mm) | Height above floor |
|---|---|---|---|
| Kitchen island | `Platypus Island 1090 x 900` | x 13610 to 14700, y -9465 to -5465 (west edge on grid 5) | 900 |
| Kitchen bench | `Platypus Kitchen Bench 615 x 900` | x 15880 to 16495, y -9440 to -5465 | 900 |
| Bed 1 robe | `Platypus Robe 680 x 2100` | x -2175 to -1495, y -7415 to -5585 | 2100 |
| Bed 2 robe | `Platypus Robe 680 x 2100` | x -2175 to -1495, y -9345 to -7515 | 2100 |
| Walk-in robe, west | `Platypus Robe 800 x 2100` | x 16585 to 17385, y -7455 to -5555 | 2100 |
| Walk-in robe, east | `Platypus Robe 800 x 2100` | x 18755 to 19555, y -7455 to -5555 | 2100 |
| Desk top | `Platypus Desk Top 545 x 720` | x 75 to 3015, y -11210 to -10665 | 680 to 720 |
| Window seat | `Platypus Window Seat 680 x 450` | x 24215 to 24895, y -9345 to -5585 | 450 |
| Bath vanity | `Platypus Vanity 450 x 850` | x 5685 to 6135, y -9410 to -7500 | 850 |
| Ensuite vanity | `Platypus Ensuite Vanity 500 x 850` | x 16585 to 19555, y -11175 to -10675 | 850 |
| Laundry bench | `Platypus Laundry Bench 600 x 900` | x 3135 to 5280, y -6485 to -5885 | 900 |
| Bath shower base | `Platypus Shower Base 900 x 900` | x 3135 to 4035, y -7475 to -6575 | 60 |
| Ensuite shower base | `Platypus Shower Base 900 x 900` | x 18685 to 19585, y -8445 to -7545 | 60 |

Known limits of these blocks:

- The `M_Counter Top` family was edited to remove its 102 mm upstand, so every block is a plain box with a flat top.
- The island, both vanities and the laundry bench have a real cut-out under each sink. The island is cut by the sink family's own void; the other three are cut by invisible void families named `Platypus Cutout ...` (three instances, no visible geometry). Delete a cutter and its hole closes.
- The type names carry the plan depth; the long dimension is the type's Depth parameter (and Length on the instance for wall-run items).
- The desk top overlaps the south wall lining by 15 mm.

## Furniture and fixtures (First Floor)

Revit families copied from two sample projects that ship with Revit 2027 (`Pacific Continental Residence Sample.rvt` and `Snowdon Towers Sample Architectural.rvt`). Where a family has size parameters, a `Platypus ...` type was made at the size scaled off sheet A04. Positions are scaled off A04 (plus or minus 50 mm) and each piece is set against the wall or neighbour the plan shows. Extents are from the IFC.

| Room | Family and type | Plan extent (mm) | Top above floor |
|---|---|---|---|
| Bed 1 | `Bed-Shaker` Platypus Single 790 x 1905 | x -395 to 395, y -7425 to -5506 | 933 |
| Bed 1 | `Table-Night Stand` 18" x 18" x 24" | x 488 to 945, y -7324 to -6867 | 610 |
| Bed 2 | `Bed-Shaker` Platypus Single 790 x 1905 | x -395 to 395, y -9434 to -7515 | 933 |
| Bed 2 | `Table-Night Stand` 18" x 18" x 24" | x 488 to 945, y -8070 to -7613 | 610 |
| Bed 2 | `M_Chair-Task (Arms)` M_Chair-Task (Arms) | x 987 to 1661, y -10570 to -9930 | 889 |
| Laundry | `Sink Vanity-Round` Platypus Laundry Tub on 900 bench | x 4190 to 4672, y -6394 to -5912 | 1087 |
| Bath | `Toilet-Domestic-3D` Toilet-Domestic-3D | x 5336 to 6135, y -7265 to -6759 | 705 |
| Bath | `Tub-Rectangular-3D` Platypus 705 x 1800 | x 3135 to 3840, y -9328 to -7528 | 381 |
| Bath | `M_Sink - Island - Single` Platypus Basin 460 x 350 | x 5702 to 6076, y -8692 to -8235 | 1123 |
| Living, lounge | `Area Rug` Platypus 1925 x 3400 | x 7064 to 8989, y -9271 to -5871 | 6 |
| Living, lounge | `Sofa` Platypus 2400 | x 8887 to 9547, y -8955 to -6539 | 813 |
| Living, lounge | `Table-Night Stand` 18" x 18" x 24" | x 8754 to 9212, y -6506 to -6048 | 610 |
| Living, lounge | `Table-Trio End` 20" Diameter | x 7639 to 8141, y -7779 to -7277 | 502 |
| Living, lounge | `Chair-Corbu` Chair | x 7258 to 8296, y -9482 to -8710 | 622 |
| Living, dining | `Chair-Breuer` Chair-Breuer | 6 pieces within x 10845 to 12178, y -8460 to -6731 | 806 |
| Living, dining | `Table-Rectangular` Platypus Dining 880 x 2050 | x 11071 to 11952, y -8629 to -6576 | 762 |
| Kitchen | `Cook Top-4 Unit2` 22" x 20" | x 15898 to 16482, y -7446 to -6811 | 924 |
| Kitchen | `Sink Kitchen-Double` Platypus 1120 x 495 on 900 bench | x 14167 to 14661, y -8071 to -6950 | 1173 |
| Ensuite | `Toilet-Domestic-3D` Toilet-Domestic-3D | x 16585 to 17384, y -8243 to -7737 | 705 |
| Ensuite | `M_Sink - Island - Single` Platypus Basin 460 x 350 | x 17861 to 18318, y -11141 to -10767 | 1123 |
| Master bed | `Bed-Standard` Platypus Double 1490 x 1860 | x 19707 to 21567, y -8210 to -6720 | 635 |
| Master bed | `Table-Night Stand` 18" x 18" x 24" | x 19720 to 20177, y -6661 to -6204 | 610 |
| Master bed | `Table-Night Stand` 18" x 18" x 24" | x 19720 to 20177, y -8740 to -8283 | 610 |
| North decking | `Outdoor Table` Platypus Deck 735 x 2000 | x 10361 to 12361, y -2514 to -1752 | 749 |
| North decking | `Chair-Breuer` Chair-Breuer | 8 pieces within x 9854 to 12868, y -3021 to -1245 | 806 |

Where this still differs from sheet A04:

- Coffee table: A04 draws a round table about 760 across; the only round family available is 502 across and has no size parameter.
- Sofa: length matches (2416) but the family is 660 deep; A04 draws about 840.
- Armchair: `Chair-Corbu` at its stock 1040 x 770; A04 draws about 1000 x 840.
- Night stands and side table: stock 457 square; A04 draws about 445 square (side table about 495 x 390).
- Dining and deck chairs: one stock chair family for both; A04 draws plain squares.
- Basins: rectangular 460 x 350 as drawn. The laundry tub is round as drawn but 482 across where A04 draws about 390.
- Bath vanity: 450 deep; A04 scales at about 485. A short wall nib A04 draws between the WC and the vanity is not modelled (its height is not given).
- Showers: A04 shows a square with a floor waste. The model has a 60 mm base only; screens and fittings are not modelled.

In the IFC the beds, tables, chairs, sofa and rug are `IfcFurnishingElement`, the toilets, tub, basins and sinks are `IfcFlowTerminal`, and the cooktop is `IfcBuildingElementProxy`.

The layout and the review fixes were applied with Dynamo Python scripts (Manage > Dynamo), which move each piece by its geometry to a wall face or centre point. Chair, sofa, bed, toilet and tap directions were checked from the geometry.

## Windows

Black aluminium, double glazed, laid out from the north, south, east and west elevations on A05 and A06. The floor plan agrees: its thick wall segments are the louvre panels. Each grid bay holds one unit of three equal panels (glass, louvre, glass) with a 50 mm frame and mullions; the ends of the house have single-panel units, glazed to the floor at the corners. Units sit between the 90 mm wall strips on the grid lines.

| Mark | Family : type | Wall | Along-wall extent (mm) | Sill z | Head z |
|---|---|---|---|---|---|
| W01 | Fixed : 1310 x 2380 | North (y = -4425) | x -1360 to -50 | 2600 | 4980 |
| W02 | Louvre Centre : 3000 x 2380 | North | x 45 to 3045 | 2600 | 4980 |
| W03 | Louvre Centre : 2950 x 2380 | North | x 3135 to 6085 | 2600 | 4980 |
| W04 | Louvre Centre : 2950 x 2380 | North | x 16630 to 19580 | 2600 | 4980 |
| W05 | Louvre Centre : 3000 x 2380 | North | x 19670 to 22670 | 2600 | 4980 |
| W06 | Fixed : 1315 x 2380 | North | x 22765 to 24080 | 2600 | 4980 |
| W07 | Fixed : 970 x 2380 | East main (x = 24215) | y -5440 to -4470 | 2600 | 4980 |
| W08 | Louvre Centre : 3820 x 1930 | East bay (x = 25015) | y -9370 to -5550 | 3050 | 4980 |
| W09 | Fixed : 890 x 2380 | East main | y -10370 to -9480 | 2600 | 4980 |
| W10 | Fixed : 1400 x 2380 | South main (y = -10505) | x 22770 to 24170 | 2600 | 4980 |
| W11 | Louvre Centre : 2970 x 2380 | South main | x 19700 to 22670 | 2600 | 4980 |
| W12 | Louvre Centre : 2950 x 1480 | South pop-out (y = -11315) | x 16595 to 19545 | 3500 | 4980 |
| W13 | Louvre Centre : 2950 x 2380 | South main | x 3162 to 6112 | 2600 | 4980 |
| W14 | Louvre Centre : 2930 x 1480 | South pop-out | x 80 to 3010 | 3500 | 4980 |
| W15 | Fixed : 1305 x 2380 | South main | x -1360 to -55 | 2600 | 4980 |
| W16 | Fixed : 970 x 2380 | West main (x = -1495) | y -10470 to -9500 | 2600 | 4980 |
| W17 | Fixed : 970 x 2380 | West main | y -5440 to -4470 | 2600 | 4980 |

Family names are `Platypus - Aluminium Window - Fixed` and `Platypus - Aluminium Window - Louvre Centre`. Sill and head are absolute z; subtract 2600 for height above floor. Heads stop 20 below the 5000 wall top. Where an elevation draws glazing right into a corner, the unit stops 15 mm inside the 120 mm corner wall, so the corner units are up to 90 mm narrower than they scale.

## Window and door families

All six were built for this model with the Revit API (Dynamo Python), from the standard metric window, door and generic model templates. Every face is locked to reference planes and labelled dimensions, and each family was flex-tested at three sizes before it was loaded.

| Family | Parameters | Geometry |
|---|---|---|
| `Platypus - Aluminium Window - Fixed` | Width, Height, Frame Width (50), Frame Material, Glass Material | 50 x 100 frame on the wall centreline; two 6 mm panes with a 12 mm gap |
| `Platypus - Aluminium Window - Louvre Centre` | as above, plus Louvre Material; Louvre Width = Width / 3 - Frame Width, Louvre Height = Height - 2 x Frame Width | three equal panels; the middle one holds the nested `Platypus Louvre Blades` |
| `Platypus Louvre Blades` (nested) | Panel Width, Panel Height, Blade Count = rounddown((Panel Height - 60) / 100) + 1 | an array of `Platypus Louvre Blade` (80 deep x 12 thick) at 100 centres |
| `Platypus - Wall Slide Door` | Width, Height, Thickness (35), Panel Overlap (50), Panel Top Overlap (40), Panel Gap (15), Open Percent (instance), Panel/Track Material | face-mounted panel and top track; jamb lining from the template frame |
| `Platypus - Wall Slide Door - Bypass Pair` | as above, plus Track Spacing (45) | two panels on two tracks, each covering half the opening plus 25 |
| `Platypus - Aluminium Sliding Door - 2 Panel` / `- 3 Panel` | Width, Height, Frame Width (50), Meeting Overlap (25), Frame/Glass Material | outer frame; sashes alternate between an outer and inner track and overlap 50 at the meeting stiles |

Materials: `Aluminium - Black Anodised` for frames, mullions and blades; the project `Glass`; `Paint - White Door Panel` for the internal door panels.

## Drawing set (sheets A01–A12)

The sheets follow the example set: same numbers, names, views and scales, on A3.

| Sheet | Content |
|---|---|
| A01 Cover Sheet | Title, student and group placeholders, the general notes from the example set, sheet index, hidden-line 3D view |
| A02 Site Plan | 1:200 roof plan in the 47.31 x 27.68 m site with bearings, crossover, road reserve and driveway, roof and gutter notes, site notes |
| A03 Sub Floor Plan | 1:100 with grid dimensions, columns, pad footings, slab, tanks, bracing and screens, notes, section marks |
| A04 First Floor Plan | 1:100 with grid and wall dimension strings, door, window and room tags, fixture labels (CK, SK, WC, FPL, BIR), notes, room schedule and legend |
| A05 / A06 Elevations | 1:100 north/south and east/west with level dimensions (2600, 2400, 2035, 7035) and material notes; window and door schedules on A06 |
| A07 Sections | Section AA (cut at x 13470, looking west), Section BB (cut at y -6638, looking north), 3D section box |
| A08 Details | Footing and roof details at 1:20 (callouts on Section AA) with the construction notes from A08 |
| A09–A11 Shadow Diagrams | Site plan with shadows at 9am, 12pm and 3pm on 21 June |
| A12 Rendered Views | Exterior and interior perspectives (realistic display, not rendered) |

- Title block `Platypus A3 Title Block`: an A3 sheet laid out like the course title strip (amendments table, documentation by, RMIT University VE School of Design, project, sheet, date, scale, drawn by, drawing number). It reuses the parameter labels from the standard A1 title block, so the sheet name, number, scale, project name and issue date fill in automatically.
- Project information: AT3 - PLATYPUS HOUSE; documentation by "Designer", as on the example set.
- Site location Melbourne (37.81° S, 144.96° E, UTC+10). True north is 35° west of project north, from the boundary bearings on A02 (the 125° boundary runs along +X). The shadow diagrams depend on this.
- Window types carry type marks WF1–WF6 (fixed) and WL1–WL6 (louvre centre), and every window and door type has a short Description for the schedules.
- The template's unused sheets (A101–A601, C101, E1xx, G100, M1xx, P1xx, S1xx), its discipline plan views (L1/L2 lighting, power, mechanical, plumbing, structural, working) and their schedules were deleted.
- Not drawn: trees, and the driveway is only indicative. The A08 details are cut from the model, whose walls, floors and roof are single solids, so the layers are named in the notes rather than drawn.

## Change log

**17 Nov 2026 — checked against the drawing set and corrected**

- Roof: gable roof reset to 30° with its underside on the 5000 pitching point (was 27.64° and floating 184 mm above the walls). Ridge unchanged at 7035.
- Door 02 moved 410 mm east so the opening butts against the grid 2 wall, as drawn.
- Door 05 moved 420 mm west to make room for the fireplace enclosure.
- Added the partition between the laundry and the fireplace recess (x 5280–5370, y -6485 to -5550).
- Room names changed to the schedule names (LDRY, WIR, upper case), and rooms 10 NORTH DECKING and 11 SOUTH DECKING added.
- IFC re-exported.

**17 Nov 2026 — joinery added**

- Added 13 placeholder joinery and fixture blocks (table under "Joinery and fixtures").
- IFC re-exported.

**17 Nov 2026 — furniture added**

- Added beds, night stands, armchairs, dining table and chairs, kitchen appliances and sink, washer and dryer, toilets and a bathtub (table under "Furniture and fixtures").
- Shortened the kitchen bench block and removed the bath placeholder.
- IFC re-exported.

**18 Nov 2026 — furniture laid out to sheet A04**

- Bed 1 and Bed 2: queen beds replaced with single beds, heads on the shared wall, one night stand each. Master bed turned so its head is on the grid 7 wall.
- Lounge: sofa, rug, coffee table and side table added; one armchair kept, two removed, low bench removed.
- Dining: table turned north-south and moved to the middle of the living room, six chairs.
- Kitchen: range replaced by a cooktop on the bench; sink moved onto the island.
- Both toilets turned to back onto their walls (the ensuite one had been crossing a partition).
- Refrigerator, washer and dryer removed, since A04 does not show them; kitchen bench restored to its full 4000 length.
- Shower stalls replace the two shower base blocks; basins added to both vanities; bath moved 117 mm south to clear the shower.
- North decking: table and eight chairs added. Desk chair added in Bed 2.
- IFC re-exported.

**18 Nov 2026 — review pass against A04, A07 and A08**

Method: every element's extents were read from the IFC and checked for clashes, floating or buried items and duplicates; each room and the exterior were rendered and looked over; Revit's warnings, door swings and room areas were read from the model; and the plan and sections were compared at high zoom.

- Internal partitions attached to the underside of the gable roof (they stopped at 2400, leaving every room open to the next above the wall tops).
- Removed the 102 mm upstand from every joinery block. It sat on the room side of the kitchen bench, ensuite vanity and desk, and across one end of the island, robes, window seat and bath vanity.
- Sinks: the vanity basins were 53 mm below the bench top with only the tap showing, and the island was not cut out under the sink. Basins replaced with rectangular ones as drawn, set on the bench tops with the taps to the wall, and all benches cut out under their sinks.
- Laundry bench and round tub added (A04 shows them).
- Shower stalls (solid 1.8 m boxes) replaced with 60 mm shower bases.
- Sizes matched to A04: single beds 790 x 1905, master bed 1490 x 1860, sofa 2416 long, rug 1925 x 3400, dining table 880 x 2050 (was an outdoor table with an umbrella hole), deck table 735 x 2000, bath 705 x 1800, island sink 1120 x 495.
- Lounge: armchair changed to a square-armed chair as drawn, coffee table changed to a round one, side table changed from a 762 high table to a 610 high one.
- Island moved 45 mm east so its west edge is on grid 5; sink moved to the east side of the island as drawn; kitchen bench stopped 25 mm short of door 06's frame.
- Doors 01, 03 and 08 moved to their drawn positions (door 03's opening had been cutting into the bathroom corner); deck door N2 re-hung on its east side.
- Cleared a duplicate type mark on an unused floor type. Three Revit warnings remain: two room separation lines overlapping wall returns on the south deck, and door 03 against the bath wall.
- Room areas read from the model: BATH 8.5, BED 1 14.2, BED 2 16.5, ENSUITE 10.9, HALL 2.7, LDRY 2.0, LIVING 70.3, MASTER BED 28.8, NORTH DECKING 45.8, SOUTH DECKING 19.5, WIR 5.8 (225 m² against 230 scheduled). LDRY (5 scheduled) and MASTER BED (33 scheduled) differ most; the schedule seems to count the lobbies north of the laundry and walk-in robe with those rooms, where the model counts them with LIVING.
- IFC re-exported.

**18 Nov 2026 — teacher feedback: window, door, railing and stair types**

Feedback: the window and door types needed refining, and the railing and stair types were more generic than the project specs call for.

- Windows: the 18 stock fixed windows (with a 2 x 2 glazing-bar grid, white frames, a stained timber inside frame and trims) replaced with 17 black aluminium units laid out from A05/A06: 9 three-panel units with a louvre in the middle and 8 single panels. New: the four full-height panels either side of the east and west bays, and the louvre panels, which had been solid wall. The east bay window is now 3820 wide. Sizes and positions follow the elevations, so most openings changed.
- Doors: the hinged flush doors replaced with wall-face sliding doors (35 mm panels, set half open as A04 draws them) and a bypass pair for the laundry. The two hinged glazed doors and three fixed windows on the north deck wall, and the door and two windows on the south deck wall, replaced with full-height aluminium sliding doors, two or three leaves per bay, as A04 and A05 show.
- Balustrades: the four-rail guard (200 mm gaps, over the 125 mm limit in the A01 notes) replaced with nine rods at 100 mm and a 40 mm top rail at 1000, on 25 mm posts at 1150, matching the elevations. The stair handrails (900 high, vertical pickets) now use the same balustrade.
- Stair: closed 6 mm steel risers and side stringers replaced with open risers, 65 mm slip-resistant hardwood treads and two inset steel carriages, as shown in the A12 render and the A05 elevation, keeping the open gap under 125 mm.
- Removed the families no longer used: M_Window-Fixed, M_Muntin Pattern_2x2, both window trims, M_Door-Passage-Single-Flush, M_Door-Passage-Double-Flush and M_Door-Exterior-Single-Two_Lite.
- IFC re-exported.

**18 Nov 2026 — teacher feedback: drawings and sheet layouts**

- Replaced the template's A0/A1 sheets with the A01–A12 set above on a new A3 title block, with dimensions, tags, notes, schedules, sections, details, shadow diagrams and perspective views. Exported to `Platypus House - Sheets A01-A12.pdf`.
- Added the Ridge level (7035, not a storey), Melbourne site location and true north.

## Not modelled yet

- Pantry and store shelving, fireplace unit (its recess is an enclosed box, as drawn in plan), rangehood, shower screens and fittings.
- Detailed joinery in place of the plain blocks.
- Gutters, box gutter and downpipes.
- Site: boundaries (47.31 x 27.68 m, 1309 m²), driveway, crossover and ground surface from sheet A02.

## Taking it into Unity or Unreal

- FBX export in Revit is only enabled from a 3D view; open `Whole House - Undercroft` first. Unreal can also take the model through Datasmith, and Unity through FBX or an IFC importer.
- Keep one object per building element and carry the Revit type name across, so layers can be shown, hidden and labelled for teaching (cladding, studs, lining, rafters, bracing, footings).
- The wall, floor and roof types are single solids in the model. To show construction layers in VR, split them using the build-up table above and the two details on sheet A08.
- Scale is 1:1 in millimetres; apply the axis mapping under "Coordinates and units" on import and check that First Floor lands 2.6 m above ground.
