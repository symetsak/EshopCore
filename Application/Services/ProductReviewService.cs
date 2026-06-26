using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Eshop.Application.DTOs;
using Eshop.Core.Entities;
using Eshop.Core.Interfaces;

namespace Eshop.Application.Services
{
    public class ProductReviewService : IProductReviewService
    {
        private readonly IProductReviewRepository _reviewRepo;
        private readonly IMapper _mapper;

        // Κάνουμε Inject το Repository και τον AutoMapper
        public ProductReviewService(IProductReviewRepository reviewRepo, IMapper mapper)
        {
            _reviewRepo = reviewRepo;
            _mapper = mapper;
        }

        // Α) Λήψη κριτικών και υπολογισμός Μέσου Όρου / Πλήθους
        public async Task<ProductReviewContainerDto> GetProductReviewsAsync(int productId)
        {
            var reviews = await _reviewRepo.GetByProductIdAsync(productId);

            double average = 0.0;
            int total = reviews.Count();

            if (total > 0)
            {
                // Υπολογισμός του μέσου όρου (π.χ. 4.6) με στρογγυλοποίηση σε 1 δεκαδικό
                average = Math.Round(reviews.Average(r => r.Rating), 1);
            }

            return new ProductReviewContainerDto
            {
                AverageRating = average,
                TotalReviews = total,
                // Μετατροπή των Entities σε DTOs μέσω AutoMapper
                Reviews = _mapper.Map<IEnumerable<ReviewResponseDto>>(reviews)
            };
        }

        // Β) Προσθήκη κριτικής με έλεγχο Verified Buyer
        public async Task<bool> AddReviewAsync(int productId, int customerId, CreateReviewDto dto)
        {
            // 1. Έλεγχος αν ο πελάτης έχει όντως αγοράσει το προϊόν
            bool hasPurchased = await _reviewRepo.HasCustomerPurchasedProductAsync(customerId, productId);

            if (!hasPurchased)
            {
                // Αν δεν το έχει αγοράσει, πετάμε ένα Exception το οποίο θα πιάσει το Middleware μας
                throw new InvalidOperationException("Μόνο οι αγοραστές του προϊόντος μπορούν να υποβάλουν κριτική.");
            }

            // 2. Δημιουργία του Entity
            var review = new ProductReview
            {
                ProductId = productId,
                CustomerId = customerId,
                Rating = dto.Rating,
                Title = dto.Title,
                Comment = dto.Comment,
                IsApproved = true, // Εμφάνιση κατευθείαν στο site, όπως το ζήτησες!
                CreatedAt = DateTime.UtcNow
            };

            // 3. Αποθήκευση στη βάση δεδομένων του Tenant
            await _reviewRepo.AddAsync(review);
            return await _reviewRepo.SaveChangesAsync();
        }
    }
}