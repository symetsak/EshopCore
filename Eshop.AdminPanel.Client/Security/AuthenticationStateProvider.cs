using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace Eshop.AdminPanel.Client.Security
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;
        private readonly HttpClient _httpClient;
        private readonly AuthenticationState _anonymous;

        public CustomAuthStateProvider(ILocalStorageService localStorage, HttpClient httpClient)
        {
            _localStorage = localStorage;
            _httpClient = httpClient;
            _anonymous = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");

            if (string.IsNullOrWhiteSpace(token))
            {
                // Σιγουρευόμαστε ότι το header είναι καθαρό αν δεν υπάρχει token
                _httpClient.DefaultRequestHeaders.Authorization = null;

                return _anonymous;
            }

            // Μόλις βρούμε token, προετοιμάζουμε τον HttpClient βάζοντας το Bearer Header
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);

            try
            {
                // Μόλις βρούμε token, προετοιμάζουμε τον HttpClient βάζοντας το Bearer Header
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);

                var claims = JwtParser.ParseClaimsFromJwt(token);

                // Δημιουργία της ταυτότητας του χρήστη (προσοχή στο authenticationType "jwt" για να θεωρηθεί Authenticated!)
                var identity = new ClaimsIdentity(claims, "jwt", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
                var user = new ClaimsPrincipal(identity);

                return new AuthenticationState(user);
            }
            catch
            {
                // Αν αποτύχει, σβήνουμε το token από το storage και τα headers χειροκίνητα
                // ΧΩΡΙΣ να καλέσουμε τη MarkUserAsLoggedOut(), αποφεύγοντας το infinite loop!
                await _localStorage.RemoveItemAsync("authToken");
                _httpClient.DefaultRequestHeaders.Authorization = null;

                return _anonymous;
            }
        }

        // Μέθοδος που θα καλούμε από τη σελίδα του Login όταν η σύνδεση πετυχαίνει
        public async Task MarkUserAsAuthenticated(string token, string refreshToken)
        {
            await _localStorage.SetItemAsync("authToken", token);
            await _localStorage.SetItemAsync("refreshToken", refreshToken); 

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);

            var claims = JwtParser.ParseClaimsFromJwt(token);
            var identity = new ClaimsIdentity(claims, "jwt", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
            var user = new ClaimsPrincipal(identity);

            var authState = Task.FromResult(new AuthenticationState(user));
            NotifyAuthenticationStateChanged(authState);
        }

        // Μέθοδος για το Logout
        public async Task MarkUserAsLoggedOut()
        {
            await _localStorage.RemoveItemAsync("authToken");
            await _localStorage.RemoveItemAsync("refreshToken"); 

            _httpClient.DefaultRequestHeaders.Remove("Authorization");

            var authState = Task.FromResult(_anonymous);
            NotifyAuthenticationStateChanged(authState);
        }
    }
}