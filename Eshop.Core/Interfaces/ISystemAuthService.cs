using Eshop.Core.DTOs;

namespace Eshop.Core.Interfaces
{
    public interface ISystemAuthService
    {
        // Επιστρέφει το JWT token αν το login είναι σωστό, αλλιώς null
        string? Login(LoginRequestDto request);
    }
}
