using DragonCAD.Core.Components.Definitions;
using DragonCAD.Core.Libraries;

namespace DragonCAD.Core.Tests.Libraries;

public sealed class HawkCadComponentLibraryImporterTests
{
    [Fact]
    public void ImportsHawkCadLibraryDeviceIntoDragonCadComponentDefinition()
    {
        const string json = """
            {
              "attributes": [
                { "name": "SeedSources", "value": "sparkfun-eagle-libraries" }
              ],
              "devices": [
                {
                  "attributes": [
                    { "name": "Description", "value": "Generic 0603 resistor" },
                    { "name": "Manufacturer", "value": "Yageo" },
                    { "name": "PartNumber", "value": "RC0603FR-0710KL" }
                  ],
                  "gates": [
                    { "name": "G$1", "symbolName": "sparkfun/R-US", "variantName": "sparkfun/0603" }
                  ],
                  "mappings": [
                    { "gateName": "G$1", "pinName": "1", "padName": "1" },
                    { "gateName": "G$1", "pinName": "2", "padName": "2" }
                  ],
                  "name": "sparkfun/RESISTOR-0603",
                  "variants": [
                    { "name": "sparkfun/0603", "packageName": "sparkfun/0603" }
                  ]
                }
              ],
              "name": "HawkCAD Base Components",
              "packages": [
                {
                  "name": "sparkfun/0603",
                  "pads": [
                    { "name": "1", "position": { "x": -750000, "y": 0 }, "size": { "x": 900000, "y": 700000 }, "technology": "SurfaceMount" },
                    { "name": "2", "position": { "x": 750000, "y": 0 }, "size": { "x": 900000, "y": 700000 }, "technology": "SurfaceMount" }
                  ],
                  "silkscreen": [
                    { "start": { "x": -1300000, "y": -500000 }, "end": { "x": 1300000, "y": -500000 } }
                  ]
                }
              ],
              "symbols": [
                {
                  "name": "sparkfun/R-US",
                  "pins": [
                    { "name": "1", "position": { "x": -2540000, "y": 0 } },
                    { "name": "2", "position": { "x": 2540000, "y": 0 } }
                  ],
                  "outlines": [
                    { "start": { "x": -1270000, "y": 0 }, "end": { "x": 1270000, "y": 0 } }
                  ],
                  "texts": [
                    { "kind": "Name", "position": { "x": 0, "y": 1270000 }, "value": ">NAME" }
                  ]
                }
              ],
              "version": 1
            }
            """;

        HawkCadComponentLibraryImportResult result = HawkCadComponentLibraryImporter.Import(json, maxDevices: 20);

        ComponentDefinition component = Assert.Single(result.Components);
        Assert.Empty(result.Diagnostics);
        Assert.Equal("hawkcad:sparkfun/resistor-0603", component.Id.Value);
        Assert.Equal("sparkfun/RESISTOR-0603", component.DisplayName);
        Assert.Equal(ComponentKind.Passive, component.Kind);
        Assert.Equal("Yageo", component.Manufacturer);
        Assert.Equal("RC0603FR-0710KL", component.ManufacturerPartNumber);
        Assert.Equal("Generic 0603 resistor", component.Description);
        Assert.Equal(2, component.Pins.Count);
        Assert.Single(component.Symbols);
        Assert.Single(component.Footprints);
        Assert.Single(component.Variants);
        Assert.Equal(2, component.PinPadMappings.Count);
        Assert.Contains(component.Provenance, record => record.Source == "HawkCAD Base Components");
        component.Validate();
    }

