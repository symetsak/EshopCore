using AutoMapper;
using Eshop.Core.Entities;
using Eshop.Core.DTOs;

namespace Eshop.Application.DTOs
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // 1. Users
            CreateMap<User, LoginResponseDto>()
                .ForMember(dest => dest.Token, opt => opt.Ignore())
                .ForMember(dest => dest.RefreshToken, opt => opt.Ignore());

            CreateMap<User, UserResponseDto>();

            // 2. Products
            CreateMap<ProductCreateDto, Product>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.SalePrice, opt => opt.Ignore())
                .ForMember(dest => dest.SaleStartDate, opt => opt.Ignore())
                .ForMember(dest => dest.SaleEndDate, opt => opt.Ignore())
                .ForMember(dest => dest.ImageUrl, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore());

            CreateMap<Product, ProductResponseDto>()
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.CurrentPrice))
                .ForMember(dest => dest.OriginalPrice, opt => opt.MapFrom(src => src.Price))
                .ForMember(dest => dest.SalePrice, opt => opt.MapFrom(src =>
                    (src.SalePrice.HasValue && src.SaleEndDate.HasValue && src.SaleEndDate.Value < DateTime.UtcNow)
                    ? null
                    : src.SalePrice))
                .ReverseMap();

            // 3. Categories
            CreateMap<CategoryCreateDto, Category>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Products, opt => opt.Ignore());

            CreateMap<Category, CategoryResponseDto>();

            // 4. Customers 
            CreateMap<CustomerRegisterDto, Customer>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.RefreshToken, opt => opt.Ignore())
                .ForMember(dest => dest.RefreshTokenExpiry, opt => opt.Ignore())
                .ForMember(dest => dest.Orders, opt => opt.Ignore());

            CreateMap<Customer, CustomerProfileDto>();

            CreateMap<Customer, CustomerAuthResponseDto>()
                .ForMember(dest => dest.Token, opt => opt.Ignore());

            // 5. Orders
            CreateMap<Order, OrderResponseDto>()
                 .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? $"{src.Customer.FirstName} {src.Customer.LastName}" : string.Empty))
                 .ForMember(dest => dest.CustomerEmail, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Email : string.Empty))
                 .ForMember(dest => dest.CustomerPhone, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Phone : string.Empty));

            CreateMap<OrderItem, OrderItemResponseDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : string.Empty));

            // 6. Product Reviews
            CreateMap<ProductReview, ReviewResponseDto>();

            // 7. Wishlist Mappings
            CreateMap<Wishlist, WishlistResponseDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : string.Empty))
                .ForMember(dest => dest.ProductPrice, opt => opt.MapFrom(src => src.Product != null ? src.Product.CurrentPrice : 0))
                .ForMember(dest => dest.ProductImageUrl, opt => opt.MapFrom(src => src.Product != null ? src.Product.ImageUrl : null));

            // 8. Notification Mappings
            CreateMap<Notification, NotificationResponseDto>();

            // 9. Order Return Mappings
            CreateMap<OrderReturn, OrderReturnResponseDto>();

            CreateMap<OrderReturnItem, OrderReturnItemResponseDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : "Άγνωστο Προϊόν"));
        }
    }
}