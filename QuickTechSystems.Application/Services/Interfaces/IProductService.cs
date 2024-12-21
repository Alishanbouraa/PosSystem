using QuickTechSystems.Application.DTOs;

namespace QuickTechSystems.Application.Services.Interfaces
{
    public interface IProductService : IBaseService<ProductDTO>
    {
        Task<ProductDTO?> GetByBarcodeAsync(string barcode);
        Task<IEnumerable<ProductDTO>> GetByCategoryAsync(int categoryId);
        Task<bool> UpdateStockAsync(int productId, int quantity);
        Task<IEnumerable<ProductDTO>> GetLowStockProductsAsync();
        Task<bool> CanDeleteProductAsync(int productId); // Add this line
    }
}