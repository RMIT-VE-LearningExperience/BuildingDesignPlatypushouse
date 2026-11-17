# Platypus House — build notes

How the Revit model in this repo is put together, written so the house can be rebuilt in Unity or Unreal for Quest 3 without re-measuring the drawings.

## Files

| File | What it is |
|---|---|
| `BD1X_AT3_Example File 1.pdf` | The source drawing set (12 A3 sheets, A01–A12). The model is built from this. |
| `Platypus House.rvt` | Revit 2027 model (Git LFS). Will not open in Revit 2026 or earlier. |
| `Platypus House.ifc` | IFC 2x3 Coordination View 2.0 export of the `.rvt`, for older Revit and other tools. Re-export after every model change. |

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
| Ridge | 7035 | Top of gable roof |

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
- Stair: 14 risers at 185.7, 13 goings at 250, 1500 wide, x 13610–16860 at y -14905 to -13405, with handrails both sides.
- Deck guard railings are 1000 high.

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

Areas are the figures printed on sheet A04, not values read back from the model. The two deck rooms are bounded by room separation lines on the deck edges.

## Doors

| Mark | Size | Location |
|---|---|---|
| 01 | 910 x 2340 | Bed 1, in grid 2 wall at the north end |
| 02 | 910 x 2340 | Between Bed 1 and Bed 2, hard against the grid 2 wall |
| 03 | 910 x 2340 | Bed 2, in grid 2 wall at the south end |
| 04 | 910 x 2340 | Bath, south wall onto the hall |
| 05 | 2000 x 2340 | Laundry cupboard, double doors facing north |
| 06 | 910 x 2340 | Ensuite from living, in grid 6 wall |
| 07 | 910 x 2340 | Walk-in robe, north wall |
| 08 | 910 x 2340 | Ensuite from master bed, in grid 7 wall |
| — | 1000 x 2340 (x3) | Glazed deck doors: two to the north deck, one to the south deck |

The model uses hinged door families. Sheet A04 draws the internal doors as sliding panels.

## Joinery and fixtures (First Floor)

Placeholder blocks, all built from the `M_Counter Top` casework family with one `Platypus ...` type per item. They export to IFC as `IfcFurnishingElement`. Sizes and positions are scaled from sheet A04, which gives no joinery dimensions, so treat them as plus or minus 50 mm.

| Item | Type | Plan extent (mm) | Height above floor |
|---|---|---|---|
| Kitchen island | `Platypus Island 1090 x 900` | x 13565 to 14655, y -9465 to -5465 | 900 |
| Kitchen bench | `Platypus Kitchen Bench 615 x 900` | x 15880 to 16495, y -9465 to -5465 | 900 |
| Bed 1 robe | `Platypus Robe 680 x 2100` | x -2175 to -1495, y -7415 to -5585 | 2100 |
| Bed 2 robe | `Platypus Robe 680 x 2100` | x -2175 to -1495, y -9345 to -7515 | 2100 |
| Walk-in robe, west | `Platypus Robe 800 x 2100` | x 16585 to 17385, y -7455 to -5555 | 2100 |
| Walk-in robe, east | `Platypus Robe 800 x 2100` | x 18755 to 19555, y -7455 to -5555 | 2100 |
| Desk top | `Platypus Desk Top 545 x 720` | x 75 to 3015, y -11210 to -10665 | 680 to 720 |
| Window seat | `Platypus Window Seat 680 x 450` | x 24215 to 24895, y -9345 to -5585 | 450 |
| Bath vanity | `Platypus Vanity 450 x 850` | x 5685 to 6135, y -9410 to -7560 | 850 |
| Bath | `Platypus Bath 750 x 520 placeholder` | x 3135 to 3885, y -9410 to -7560 | 520 |
| Bath shower base | `Platypus Shower Base 900 x 900 placeholder` | x 3135 to 4035, y -7475 to -6575 | 60 |
| Ensuite vanity | `Platypus Ensuite Vanity 500 x 850` | x 16585 to 19555, y -11175 to -10675 | 850 |
| Ensuite shower base | `Platypus Shower Base 900 x 900 placeholder` | x 18685 to 19585, y -8445 to -7545 | 60 |

