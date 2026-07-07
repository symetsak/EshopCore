using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Eshop.Core.Interfaces;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Eshop.API.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class TenantAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string[] _roles;

        // Ο κατασκευαστής δέχεται προαιρετικά ρόλους, π.χ. [TenantAuthorize("Admin")]
        public TenantAuthorizeAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            // 1. ΕΛΕΓΧΟΣ ΑΥΘΕΝΤΙΚΟΠΟΙΗΣΗΣ: Αν το JWT Token δεν έχει περάσει ή είναι άκυρο
            if (user?.Identity?.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedResult(); // 401 Unauthorized
                return;
            }

            // 2. ΑΝΑΓΝΩΣΗ CLAIM: Διαβάζουμε το TenantId από τα Claims του JWT Token
            // Ψάχνουμε είτε "TenantId" είτε με το default URI αν έχει γίνει έτσι το mapping
            var tokenTenant = user.FindFirst("TenantId")?.Value ?? user.FindFirst(ClaimTypes.System)?.Value;

            // 3. ΑΝΑΓΝΩΣΗ REQUEST TENANT: Παίρνουμε τον Tenant που έκανε resolve το Middleware
            var tenantProvider = context.HttpContext.RequestServices.GetRequiredService<ITenantProvider>();
            var currentRequestTenant = tenantProvider.TenantId;

            // ΤΟ ΜΕΓΑΛΟ ΜΠΛΟΚΟ (Cross-Tenant Validation)
            // Αν ο Tenant του Token δεν ταιριάζει με τον Tenant του Request, πετάμε τον χρήστη έξω!
            if (string.IsNullOrEmpty(tokenTenant) || !tokenTenant.Equals(currentRequestTenant, StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new ContentResult
                {
                    StatusCode = 403, // Forbidden
                    ContentType = "text/plain; charset=utf-8",
                    Content = "Forbidden: Δεν έχετε δικαίωμα πρόσβασης στα δεδομένα αυτού του πελάτη (Tenant)!"
                };
                return;
            }

            // 4. ΕΛΕΓΧΟΣ ΡΟΛΩΝ (Optional): Αν έχουμε ορίσει συγκεκριμένους επιτρεπόμενους ρόλους
            if (_roles != null && _roles.Length > 0)
            {
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userRole) || !_roles.Contains(userRole))
                {
                    context.Result = new ContentResult
                    {
                        StatusCode = 403,
                        ContentType = "text/plain; charset=utf-8",
                        Content = "Forbidden: Ο λογαριασμός σας δεν έχει τον κατάλληλο ρόλο για αυτή την ενέργεια."
                    };
                    return;
                }
            }
        }
    }
}