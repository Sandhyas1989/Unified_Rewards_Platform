namespace UnifiedRewards.EmployeeProfile.Auth;

// BCrypt password hashing (ported from the monolith's BCryptPasswordHasher).
public static class PasswordHasher
{
    public static string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);
    public static bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
