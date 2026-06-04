# Net Editing

DragonCAD derives nets from schematic wires. Net editing is currently a review-and-correct workflow: inspect the generated connectivity, fix wires or labels that created the wrong net, then verify the PCB airwires before routing.

## Net review loop

- Place parts with `PlaceSelectedComponentCommand` and route intent with `ActivateWireToolCommand`.
- Use `ActivateSelectToolCommand` to inspect connected wires, pins, and component placement.
- Delete bad wire geometry with `DeleteActiveSelectionCommand`, then redraw the intended connection.
- Use `AddNetLabelCommand` only as a planned command reference; visible label editing is not yet implemented.
- Open PCB layout with `ShowPcbLayoutTabCommand` and confirm airwires match the schematic intent.

| Symptom | Local fix |
| --- | --- |
| Missing airwire | Return to the schematic and confirm both pins share a wire path. |
| Accidental short | Delete the offending wire segment and redraw only the intended connection. |
| Unclear name | Keep the wire topology simple until visible net labels are available. |

If connectivity still looks wrong after redrawing, see [Common troubleshooting](../troubleshooting/common-issues.md).
