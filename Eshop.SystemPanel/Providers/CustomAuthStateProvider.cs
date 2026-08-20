using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace Eshop.SystemPanel.Providers
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;

        public CustomAuthStateProvider(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");

            if (string.IsNullOrWhiteSpace(token))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            // Διαβάζουμε τα στοιχεία του Token
            var claims = ParseClaimsFromJwt(token).ToList();

            // --- ΝΕΟΣ ΚΩΔΙΚΑΣ: Έλεγχος Ημερομηνίας Λήξης (exp claim) ---
            var expClaim = claims.FirstOrDefault(c => c.Type == "exp");
            if (expClaim != null && long.TryParse(expClaim.Value, out var expTime))
            {
                // Μετατρέπουμε τον χρόνο του JWT σε κανονική ώρα
                var expDateTime = DateTimeOffset.FromUnixTimeSeconds(expTime).UtcDateTime;

                // Αν η ώρα λήξης είναι μικρότερη από την τωρινή ώρα...
                if (expDateTime < DateTime.UtcNow)
                {
                    // Το Token έληξε! Το διαγράφουμε από τον browser
                    await _localStorage.RemoveItemAsync("authToken");

                    // Και λέμε στο Blazor ότι ο χρήστης ΔΕΝ είναι συνδεδεμένος
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }

        // Βοηθητική μέθοδος για να διαβάζει το JWT
        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var claims = new List<Claim>();
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            if (keyValuePairs != null)
            {
                foreach (var kvp in keyValuePairs)
                {
                    claims.Add(new Claim(kvp.Key, kvp.Value.ToString()!));
                }
            }
            return claims;
        }

        private byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }
}