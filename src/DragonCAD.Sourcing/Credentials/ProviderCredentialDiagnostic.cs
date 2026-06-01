namespace DragonCAD.Sourcing.Credentials;

public sealed record ProviderCredentialDiagnostic(
    string ProviderName,
    string KeyName,
    ProviderCredentialDiagnosticSeverity Severity,
    string LogSafeMessage);
