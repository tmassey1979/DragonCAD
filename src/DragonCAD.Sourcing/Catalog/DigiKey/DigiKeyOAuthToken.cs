namespace DragonCAD.Sourcing.Catalog.DigiKey;

public sealed record DigiKeyOAuthToken(
    string AccessToken,
    string TokenType,
    int ExpiresInSeconds);
