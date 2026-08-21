using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Eshop.Application.Interfaces;

namespace Eshop.API.Services 
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // Διαβάζει το ID του Χρήστη
        public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
                         _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub") ??
                         _httpContextAccessor.HttpContext?.User?.FindFirstValue("id");    

        // Διαβάζει το TenantId (Ψάχνουμε και με κεφαλαίο και με μικρό για σιγουριά)
        public string? TenantId => _httpContextAccessor.HttpContext?.User?.FindFirstValue("TenantId") ??
                                   _httpContextAccessor.HttpContext?.User?.FindFirstValue("tenantId");

        //Πώς θα βρίσκει το Username
        public string? Username => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name) ??
                           _httpContextAccessor.HttpContext?.User?.FindFirstValue("name") ??
                           _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email) ??
                           UserId;

        // Διαβάζει το Role
        public string? Role => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role) ??
                               _httpContextAccessor.HttpContext?.User?.FindFirstValue("role");

        // Ελέγχει αν είναι συνδεδεμένος
        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        // Ελέγχει αν είμαστε εμείς
        public bool IsSuperAdmin => UserId == "system-superadmin";
    }
}