Known limits of these blocks:

- The family adds a 102 mm upstand along one long edge of every block, so each is 102 mm taller there than the height given.
- The type names carry the plan depth; the long dimension is the type's Depth parameter (and Length on the instance for wall-run items).
- The bath and shower bases are solid blocks, not sanitaryware. Replace them with proper fixtures in the engine.
- The desk top overlaps the south wall lining by 15 mm.

## Windows

All fixed glazing, hosted in the external walls.

| Type | Wall | Along-wall extent (mm) | Sill z | Head z |
|---|---|---|---|---|
| 2700 x 1900 | East bay (x = 25015) | y -8835 to -6135 | 3050 | 4950 |
| 1300 x 2240 | North (y = -4425) | x -1350 to -50 | 2690 | 4930 |
| 2070 x 2240 | North | x 475 to 2545 | 2690 | 4930 |
| 2050 x 1860 | North | x 3475 to 5525 | 3070 | 4930 |
| 1150 x 2240 | North | x 6325 to 7475 | 2690 | 4930 |
| 2070 x 2240 | North | x 9075 to 11145 | 2690 | 4930 |
| 2220 x 2240 | North | x 11370 to 13590 | 2690 | 4930 |
| 1910 x 1860 | North | x 16290 to 18200 | 3070 | 4930 |
| 2100 x 2240 | North | x 19605 to 21705 | 2690 | 4930 |
| 1450 x 2240 | North | x 22535 to 23985 | 2690 | 4930 |
| 1280 x 2240 | South main (y = -10505) | x -1370 to -90 | 2690 | 4930 |
| 2150 x 1970 | South main | x 3430 to 5580 | 2960 | 4930 |
| 1250 x 2240 | South main | x 9225 to 10475 | 2690 | 4930 |
| 1250 x 2240 | South main | x 12245 to 13495 | 2690 | 4930 |
| 2020 x 2240 | South main | x 19750 to 21770 | 2690 | 4930 |
| 1450 x 2240 | South main | x 22540 to 23990 | 2690 | 4930 |
| 2700 x 1400 | South pop-out (y = -11315) | x 200 to 2900 | 3500 | 4900 |
| 2750 x 1340 | South pop-out | x 16690 to 19440 | 3590 | 4930 |

Sill and head heights are absolute z; subtract 2600 for height above floor. The elevations show a louvre panel in the middle of most window groups; the model has plain fixed glass there.

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

## Not modelled yet

- WCs, sinks and basins, cooktop, shower screens, pantry and store shelving, laundry fit-out, fireplace unit. The project has no plumbing or appliance families loaded and no residential family library is installed.
- Loose furniture (beds, sofa, tables, chairs), intended to be added in the game engine.
- Louvre panels in the windows.
- Gutters, box gutter and downpipes.
- Site: boundaries (47.31 x 27.68 m, 1309 m²), driveway, crossover and ground surface from sheet A02.
- Sliding door families for the internal doors.

## Taking it into Unity or Unreal

- FBX export in Revit is only enabled from a 3D view; open `Whole House - Undercroft` first. Unreal can also take the model through Datasmith, and Unity through FBX or an IFC importer.
- Keep one object per building element and carry the Revit type name across, so layers can be shown, hidden and labelled for teaching (cladding, studs, lining, rafters, bracing, footings).
- The wall, floor and roof types are single solids in the model. To show construction layers in VR, split them using the build-up table above and the two details on sheet A08.
- Scale is 1:1 in millimetres; apply the axis mapping under "Coordinates and units" on import and check that First Floor lands 2.6 m above ground.
