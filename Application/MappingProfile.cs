using AutoMapper;
using Eshop.Core.Entities;
using Eshop.Core.DTOs;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Eshop.Application.DTOs
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<ProductCreateDto, Eshop.Core.Entities.Product>();
            CreateMap<Eshop.Core.Entities.Product, ProductResponseDto>();
            CreateMap<CategoryCreateDto, Eshop.Core.Entities.Category>();
            CreateMap<Eshop.Core.Entities.Category, CategoryResponseDto>();
            CreateMap<CustomerRegisterDto, Eshop.Core.Entities.Customer>();
            CreateMap<Eshop.Core.Entities.Customer, CustomerAuthResponseDto>();
            CreateMap<Order, OrderResponseDto>();
            CreateMap<OrderItem, OrderItemResponseDto>().ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : string.Empty)); // κανόνας για να γεμίζει το ProductName απευθείας από το Line Item
        }
    }
}