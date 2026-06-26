using System.Collections.Generic;
using System.Threading.Tasks;
using Eshop.Application.DTOs;

namespace Eshop.Application.Services
{
    public interface IProductReviewService
    {
        // Φέρνει τις κριτικές ενός προϊόντος μαζί με τον Μέσο Όρο και το Συνολικό Πλήθος
        Task<ProductReviewContainerDto> GetProductReviewsAsync(int productId);

        // Προσθέτει μια νέα κριτική (αφού ελέγξει αν ο πελάτης είναι αγοραστής)
        Task<bool> AddReviewAsync(int productId, int customerId, CreateReviewDto dto);
    }
}