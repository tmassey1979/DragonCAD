namespace DragonCAD.Sourcing.Credentials;

public static class ProviderCredentialPlanner
{
    public static ProviderCredentialPlan Plan(
        ProviderCredentialRequirement requirement,
        IEnumerable<ProviderCredentialMetadata> availableCredentials)
    {
        ArgumentNullException.ThrowIfNull(requirement);
        ArgumentNullException.ThrowIfNull(availableCredentials);

        var providerCredentials = availableCredentials
            .Where(credential => credential.ProviderName.Equals(requirement.ProviderName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var matchingCredentials = OrderByRequiredKeys(requirement, providerCredentials);

        var diagnostics = BuildMissingCredentialDiagnostics(requirement, matchingCredentials);

        return new ProviderCredentialPlan(
            requirement.ProviderName,
            matchingCredentials,
            diagnostics);
    }

    private static IReadOnlyList<ProviderCredentialMetadata> OrderByRequiredKeys(
        ProviderCredentialRequirement requirement,
        IReadOnlyList<ProviderCredentialMetadata> credentials)
    {
        var requiredKeyPositions = requirement.RequiredKeyNames
            .Select((key, index) => new { key, index })
            .ToDictionary(item => item.key, item => item.index, StringComparer.OrdinalIgnoreCase);

        return credentials
            .OrderBy(credential => requiredKeyPositions.TryGetValue(credential.KeyName, out var index) ? index : int.MaxValue)
            .ThenBy(credential => credential.KeyName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<ProviderCredentialDiagnostic> BuildMissingCredentialDiagnostics(
        ProviderCredentialRequirement requirement,
        IReadOnlyList<ProviderCredentialMetadata> matchingCredentials)
    {
        return requirement.RequiredKeyNames
            .Where(requiredKey => !HasConfiguredCredential(matchingCredentials, requiredKey))
            .Select(requiredKey => new ProviderCredentialDiagnostic(
                requirement.ProviderName,
                requiredKey,
                ProviderCredentialDiagnosticSeverity.Error,
                $"{requirement.ProviderName}: missing required credential '{requiredKey}'. Configure it in a user credential store or environment reference outside the project file."))
            .ToArray();
    }

    private static bool HasConfiguredCredential(
        IReadOnlyList<ProviderCredentialMetadata> credentials,
        string requiredKey)
    {
        return credentials.Any(credential =>
            credential.KeyName.Equals(requiredKey, StringComparison.OrdinalIgnoreCase) &&
            credential.State == ProviderCredentialState.Configured);
    }
}
