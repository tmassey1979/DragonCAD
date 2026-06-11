namespace DragonCAD.Sourcing.Credentials;

public sealed record ProviderCredentialPlan(
    string ProviderName,
    IReadOnlyList<ProviderCredentialMetadata> Credentials,
    IReadOnlyList<ProviderCredentialDiagnostic> Diagnostics)
{
    public bool IsReady => Diagnostics.All(diagnostic => diagnostic.Severity != ProviderCredentialDiagnosticSeverity.Error);

    public IReadOnlyList<string> MissingRequiredKeys { get; } = Diagnostics
        .Where(diagnostic => diagnostic.Severity == ProviderCredentialDiagnosticSeverity.Error)
        .Select(diagnostic => diagnostic.KeyName)
        .ToArray();

    public ProviderCredentialProjectRecord ToProjectRecord() =>
        new(
            ProviderName,
            Credentials
                .Select(credential => new ProviderCredentialProjectChoice(
                    credential.ProviderName,
                    credential.KeyName,
                    credential.Kind.ToString(),
                    credential.StorageLocation.ToString(),
                    credential.State.ToString()))
                .ToArray());

    public string LogSafeSummary
    {
        get
        {
            if (Credentials.Count == 0 && Diagnostics.Count == 0)
            {
                return $"{ProviderName}: no credentials required";
            }

            var credentialDisplays = Credentials.Select(credential => credential.RedactedDisplay);
            var diagnosticMessages = Diagnostics.Select(diagnostic => diagnostic.LogSafeMessage);

            return string.Join("; ", credentialDisplays.Concat(diagnosticMessages));
        }
    }
}

public sealed record ProviderCredentialProjectRecord(
    string ProviderName,
    IReadOnlyList<ProviderCredentialProjectChoice> Choices);

public sealed record ProviderCredentialProjectChoice(
    string ProviderName,
    string KeyName,
    string Kind,
    string StorageLocation,
    string State);
