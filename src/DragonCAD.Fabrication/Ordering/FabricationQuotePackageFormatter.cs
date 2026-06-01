using System.Text;

namespace DragonCAD.Fabrication.Ordering;

public static class FabricationQuotePackageFormatter
{
    private const string NewLine = "\r\n";

    public static string FormatSummary(FabricationQuotePackage package)
    {
        ArgumentNullException.ThrowIfNull(package);

        FabricationPackageValidationResult validation = package.Validate();
        StringBuilder builder = new();

        builder.Append("Provider: ");
        builder.Append(package.Provider.DisplayName);
        builder.Append(" (");
        builder.Append(package.Provider.Id);
        builder.Append(')');
        builder.Append(NewLine);

        builder.Append("OrderMode: ");
        builder.Append(package.OrderMode);
        builder.Append(NewLine);

        builder.Append("Handoff: ");
        builder.Append(package.HandoffType);
        builder.Append(NewLine);

        builder.Append("RequiredFiles: ");
        builder.AppendJoin(", ", package.Provider.RequiredFileRoles);
        builder.Append(NewLine);

        builder.Append("PackageFiles:");
        builder.Append(NewLine);
        foreach (var entry in package.Manifest.Entries)
        {
            builder.Append("- ");
            builder.Append(entry.Role);
            builder.Append(": ");
            builder.Append(entry.RelativePath.Value);
            builder.Append(NewLine);
        }

        builder.Append("Diagnostics: ");
        if (validation.Diagnostics.Count == 0)
        {
            builder.Append("none");
            return builder.ToString();
        }

        builder.Append(NewLine);
        foreach (FabricationPackageDiagnostic diagnostic in validation.Diagnostics)
        {
            builder.Append("- ");
            builder.Append(diagnostic.Severity);
            builder.Append(' ');
            builder.Append(diagnostic.Code);
            builder.Append(": ");
            builder.Append(diagnostic.Message);
            builder.Append(NewLine);
        }

        return builder.ToString().TrimEnd('\r', '\n');
    }
}
