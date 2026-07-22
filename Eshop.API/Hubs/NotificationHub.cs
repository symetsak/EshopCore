using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace Eshop.API.Hubs
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class NotificationHub : Hub
    {
        // Εκτελείται αυτόματα τη στιγμή που ένας χρήστης (Admin ή Customer) συνδέεται στο SignalR
        public override async Task OnConnectedAsync()
        {
            var tenantIdClaim = Context.User?.FindFirst("TenantId")?.Value;
            var userIdClaim = Context.User?.FindFirst("UserId")?.Value ?? Context.User?.FindFirst("CustomerId")?.Value;
            var roleClaim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (!string.IsNullOrEmpty(tenantIdClaim) && !string.IsNullOrEmpty(userIdClaim))
            {
                var tenantLower = tenantIdClaim.ToLower().Trim();

                if (roleClaim == "Administrator" || roleClaim == "Employee")
                {
                    // Οι Admins και οι Employees μπαίνουν στο κοινό διαχειριστικό Group του Tenant
                    var adminGroupName = $"Group_{tenantLower}_Admins";
                    await Groups.AddToGroupAsync(Context.ConnectionId, adminGroupName);
                    Console.WriteLine($"[SignalR] Το Staff μέλος {Context.ConnectionId} μπήκε στο κανάλι: {adminGroupName}");
                }
                else
                {
                    // Ο ΚΑΘΕ CUSTOMER ΜΠΑΙΝΕΙ ΣΤΟ ΔΙΚΟ ΤΟΥ ΠΡΟΣΩΠΙΚΟ ΚΑΝΑΛΙ
                    var customerGroupName = $"Group_{tenantLower}_Customer_{userIdClaim}";
                    await Groups.AddToGroupAsync(Context.ConnectionId, customerGroupName);
                    Console.WriteLine($"[SignalR] Ασφαλής απομόνωση για τον Customer {Context.ConnectionId} στο προσωπικό κανάλι: {customerGroupName}");
                }
            }
            else
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Group_Anonymous");
            }

            await base.OnConnectedAsync();
        }

        // Εκτελείται αυτόματα όταν ο χρήστης κλείνει το tab ή αποσυνδέεται
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var tenantIdClaim = Context.User?.FindFirst("TenantId")?.Value;

            if (!string.IsNullOrEmpty(tenantIdClaim))
            {
                var groupName = $"Group_{tenantIdClaim.ToLower().Trim()}";
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
                Console.WriteLine($"[SignalR] Ο χρήστης {Context.ConnectionId} αποσυνδέθηκε από τον Tenant: {tenantIdClaim}");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}