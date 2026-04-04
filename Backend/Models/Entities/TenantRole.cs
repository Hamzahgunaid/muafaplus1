namespace MuafaPlus.Models;

/// <summary>
/// Shared role enum used by InvitationCode and UserRole.
/// Stored as int in PostgreSQL.
/// </summary>
public enum TenantRole
{
    SuperAdmin    = 0,
    HospitalAdmin = 1,
    Physician     = 2,
    Assistant     = 3,
}