    [Fact]
    public void ImportLimitKeepsLargeHawkCadLibraryLoadsBounded()
    {
        string json = $$"""
            {
              "attributes": [],
              "devices": [
                {{DeviceJson("first")}},
                {{DeviceJson("second")}}
              ],
              "name": "HawkCAD Base Components",
              "packages": [
                {{PackageJson("first")}},
                {{PackageJson("second")}}
              ],
              "symbols": [
                {{SymbolJson("first")}},
                {{SymbolJson("second")}}
              ],
              "version": 1
            }
            """;

        HawkCadComponentLibraryImportResult result = HawkCadComponentLibraryImporter.Import(json, maxDevices: 1);

        ComponentDefinition component = Assert.Single(result.Components);
        Assert.Equal("hawkcad:first", component.Id.Value);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == HawkCadComponentLibraryDiagnosticCodes.DeviceLimitReached);
    }

    [Fact]
    public void ImportDevicesMaterializesOnlyNamedIndexMatches()
    {
        string json = $$"""
            {
              "attributes": [],
              "devices": [
                {{DeviceJson("first")}},
                {{DeviceJson("second")}},
                {{DeviceJson("third")}}
              ],
              "name": "HawkCAD Base Components",
              "packages": [
                {{PackageJson("first")}},
                {{PackageJson("second")}},
                {{PackageJson("third")}}
              ],
              "symbols": [
                {{SymbolJson("first")}},
                {{SymbolJson("second")}},
                {{SymbolJson("third")}}
              ],
              "version": 1
            }
            """;

        HawkCadComponentLibraryImportResult result = HawkCadComponentLibraryImporter.ImportDevices(
            json,
            ["third", "first"]);

        Assert.Equal(["hawkcad:first", "hawkcad:third"], result.Components.Select(component => component.Id.Value).Order(StringComparer.Ordinal));
        Assert.DoesNotContain(result.Components, component => component.Id.Value == "hawkcad:second");
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void IndexListsAndSearchesHawkCadDevicesWithoutImportingComponents()
    {
        string json = $$"""
            {
              "attributes": [],
              "devices": [
                {{DeviceJson("sparkfun/RESISTOR-0603")}},
                {{DeviceJson("adafruit/*555")}},
                {{DeviceJson("sparkfun/USB-C")}}
              ],
              "name": "HawkCAD Base Components",
              "packages": [
                {{PackageJson("sparkfun/RESISTOR-0603")}},
                {{PackageJson("adafruit/*555")}},
                {{PackageJson("sparkfun/USB-C")}}
              ],
              "symbols": [
                {{SymbolJson("sparkfun/RESISTOR-0603")}},
                {{SymbolJson("adafruit/*555")}},
                {{SymbolJson("sparkfun/USB-C")}}
              ],
              "version": 1
            }
            """;

        HawkCadComponentLibraryIndex index = HawkCadComponentLibraryIndex.FromJson(json);

        Assert.Equal("HawkCAD Base Components", index.LibraryName);
        Assert.Equal(3, index.TotalDevices);
        Assert.Equal(
            ["adafruit/*555", "sparkfun/RESISTOR-0603", "sparkfun/USB-C"],
            index.Devices.Select(device => device.Name));

        IReadOnlyList<HawkCadComponentLibraryIndexEntry> results = index.Search("sparkfun", maxResults: 1);

        HawkCadComponentLibraryIndexEntry result = Assert.Single(results);
        Assert.Equal("sparkfun/RESISTOR-0603", result.Name);
        Assert.Equal("hawkcad:sparkfun/resistor-0603", result.ComponentId.Value);
    }

    private static string DeviceJson(string name) =>
        $$"""
        {
          "attributes": [],
          "gates": [{ "name": "G$1", "symbolName": "{{name}}", "variantName": "{{name}}" }],
          "mappings": [{ "gateName": "G$1", "pinName": "1", "padName": "1" }],
          "name": "{{name}}",
          "variants": [{ "name": "{{name}}", "packageName": "{{name}}" }]
        }
        """;

    private static string PackageJson(string name) =>
        $$"""
        {
          "name": "{{name}}",
          "pads": [{ "name": "1", "position": { "x": 0, "y": 0 }, "size": { "x": 1000, "y": 1000 } }]
        }
        """;

    private static string SymbolJson(string name) =>
        $$"""
        {
          "name": "{{name}}",
          "pins": [{ "name": "1", "position": { "x": 0, "y": 0 } }]
        }
        """;
}
