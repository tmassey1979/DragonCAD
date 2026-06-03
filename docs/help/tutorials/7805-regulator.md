# 7805 Regulator Walkthrough

Use this tutorial to learn the basic schematic-to-board loop with the built-in 7805 regulator sample. It covers loading the sample, placing a regulator-style part, connecting passives, checking board sync, and reviewing fabrication details without depending on screenshots.

## Load the sample

1. Choose File > Load 7805 Sample or open the Schematic workspace and choose the 7805 toolbar button. Both locations invoke `Load7805SampleCommand`.
2. Confirm the active workspace switches to Schematic and the status line starts with "Loaded 7805 TO-220 5V regulator sample".
3. Expect the schematic to contain three parts: the LM7805 TO-220 regulator, a 0.33uF input capacitor, and a 0.1uF output capacitor.

## Place a regulator

1. Open Component Manager from the top workspace tabs or View > Component Manager.
2. Search for `LM7805` with `SearchLibraryCommand`.
3. Select the regulator row and choose Place from the editor toolbar or Place > Place Selected Component. This invokes `PlaceSelectedComponentCommand` and arms the component for schematic placement.
4. Switch to Schematic with `ShowSchematicTabCommand`, then click on the sheet or use `PlaceArmedComponentOnSchematicCommand` to place it at the default point.

## Connect passives

1. Choose Wire from the Schematic toolbar or Place > Activate Wire Tool. This invokes `ActivateWireToolCommand`.
2. Connect the regulator IN pin to the input capacitor P pin.
3. Connect the regulator OUT pin to the output capacitor P pin.
4. Connect the regulator GND pin to both capacitor N pins.
5. Use Select from the Schematic toolbar, `ActivateSelectToolCommand`, to inspect parts or wire segments after routing.

The loaded sample already demonstrates this pattern with five schematic wires and three nets. The ground return includes the input capacitor N pin, output capacitor N pin, and regulator GND pin.

## Sync board footprints

Board footprints synchronize when schematic parts or wires are placed, moved, duplicated, deleted, or edited. After loading the 7805 sample, open PCB Layout from View > PCB Layout or the left Project Explorer. This invokes `ShowPcbLayoutTabCommand`.

Expect three board footprints, one for each schematic component, and five ratsnest airwires that mirror the schematic connections. Select a footprint in PCB Layout and use the PCB inspector to confirm its reference, display name, value, sync id, geometry summary, and placement summary.

## Route and inspect PCB details

1. In PCB Layout, choose Route from the PCB toolbar or Route > Board Route Tool. This invokes `ActivateBoardRouteToolCommand`.
2. Pick the active layer from the Layer control before starting a trace.
3. Click pads or board points to add route vertices. Choose Finish or Route > Finish Board Route to invoke `FinishBoardRouteCommand`.
4. Use Via or Route > Place Board Via to invoke `PlaceBoardViaCommand` when a route needs to change layers.
5. Use the PCB inspector to review selected trace width, active layer, layer visibility, and selected footprint geometry.

## Fabrication review

Open Fabrication from the left Project Explorer under Manufacturing or choose Fabrication > Open Fabrication Workspace. This invokes `ShowFabricationTabCommand`. Review the Gerber, drill, paste, BOM, pick-and-place, and order-readiness rows before treating the board as ready for fabrication.
