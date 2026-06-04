# Sample Projects

Sample projects provide local practice designs for schematic capture, PCB layout, routing, vias, layers, and fabrication review without requiring network access.

## Available samples

| Sample | Command | Focus |
| --- | --- | --- |
| 7805 regulator | `Load7805SampleCommand` | Small schematic, simple board sync, passives, and fabrication review. |
| Arduino Uno reference | `LoadArduinoUnoSampleCommand` | Larger schematic, many footprints, routed board state, and sample limitations. |

## How to use a sample

- Open or create a local workspace first.
- Load the sample from the File menu or schematic toolbar.
- Read the status line to confirm which sample was loaded.
- Move between [Schematic placement](../schematic-editing/placing-symbols.md), [PCB routing](../editing/routing.md), and [Fabrication outputs](../fabrication/outputs.md).
- Save to a project folder with `SaveAsProjectCommand` before making experimental edits.

The samples are teaching material. Review the topic-specific walkthrough before treating any sample as a manufacturing candidate.
