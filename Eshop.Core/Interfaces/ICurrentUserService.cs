namespace Eshop.Application.Interfaces
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        string? Username { get; }
        string? TenantId { get; }
        string? Role { get; }
        bool IsAuthenticated { get; }
        bool IsSuperAdmin { get; } 
    }
}