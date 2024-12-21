using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QuickTechSystems.Application.DTOs;
using QuickTechSystems.Application.Services.Interfaces;
using QuickTechSystems.Domain.Entities;
using QuickTechSystems.Domain.Enums;
using QuickTechSystems.Domain.Interfaces.Repositories;

namespace QuickTechSystems.Application.Services
{
    public class ProductService : BaseService<Product, ProductDTO>, IProductService
    {
        public ProductService(IUnitOfWork unitOfWork, IMapper mapper)
            : base(unitOfWork, mapper, unitOfWork.Products)
        {
        }

        public override async Task<ProductDTO> CreateAsync(ProductDTO dto)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var entity = _mapper.Map<Product>(dto);

                // Ensure unique barcode
                if (!string.IsNullOrEmpty(entity.Barcode))
                {
                    var existingProduct = await _repository.Query()
                        .FirstOrDefaultAsync(p => p.Barcode == entity.Barcode);

                    if (existingProduct != null)
                    {
                        throw new InvalidOperationException("A product with this barcode already exists.");
                    }
                }

                // Validate category exists
                var category = await _unitOfWork.Categories.GetByIdAsync(entity.CategoryId);
                if (category == null)
                {
                    throw new InvalidOperationException("Selected category does not exist.");
                }

                // Validate supplier if specified
                if (entity.SupplierId.HasValue)
                {
                    var supplier = await _unitOfWork.Suppliers.GetByIdAsync(entity.SupplierId.Value);
                    if (supplier == null)
                    {
                        throw new InvalidOperationException("Selected supplier does not exist.");
                    }
                }

                var result = await _repository.AddAsync(entity);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                // Load related entities for the response
                var productWithRelations = await _repository.Query()
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .FirstOrDefaultAsync(p => p.ProductId == result.ProductId);

                return _mapper.Map<ProductDTO>(productWithRelations);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public override async Task UpdateAsync(ProductDTO dto)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var existingProduct = await _repository.Query()
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .FirstOrDefaultAsync(p => p.ProductId == dto.ProductId);

                if (existingProduct == null)
                {
                    throw new InvalidOperationException("Product not found.");
                }

                // Check barcode uniqueness if changed
                if (!string.IsNullOrEmpty(dto.Barcode) &&
                    dto.Barcode != existingProduct.Barcode)
                {
                    var duplicateProduct = await _repository.Query()
                        .FirstOrDefaultAsync(p => p.Barcode == dto.Barcode);

                    if (duplicateProduct != null)
                    {
                        throw new InvalidOperationException("A product with this barcode already exists.");
                    }
                }

                // Update the entity
                _mapper.Map(dto, existingProduct);
                existingProduct.UpdatedAt = DateTime.Now;

                await _repository.UpdateAsync(existingProduct);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public override async Task<IEnumerable<ProductDTO>> GetAllAsync()
        {
            var products = await _repository.Query()
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProductDTO>>(products);
        }

        public async Task<ProductDTO?> GetByBarcodeAsync(string barcode)
        {
            if (string.IsNullOrEmpty(barcode))
                return null;

            var product = await _repository.Query()
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(p => p.Barcode == barcode && p.IsActive);

            return _mapper.Map<ProductDTO>(product);
        }

        public async Task<IEnumerable<ProductDTO>> GetByCategoryAsync(int categoryId)
        {
            var products = await _repository.Query()
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Where(p => p.CategoryId == categoryId && p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProductDTO>>(products);
        }

        public async Task<bool> UpdateStockAsync(int productId, int quantity)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var product = await _repository.GetByIdAsync(productId);
                if (product == null) return false;

                // Prevent negative stock unless explicitly allowed
                if (product.CurrentStock + quantity < 0)
                {
                    throw new InvalidOperationException("Operation would result in negative stock.");
                }

                product.CurrentStock += quantity;
                product.UpdatedAt = DateTime.Now;

                await _repository.UpdateAsync(product);

                // Add inventory history
                var history = new InventoryHistory
                {
                    ProductId = productId,
                    QuantityChanged = quantity,
                    Date = DateTime.Now,
                    OperationType = quantity > 0 ? TransactionType.Adjustment : TransactionType.Adjustment,
                    Reference = "Stock Adjustment"
                };

                await _unitOfWork.Context.Set<InventoryHistory>().AddAsync(history);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<ProductDTO>> GetLowStockProductsAsync()
        {
            var products = await _repository.Query()
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Where(p => p.IsActive && p.CurrentStock <= p.MinimumStock)
                .OrderBy(p => p.CurrentStock)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProductDTO>>(products);
        }

        public async Task<bool> CanDeleteProductAsync(int productId)
        {
            // Check if product has any transactions or inventory history
            var hasTransactions = await _unitOfWork.Context.Set<TransactionDetail>()
                .AnyAsync(td => td.ProductId == productId);

            var hasInventoryHistory = await _unitOfWork.Context.Set<InventoryHistory>()
                .AnyAsync(ih => ih.ProductId == productId);

            return !hasTransactions && !hasInventoryHistory;
        }
    }
}