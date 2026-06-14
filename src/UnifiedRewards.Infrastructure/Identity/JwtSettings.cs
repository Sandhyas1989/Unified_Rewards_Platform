namespace UnifiedRewards.Infrastructure.Identity;

public sealed class JwtSettings
{
    public string Issuer { get; set; } = "UnifiedRewards";

    public string Audience { get; set; } = "UnifiedRewards.Client";

    public string SigningKey { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 60;
}
