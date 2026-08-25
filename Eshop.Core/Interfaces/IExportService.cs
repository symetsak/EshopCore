using Eshop.Application.DTOs;
using Eshop.Core.DTOs;

namespace Eshop.Application.Services
{
    public interface IExportService
    {
        byte[] GenerateOrdersExcel(IEnumerable<OrderResponseDto> orders);
        byte[] GenerateOrdersPdf(IEnumerable<OrderResponseDto> orders);
        byte[] GenerateReturnsExcel(IEnumerable<OrderReturnResponseDto> returns);
        byte[] GenerateReturnsPdf(IEnumerable<OrderReturnResponseDto> returns);
        byte[] GenerateProductsExcel(IEnumerable<ProductResponseDto> products); 
        byte[] GenerateProductsPdf(IEnumerable<ProductResponseDto> products);
    }
}