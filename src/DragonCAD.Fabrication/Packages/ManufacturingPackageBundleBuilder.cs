using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DragonCAD.Fabrication.Outputs;

namespace DragonCAD.Fabrication.Packages;

public static class ManufacturingPackageBundleBuilder
{
    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static ManufacturingPackageBundle Build(ManufacturingPackageBundleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ManufacturingPackageArtifact[] sourceArtifacts = request.Artifacts
            .Select(ToArtifact)
            .OrderBy(artifact => artifact.Kind)
            .ThenBy(artifact => artifact.RelativePath.Value, StringComparer.Ordinal)
            .ToArray();

        ManufacturingPackageDiagnostic[] diagnostics = CreateDiagnostics(request, sourceArtifacts);
        string manifestJson = CreateManifestJson(request, sourceArtifacts, diagnostics);
        ManufacturingPackageArtifact manifestArtifact = new(
            ManufacturingPackageArtifactKind.Manifest,
            ManufacturingRelativePath.Create("manifest/manufacturing-package.json"),
            Sha256(Encoding.UTF8.GetBytes(manifestJson)),
            Encoding.UTF8.GetByteCount(manifestJson));

        return new ManufacturingPackageBundle(
            request.BoardId,
            request.SourceProjectId,
            request.GeneratedAt,
            request.HandoffTarget,
            [.. sourceArtifacts, manifestArtifact],
            diagnostics,
            manifestJson);
    }

    private static ManufacturingPackageArtifact ToArtifact(ManufacturingPackageArtifactSource source)
    {
        return new ManufacturingPackageArtifact(
            source.Kind,
            source.RelativePath,
            Sha256(source.Content),
            source.Content.Count);
    }

    private static ManufacturingPackageDiagnostic[] CreateDiagnostics(
        ManufacturingPackageBundleRequest request,
        IEnumerable<ManufacturingPackageArtifact> artifacts)
    {
        HashSet<ManufacturingPackageArtifactKind> providedKinds = artifacts
            .Select(artifact => artifact.Kind)
            .ToHashSet();

        return RequiredKinds(request.HandoffTarget)
            .Where(requiredKind => !providedKinds.Contains(requiredKind))
            .Select(requiredKind => new ManufacturingPackageDiagnostic(
                ManufacturingPackageDiagnosticSeverity.Blocker,
                "missing-required-artifact",
                $"{request.HandoffTarget} manufacturing package is missing required {requiredKind} artifact.",
                requiredKind,
                request.HandoffTarget))
            .OrderByDescending(diagnostic => diagnostic.Severity)
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.ArtifactKind)
            .ToArray();
    }

    private static ManufacturingPackageArtifactKind[] RequiredKinds(ManufacturingPackageHandoffTarget handoffTarget)
    {
        return handoffTarget switch
        {
            ManufacturingPackageHandoffTarget.GerberPrototype =>
            [
                ManufacturingPackageArtifactKind.Gerber,
                ManufacturingPackageArtifactKind.Drill
            ],
            ManufacturingPackageHandoffTarget.Assembly =>
            [
                ManufacturingPackageArtifactKind.Gerber,
                ManufacturingPackageArtifactKind.Drill,
                ManufacturingPackageArtifactKind.BillOfMaterials,
                ManufacturingPackageArtifactKind.PickAndPlace
            ],
            _ => []
        };
    }

    private static string CreateManifestJson(
        ManufacturingPackageBundleRequest request,
        IEnumerable<ManufacturingPackageArtifact> artifacts,
        IEnumerable<ManufacturingPackageDiagnostic> diagnostics)
    {
        ManufacturingPackageManifest manifest = new(
            request.BoardId,
            request.SourceProjectId,
            request.GeneratedAt,
            request.HandoffTarget.ToString(),
            artifacts.Select(ManufacturingPackageManifestArtifact.From).ToArray(),
            diagnostics.Select(ManufacturingPackageManifestDiagnostic.From).ToArray());

        return JsonSerializer.Serialize(manifest, ManifestJsonOptions);
    }

    private static ManufacturingChecksum Sha256(IReadOnlyCollection<byte> content)
    {
        byte[] hash = SHA256.HashData(content.ToArray());
        return ManufacturingChecksum.Create($"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}");
    }

    private sealed record ManufacturingPackageManifest(
        string BoardId,
        string SourceProjectId,
        DateTimeOffset GeneratedAt,
        string HandoffTarget,
        ManufacturingPackageManifestArtifact[] Artifacts,
        ManufacturingPackageManifestDiagnostic[] Diagnostics);

    private sealed record ManufacturingPackageManifestArtifact(
        string Kind,
        string RelativePath,
        string Checksum,
        long Length)
    {
        public static ManufacturingPackageManifestArtifact From(ManufacturingPackageArtifact artifact)
        {
            return new ManufacturingPackageManifestArtifact(
                artifact.Kind.ToString(),
                artifact.RelativePath.Value,
                artifact.Checksum.Value,
                artifact.Length);
        }
    }

    private sealed record ManufacturingPackageManifestDiagnostic(
        string Severity,
        string Code,
        string Message,
        string? ArtifactKind,
        string HandoffTarget)
    {
        public static ManufacturingPackageManifestDiagnostic From(ManufacturingPackageDiagnostic diagnostic)
        {
            return new ManufacturingPackageManifestDiagnostic(
                diagnostic.Severity.ToString(),
                diagnostic.Code,
                diagnostic.Message,
                diagnostic.ArtifactKind?.ToString(),
                diagnostic.HandoffTarget.ToString());
        }
    }
}
