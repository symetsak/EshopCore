using System.Collections.Generic;
using System.Threading.Tasks;
using Eshop.Core.Entities;

namespace Eshop.Core.Interfaces
{
    public interface IProductReviewRepository
    {
        // Φέρνει όλες τις εγκεκριμένες κριτικές για ένα συγκεκριμένο προϊόν
        Task<IEnumerable<ProductReview>> GetByProductIdAsync(int productId);

        // Προσθέτει μια νέα κριτική στη βάση
        Task AddAsync(ProductReview review);

        // ΕΛΕΓΧΟΣ: Επιστρέφει true αν ο πελάτης έχει αγοράσει το προϊόν και η παραγγελία είναι ολοκληρωμένη (Paid)
        Task<bool> HasCustomerPurchasedProductAsync(int customerId, int productId);

        // Αποθηκεύει τις αλλαγές στη βάση (SaveChanges)
        Task<bool> SaveChangesAsync();
    }
}