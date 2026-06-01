using System.Text.Json;
using System.Text.Json.Serialization;
using DragonCAD.Core.Components.Identity;

namespace DragonCAD.Core.Libraries;

public sealed record HawkCadComponentLibraryIndex(
    string LibraryName,
    IReadOnlyList<HawkCadComponentLibraryIndexEntry> Devices)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public int TotalDevices => Devices.Count;

    public static HawkCadComponentLibraryIndex FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        HawkCadLibraryIndexDto dto = JsonSerializer.Deserialize<HawkCadLibraryIndexDto>(json, Options)
            ?? throw new InvalidOperationException("HawkCAD library JSON was empty.");

        HawkCadComponentLibraryIndexEntry[] devices = dto.Devices
            .Select(device => new HawkCadComponentLibraryIndexEntry(
                device.Name,
                new ComponentId($"hawkcad:{device.Name.ToLowerInvariant()}"),
                device.Attributes.FirstOrDefault(attribute => attribute.Name.Equals("Description", StringComparison.OrdinalIgnoreCase))?.Value ?? "",
                device.Attributes.FirstOrDefault(attribute => attribute.Name.Equals("Manufacturer", StringComparison.OrdinalIgnoreCase))?.Value ?? "",
                device.Attributes.FirstOrDefault(attribute => attribute.Name.Equals("PartNumber", StringComparison.OrdinalIgnoreCase))?.Value ?? "",
                device.Gates.Count,
                device.Variants.Count))
            .OrderBy(device => device.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(device => device.Name, StringComparer.Ordinal)
            .ToArray();

        return new HawkCadComponentLibraryIndex(dto.Name, devices);
    }

    public IReadOnlyList<HawkCadComponentLibraryIndexEntry> Search(string searchText, int maxResults)
    {
        if (maxResults <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxResults), "Search result limit must be positive.");
        }

        string normalizedSearch = searchText.Trim();
        IEnumerable<HawkCadComponentLibraryIndexEntry> matches = Devices;
        if (normalizedSearch.Length > 0)
        {
            matches = matches.Where(device => device.Matches(normalizedSearch));
        }

        return matches.Take(maxResults).ToArray();
    }
}

public sealed record HawkCadComponentLibraryIndexEntry(
    string Name,
    ComponentId ComponentId,
    string Description,
    string Manufacturer,
    string ManufacturerPartNumber,
    int GateCount,
    int VariantCount)
{
    public bool Matches(string searchText) =>
        Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
        Description.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
        Manufacturer.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
        ManufacturerPartNumber.Contains(searchText, StringComparison.OrdinalIgnoreCase);
}

internal sealed record HawkCadLibraryIndexDto(
    string Name,
    IReadOnlyList<HawkCadDeviceIndexDto> Devices);

internal sealed record HawkCadDeviceIndexDto(
    string Name,
    IReadOnlyList<HawkCadAttributeDto> Attributes,
    IReadOnlyList<HawkCadDeviceGateDto> Gates,
    IReadOnlyList<HawkCadDeviceVariantDto> Variants);
