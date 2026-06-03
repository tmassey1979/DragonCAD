# Arduino Uno Sample Walkthrough

Use this tutorial to understand the current Arduino Uno Rev3 reference sample, its schematic and board relationship, and the limits that still keep it from being a production clone.

## Load the sample

1. Choose File > Load Arduino Uno Sample or open the Schematic workspace and choose the Uno toolbar button. Both locations invoke `LoadArduinoUnoSampleCommand`.
2. Confirm the active workspace switches to Schematic and the status line starts with "Loaded Arduino Uno Rev3 reference sample".
3. Expect 21 schematic components and 21 synchronized board footprints.

## Pin count expectations

The sample intentionally teaches pin and footprint relationships with the visible board state:

| Component | Expected schematic pins | Expected footprint pads |
| --- | ---: | ---: |
| ATmega328P-PU MCU | 28 | 28 |
| ATmega16U2 USB bridge | 32 | 32 |
| USB-B connector | 4 | 4 |
| Power header | 8 | 8 |
| Digital header D0-D7 | 8 | 8 |
| Digital header D8-D13/AREF/SDA/SCL | 10 | 10 |
| Analog header A0-A5 | 6 | 6 |
| Each AVR ISP header | 6 | 6 |

The remaining discrete parts, regulators, LEDs, crystals, reset switch, and series resistors expose the pins needed for the teaching nets in the current sample.

## Schematic and board relationship

The Schematic workspace is the source of electrical intent. The sample places the ATmega328P, ATmega16U2, USB connector, barrel jack, regulators, headers, reset circuit, crystals, LEDs, and USB series resistors, then connects teaching nets for USB, VIN, 5V, 3V3, reset, oscillator, serial, SPI, analog, and LED13 paths.

Open PCB Layout with `ShowPcbLayoutTabCommand` to inspect the synchronized board. The board keeps one footprint per schematic component, preserving each component sync id so footprint selection can be traced back to the schematic component. The current sample also includes 16 demonstration board traces across top and bottom layers so routing, active layer selection, trace movement, and via insertion can be reviewed.

Use `ActivateBoardRouteToolCommand`, `FinishBoardRouteCommand`, `PlaceBoardViaCommand`, `InsertBoardViaIntoSelectedTraceSegmentCommand`, and `MoveSelectedBoardTraceToLayerCommand` to practice editing the routed board. Use `ActivateBoardSelectToolCommand` and the PCB inspector when reviewing existing footprints, airwires, layer state, selected trace width, and board selection details.

## Why this is not a production clone yet

Treat the Arduino Uno walkthrough as a reference sample, not a fabrication-approved Uno Rev3 clone. It is useful for learning the DragonCAD workflow because it demonstrates component count, pin count, board sync, layer-aware routing, and fabrication review. It is not a production clone until follow-up issues complete the full production schematic, exact footprint/library parity, board outline and mechanical constraints, exhaustive netlist coverage, design-rule checks, and manufacturing output validation.

Open Fabrication with `ShowFabricationTabCommand` to review what DragonCAD can currently inspect. Do not submit the sample for manufacturing until those production-clone issues are complete and the fabrication readiness view reports validated outputs for the actual target board.
