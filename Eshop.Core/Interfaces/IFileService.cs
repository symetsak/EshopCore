using Microsoft.AspNetCore.Http;

namespace Eshop.Core.Interfaces
{
    public interface IFileService
    {
        // Επιστρέφει το σχετικό URL της εικόνας (π.χ. "/uploads/adidas-store/products/name.jpg")
        Task<string> SaveProductImageAsync(IFormFile file, string tenantId);

        // Χρήσιμο για το μέλλον αν ο Admin διαγράψει ένα προϊόν, να σβήνουμε και το αρχείο!
        void DeleteImage(string relativePath);
    }
}