using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Eshop.Application.DTOs;
using Eshop.Core.Entities;
using Eshop.Core.Interfaces;

namespace Eshop.Application.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly IWishlistRepository _wishlistRepo;
        private readonly IMapper _mapper;

        // Inject το Repository και τον AutoMapper
        public WishlistService(IWishlistRepository wishlistRepo, IMapper mapper)
        {
            _wishlistRepo = wishlistRepo;
            _mapper = mapper;
        }

        // Λήψη της Wishlist και Mapping σε DTOs
        public async Task<IEnumerable<WishlistResponseDto>> GetCustomerWishlistAsync(int customerId)
        {
            var wishlistItems = await _wishlistRepo.GetByCustomerIdAsync(customerId);
            return _mapper.Map<IEnumerable<WishlistResponseDto>>(wishlistItems);
        }

        // Το Business Logic του Toggle (Προσθήκη / Αφαίρεση)
        public async Task<string> ToggleWishlistAsync(int customerId, int productId)
        {
            // 1. Ελέγχουμε αν το προϊόν υπάρχει ήδη στη Wishlist του συγκεκριμένου πελάτη
            var existingItem = await _wishlistRepo.GetExistingAsync(customerId, productId);

            if (existingItem != null)
            {
                // Αν ΥΠΑΡΧΕΙ, το αφαιρούμε (Remove)
                _wishlistRepo.Remove(existingItem);
                await _wishlistRepo.SaveChangesAsync();
                return "Removed";
            }
            else
            {
                // Αν ΔΕΝ υπάρχει, το δημιουργούμε και το προσθέτουμε (Add)
                var newItem = new Wishlist
                {
                    CustomerId = customerId,
                    ProductId = productId,
                    AddedAt = DateTime.UtcNow
                };

                await _wishlistRepo.AddAsync(newItem);
                await _wishlistRepo.SaveChangesAsync();
                return "Added";
            }
        }
    }
}