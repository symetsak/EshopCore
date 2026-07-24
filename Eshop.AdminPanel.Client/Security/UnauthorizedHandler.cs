using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Blazored.LocalStorage;

namespace Eshop.AdminPanel.Client.Security
{
    public class UnauthorizedHandler : DelegatingHandler
    {
        private readonly ILocalStorageService _localStorage;
        private readonly NavigationManager _navigationManager;

        public UnauthorizedHandler(ILocalStorageService localStorage, NavigationManager navigationManager)
        {
            _localStorage = localStorage;
            _navigationManager = navigationManager;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // ΑΝ ΕΙΝΑΙ ΑΙΤΗΜΑ LOGIN / AUTH, ΑΦΗΣΕ ΤΟ ΝΑ ΠΕΡΑΣΕΙ ΕΛΕΥΘΕΡΑ!
            var requestPath = request.RequestUri?.AbsolutePath.ToLower();
            bool isAuthRequest = requestPath != null && (requestPath.Contains("/login") || requestPath.Contains("/refresh") || requestPath.Contains("/change-password"));

            if (isAuthRequest)
            {
                // Για το Login, αν ο χρήστης έχει γράψει το Tenant Slug στη φόρμα, 
                // το Login page το στέλνει ήδη στο request body ή στο header!
                return await base.SendAsync(request, cancellationToken);
            }

            // ΓΙΑ ΟΛΑ ΤΑ ΑΛΛΑ ΑΙΤΗΜΑΤΑ
            // Διαβάζουμε το Tenant ID αυστηρά και δυναμικά από το LocalStorage
            var tenantId = await _localStorage.GetItemAsync<string>("tenantId");

            // Αν λείπει το tenantId, ο χρήστης δεν θεωρείται έγκυρα συνδεδεμένος
            if (string.IsNullOrEmpty(tenantId))
            {
                await ForceLogout();
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            // Προσθήκη του X-Tenant-Id Header αν δεν υπάρχει ήδη στο αίτημα
            if (!request.Headers.Contains("X-Tenant-Id"))
            {
                request.Headers.Add("X-Tenant-Id", tenantId);
            }

            // Προσθήκη του Bearer Token Header αν υπάρχει αποθηκευμένο token
            var token = await _localStorage.GetItemAsync<string>("authToken");
            if (!string.IsNullOrEmpty(token) && request.Headers.Authorization == null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await base.SendAsync(request, cancellationToken);

            // 1. Αν η απάντηση από το API είναι 401 (Unauthorized)
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var refreshToken = await _localStorage.GetItemAsync<string>("refreshToken");

                // Αν δεν έχουμε tokens, πηγαίνουμε απευθείας σε logout χωρίς άσκοπες κλήσεις
                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(refreshToken))
                {
                    await ForceLogout();
                    return response;
                }

                // 2. Προσπάθεια Silent Refresh!
                var newTokens = await TryRefreshTokenAsync(token, refreshToken);

                if (newTokens != null)
                {
                    // 3. Αν πετύχει, σώζουμε τα νέα tokens
                    await _localStorage.SetItemAsync("authToken", newTokens.Token);
                    await _localStorage.SetItemAsync("refreshToken", newTokens.RefreshToken);

                    // 4. Φτιάχνουμε ένα ΝΕΟ request ίδιο με το αρχικό, αλλά με το ΝΕΟ token στα Headers!
                    var newRequest = CloneRequest(request);
                    newRequest.Headers.Authorization = new AuthenticationHeaderValue("bearer", newTokens.Token);

                    if (!newRequest.Headers.Contains("X-Tenant-Id"))
                    {
                        newRequest.Headers.Add("X-Tenant-Id", tenantId);
                    }

                    // 5. Ξαναστέλνουμε το request!
                    return await base.SendAsync(newRequest, cancellationToken);
                }
                else
                {
                    // Αν αποτύχει και το refresh
                    await ForceLogout();
                }
            }

            return response;
        }

        // Εδώ επιτρέπουμε nullable strings (string?) για να σβήσει η προειδοποίηση του compiler
        private async Task<RefreshTokenResponse?> TryRefreshTokenAsync(string? token, string? refreshToken)
        {
            try
            {
                // Βάζουμε το σωστό URL του API 
                using var client = new HttpClient { BaseAddress = new Uri("http://localhost:5284/") };

                var tenantId = await _localStorage.GetItemAsync<string>("tenantId");
                if (!string.IsNullOrEmpty(tenantId))
                {
                    client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);
                }

                var response = await client.PostAsJsonAsync("api/Users/refresh", new { RefreshToken = refreshToken });

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();
                }
            }
            catch
            {
                // Σιωπηλό σφάλμα
            }

            return null;
        }

        private async Task ForceLogout()
        {
            await _localStorage.RemoveItemAsync("authToken");
            await _localStorage.RemoveItemAsync("refreshToken");
            _navigationManager.NavigateTo("/login?expired=true");
        }

        // Διορθωμένη CloneRequest συμβατή με όλες τις εκδόσεις .NET 6+
        private HttpRequestMessage CloneRequest(HttpRequestMessage req)
        {
            var clone = new HttpRequestMessage(req.Method, req.RequestUri)
            {
                Content = req.Content,
                Version = req.Version
            };

            // Αντιγραφή των options με τον σωστό τρόπο
            foreach (var option in req.Options)
            {
                clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
            }

            foreach (var header in req.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clone;
        }
    }

    public class RefreshTokenResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}