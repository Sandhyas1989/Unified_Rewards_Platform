namespace UnifiedRewards.Application.Common.Interfaces;

/// <summary>
/// Ambient information about the authenticated caller. Implemented in the API layer over
/// HttpContext so the Application layer can audit "who" without depending on ASP.NET.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    string? Email { get; }

    bool IsAuthenticated { get; }
}
