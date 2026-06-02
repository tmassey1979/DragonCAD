using DragonCAD.Core.Components.Drafts;
using DragonCAD.Core.Components.Identity;
using DragonCAD.Core.Components.Promotion;
using DragonCAD.Core.Geometry;

namespace DragonCAD.Core.Tests.Components.Promotion;

public sealed class LibraryPromotionPlannerTests
{
    [Fact]
    public void CleanDraftProducesDeterministicPatchPreviewJson()
    {
        ComponentDraft draft = CreateValidDraft();
        LibraryPromotionRequest request = new(
            draft,
            TargetLibraryId: "core-timers",
            Reviewer: "Jamie Reviewer",
            DecisionId: "CMP-004-DECISION-001",
            SourceProvenanceId: "prov-datasheet-ne555p",
            TrustedLibraryPath: "BuiltInLibraries/hawkcad-core-library.hclib.json");

        LibraryPromotionPreview preview = new LibraryPromotionPlanner().Plan(request);

        Assert.False(preview.IsBlocked);
        Assert.False(preview.MutatesLibrary);
        Assert.Empty(preview.Blockers);
        Assert.Equal(ExpectedCleanPatchJson, preview.PatchPreviewJson);
    }

    [Fact]
    public void InvalidDraftProducesBlockedPreview()
    {
        ComponentDraft invalidDraft = CreateValidDraft() with { Footprints = [] };
        LibraryPromotionRequest request = CreateRequest(invalidDraft);

        LibraryPromotionPreview preview = new LibraryPromotionPlanner().Plan(request);

        Assert.True(preview.IsBlocked);
        Assert.Contains(preview.Blockers, blocker => blocker.Code == LibraryPromotionBlockerCodes.InvalidDraft);
        Assert.Contains("Draft must include at least one footprint.", preview.PatchPreviewJson, StringComparison.Ordinal);
        Assert.DoesNotContain("\"operations\": [", preview.PatchPreviewJson, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(" ", "CMP-004-DECISION-001", "prov-datasheet-ne555p", LibraryPromotionBlockerCodes.MissingReviewer)]
    [InlineData("Jamie Reviewer", "CMP-004-DECISION-001", " ", LibraryPromotionBlockerCodes.MissingSourceProvenance)]
    public void MissingRequiredPromotionMetadataProducesBlockedPreview(
        string reviewer,
        string decisionId,
        string sourceProvenanceId,
        string expectedCode)
    {
        LibraryPromotionRequest request = new(
            CreateValidDraft(),
            TargetLibraryId: "core-timers",
            reviewer,
            decisionId,
            sourceProvenanceId,
            TrustedLibraryPath: "BuiltInLibraries/hawkcad-core-library.hclib.json");

        LibraryPromotionPreview preview = new LibraryPromotionPlanner().Plan(request);

        Assert.True(preview.IsBlocked);
        Assert.Contains(preview.Blockers, blocker => blocker.Code == expectedCode);
    }

    [Fact]
    public void MissingDecisionIdProducesBlockedPreview()
    {
        LibraryPromotionRequest request = new(
            CreateValidDraft(),
            TargetLibraryId: "core-timers",
            Reviewer: "Jamie Reviewer",
            DecisionId: " ",
            SourceProvenanceId: "prov-datasheet-ne555p",
            TrustedLibraryPath: "BuiltInLibraries/hawkcad-core-library.hclib.json");

        LibraryPromotionPreview preview = new LibraryPromotionPlanner().Plan(request);

        Assert.True(preview.IsBlocked);
        Assert.Contains(preview.Blockers, blocker => blocker.Code == LibraryPromotionBlockerCodes.MissingDecisionId);
    }

    [Fact]
    public void PreviewOrderingIsStableWhenDraftCollectionsAreUnsorted()
    {
        ComponentDraft draft = CreateValidDraft() with
        {
            Attributes =
            [
                new ComponentDraftAttribute("value", "Timer"),
                new ComponentDraftAttribute("manufacturer", "Texas Instruments"),
            ],
            Pins =
            [
                new ComponentDraftPin(new ComponentPinId("pin-2"), "TRIG", "2", ComponentDraftPinElectricalType.Input),
                new ComponentDraftPin(new ComponentPinId("pin-1"), "GND", "1", ComponentDraftPinElectricalType.Power),
            ],
            Symbols =
            [
                new ComponentDraftSymbol(
                    new ComponentSymbolId("symbol-b"),
                    "Alternate symbol",
                    [new ComponentDraftSymbolPin(new ComponentPinId("pin-2"), new CadPoint(1000, 0), new CadPoint(0, 0), ComponentDraftPinOrientation.Left)],
                    [new ComponentDraftSymbolPrimitive(ComponentDraftPrimitiveKind.Line, new CadPoint(1000, 0), new CadPoint(0, 0))]),
                new ComponentDraftSymbol(
                    new ComponentSymbolId("symbol-a"),
                    "Primary symbol",
                    [new ComponentDraftSymbolPin(new ComponentPinId("pin-1"), new CadPoint(-1000, 0), new CadPoint(0, 0), ComponentDraftPinOrientation.Right)],
                    [new ComponentDraftSymbolPrimitive(ComponentDraftPrimitiveKind.Rectangle, new CadPoint(-500, -500), new CadPoint(500, 500))]),
            ],
            Footprints =
            [
                new ComponentDraftFootprint(
                    new ComponentFootprintId("dip-8-wide"),
                    "DIP-8 wide",
                    [new ComponentDraftPad(new ComponentPadId("pad-2"), "2", new CadPoint(2540, 0), new CadVector(1500, 1500), ComponentDraftPadTechnology.ThroughHole, ComponentDraftPadShape.Round, 800)],
                    [],
                    []),
                new ComponentDraftFootprint(
                    new ComponentFootprintId("dip-8"),
                    "DIP-8",
                    [new ComponentDraftPad(new ComponentPadId("pad-1"), "1", new CadPoint(0, 0), new CadVector(1500, 1500), ComponentDraftPadTechnology.ThroughHole, ComponentDraftPadShape.Round, 800)],
                    [],
                    []),
            ],
            DeviceMappings =
            [
                new ComponentDraftDeviceMapping(new ComponentPinId("pin-2"), new ComponentFootprintId("dip-8-wide"), new ComponentPadId("pad-2")),
                new ComponentDraftDeviceMapping(new ComponentPinId("pin-1"), new ComponentFootprintId("dip-8"), new ComponentPadId("pad-1")),
            ],
        };

        LibraryPromotionPreview preview = new LibraryPromotionPlanner().Plan(CreateRequest(draft));

        Assert.Equal(ExpectedStableOrderingPatchJson, preview.PatchPreviewJson);
    }

    [Fact]
    public void PlannerDoesNotMutateTrustedLibraryFile()
    {
        string libraryPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.hclib.json");
        File.WriteAllText(libraryPath, "{\"library\":\"original\"}");

        try
        {
            LibraryPromotionRequest request = new(
                CreateValidDraft(),
                TargetLibraryId: "core-timers",
                Reviewer: "Jamie Reviewer",
                DecisionId: "CMP-004-DECISION-001",
                SourceProvenanceId: "prov-datasheet-ne555p",
                TrustedLibraryPath: libraryPath);

            LibraryPromotionPreview preview = new LibraryPromotionPlanner().Plan(request);

            Assert.False(preview.MutatesLibrary);
            Assert.Equal("{\"library\":\"original\"}", File.ReadAllText(libraryPath));
        }
        finally
        {
            File.Delete(libraryPath);
        }
    }

    private static LibraryPromotionRequest CreateRequest(ComponentDraft draft) =>
        new(
            draft,
            TargetLibraryId: "core-timers",
            Reviewer: "Jamie Reviewer",
            DecisionId: "CMP-004-DECISION-001",
            SourceProvenanceId: "prov-datasheet-ne555p",
            TrustedLibraryPath: "BuiltInLibraries/hawkcad-core-library.hclib.json");

    private static ComponentDraft CreateValidDraft() =>
        new(
            new ComponentId("draft-ne555p"),
            "NE555P timer",
            new ComponentDraftPackage("DIP-8", "U", []),
            [new ComponentDraftAttribute("manufacturer", "Texas Instruments")],
            [
                new ComponentDraftPin(new ComponentPinId("pin-1"), "GND", "1", ComponentDraftPinElectricalType.Power),
                new ComponentDraftPin(new ComponentPinId("pin-2"), "TRIG", "2", ComponentDraftPinElectricalType.Input),
            ],
            [
                new ComponentDraftSymbol(
                    new ComponentSymbolId("symbol-ne555"),
                    "Timer symbol",
                    [
                        new ComponentDraftSymbolPin(new ComponentPinId("pin-1"), new CadPoint(-1000, 0), new CadPoint(0, 0), ComponentDraftPinOrientation.Right),
                        new ComponentDraftSymbolPin(new ComponentPinId("pin-2"), new CadPoint(1000, 0), new CadPoint(0, 0), ComponentDraftPinOrientation.Left),
                    ],
                    [new ComponentDraftSymbolPrimitive(ComponentDraftPrimitiveKind.Rectangle, new CadPoint(-500, -500), new CadPoint(500, 500))]),
            ],
            [
                new ComponentDraftFootprint(
                    new ComponentFootprintId("dip-8"),
                    "DIP-8",
                    [
                        new ComponentDraftPad(new ComponentPadId("pad-1"), "1", new CadPoint(0, 0), new CadVector(1500, 1500), ComponentDraftPadTechnology.ThroughHole, ComponentDraftPadShape.Round, 800),
                        new ComponentDraftPad(new ComponentPadId("pad-2"), "2", new CadPoint(2540, 0), new CadVector(1500, 1500), ComponentDraftPadTechnology.ThroughHole, ComponentDraftPadShape.Round, 800),
                    ],
                    [],
                    []),
            ],
            [
                new ComponentDraftDeviceMapping(new ComponentPinId("pin-1"), new ComponentFootprintId("dip-8"), new ComponentPadId("pad-1")),
                new ComponentDraftDeviceMapping(new ComponentPinId("pin-2"), new ComponentFootprintId("dip-8"), new ComponentPadId("pad-2")),
            ]);

    private const string ExpectedCleanPatchJson = """
{
  "schema": "dragoncad.trustedLibraryPromotionPreview.v1",
  "status": "Ready",
  "mutatesLibrary": false,
  "decision": {
    "decisionId": "CMP-004-DECISION-001",
    "reviewer": "Jamie Reviewer",
    "sourceProvenanceId": "prov-datasheet-ne555p",
    "targetLibraryId": "core-timers",
    "trustedLibraryPath": "BuiltInLibraries/hawkcad-core-library.hclib.json"
  },
  "component": {
    "id": "draft-ne555p",
    "displayName": "NE555P timer",
    "referencePrefix": "U",
    "attributes": [
      {
        "name": "manufacturer",
        "value": "Texas Instruments"
      }
    ],
    "pins": [
      {
        "id": "pin-1",
        "name": "GND",
        "number": "1",
        "electricalType": "Power"
      },
      {
        "id": "pin-2",
        "name": "TRIG",
        "number": "2",
        "electricalType": "Input"
      }
    ],
    "symbols": [
      {
        "id": "symbol-ne555",
        "name": "Timer symbol",
        "pinIds": [
          "pin-1",
          "pin-2"
        ]
      }
    ],
    "footprints": [
      {
        "id": "dip-8",
        "name": "DIP-8",
        "padIds": [
          "pad-1",
          "pad-2"
        ]
      }
    ],
    "deviceMappings": [
      {
        "pinId": "pin-1",
        "footprintId": "dip-8",
        "padId": "pad-1"
      },
      {
        "pinId": "pin-2",
        "footprintId": "dip-8",
        "padId": "pad-2"
      }
    ]
  },
  "operations": [
    {
      "op": "upsertComponentDraft",
      "path": "/libraries/core-timers/components/draft-ne555p",
      "source": "draft-ne555p"
    }
  ],
  "blockers": []
}
""";

    private const string ExpectedStableOrderingPatchJson = """
{
  "schema": "dragoncad.trustedLibraryPromotionPreview.v1",
  "status": "Ready",
  "mutatesLibrary": false,
  "decision": {
    "decisionId": "CMP-004-DECISION-001",
    "reviewer": "Jamie Reviewer",
    "sourceProvenanceId": "prov-datasheet-ne555p",
    "targetLibraryId": "core-timers",
    "trustedLibraryPath": "BuiltInLibraries/hawkcad-core-library.hclib.json"
  },
  "component": {
    "id": "draft-ne555p",
    "displayName": "NE555P timer",
    "referencePrefix": "U",
    "attributes": [
      {
        "name": "manufacturer",
        "value": "Texas Instruments"
      },
      {
        "name": "value",
        "value": "Timer"
      }
    ],
    "pins": [
      {
        "id": "pin-1",
        "name": "GND",
        "number": "1",
        "electricalType": "Power"
      },
      {
        "id": "pin-2",
        "name": "TRIG",
        "number": "2",
        "electricalType": "Input"
      }
    ],
    "symbols": [
      {
        "id": "symbol-a",
        "name": "Primary symbol",
        "pinIds": [
          "pin-1"
        ]
      },
      {
        "id": "symbol-b",
        "name": "Alternate symbol",
        "pinIds": [
          "pin-2"
        ]
      }
    ],
    "footprints": [
      {
        "id": "dip-8",
        "name": "DIP-8",
        "padIds": [
          "pad-1"
        ]
      },
      {
        "id": "dip-8-wide",
        "name": "DIP-8 wide",
        "padIds": [
          "pad-2"
        ]
      }
    ],
    "deviceMappings": [
      {
        "pinId": "pin-1",
        "footprintId": "dip-8",
        "padId": "pad-1"
      },
      {
        "pinId": "pin-2",
        "footprintId": "dip-8-wide",
        "padId": "pad-2"
      }
    ]
  },
  "operations": [
    {
      "op": "upsertComponentDraft",
      "path": "/libraries/core-timers/components/draft-ne555p",
      "source": "draft-ne555p"
    }
  ],
  "blockers": []
}
""";
}
