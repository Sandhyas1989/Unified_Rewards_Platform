namespace UnifiedRewards.Application.Common.Interfaces;

/// <summary>Hashes and verifies user passwords (BCrypt in Infrastructure).</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
