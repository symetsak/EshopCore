using AutoMapper;
using Eshop.Core.Entities;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Eshop.Application.DTOs
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // 1. Από το DTO που μας έρχεται (Request) -> στο αληθινό Entity της βάσης
            CreateMap<ProductCreateDto, Product>();

            // 2. Από το Entity της βάσης -> στο DTO που θα επιστρέψουμε (Response)
            CreateMap<Product, ProductResponseDto>();
        }
    }
}