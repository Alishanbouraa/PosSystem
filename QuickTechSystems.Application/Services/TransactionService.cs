using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickTechSystems.Application.DTOs;
using QuickTechSystems.Application.Services.Interfaces;
using QuickTechSystems.Domain.Entities;
using QuickTechSystems.Domain.Enums;
using QuickTechSystems.Domain.Interfaces.Repositories;

namespace QuickTechSystems.Application.Services
{
    public class TransactionService : BaseService<Transaction, TransactionDTO>, ITransactionService
    {

        private readonly IProductService _productService;

        public TransactionService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IProductService productService)
            : base(unitOfWork, mapper, unitOfWork.Transactions)
        {
            _productService = productService;
        }

        public async Task<IEnumerable<TransactionDTO>> GetByCustomerAsync(int customerId)
        {
            var transactions = await _repository.Query()
                .AsNoTracking()
                .Include(t => t.Customer)
                .Include(t => t.TransactionDetails)
                .ThenInclude(td => td.Product)
                .Where(t => t.CustomerId == customerId)
                .ToListAsync();
            return _mapper.Map<IEnumerable<TransactionDTO>>(transactions);
        }

        public async Task<TransactionDTO?> GetLastCompletedTransactionAsync()
        {
            try
            {
                var lastTransaction = await _repository.Query()
                    .AsNoTracking()
                    .Include(t => t.Customer)
                    .Include(t => t.TransactionDetails)
                        .ThenInclude(td => td.Product)
                    .Where(t => t.Status == TransactionStatus.Completed)
                    .OrderByDescending(t => t.TransactionDate)
                    .FirstOrDefaultAsync();

                return _mapper.Map<TransactionDTO>(lastTransaction);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving last transaction: {ex.Message}");
            }
        }

        public async Task<IEnumerable<TransactionDTO>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                // Create a new query each time
                var query = _repository.Query()
                    .AsNoTracking()  // Use AsNoTracking for read-only operations
                    .Include(t => t.Customer)
                    .Include(t => t.TransactionDetails)
                        .ThenInclude(td => td.Product)
                    .Where(t => t.TransactionDate >= startDate &&
                               t.TransactionDate <= endDate &&
                               t.Status == TransactionStatus.Completed)
                    .OrderByDescending(t => t.TransactionDate);

                // Execute the query
                var transactions = await query.ToListAsync().ConfigureAwait(false);

                // Map the results
                var result = _mapper.Map<IEnumerable<TransactionDTO>>(transactions);

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving transactions: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<TransactionDTO>> GetByTypeAsync(TransactionType type)
        {
            var transactions = await _repository.Query()
                .AsNoTracking()
                .Include(t => t.Customer)
                .Include(t => t.TransactionDetails)
                .ThenInclude(td => td.Product)
                .Where(t => t.TransactionType == type)
                .ToListAsync();
            return _mapper.Map<IEnumerable<TransactionDTO>>(transactions);
        }

        public async Task<bool> UpdateStatusAsync(int id, TransactionStatus status)
        {
            var transaction = await _repository.GetByIdAsync(id);
            if (transaction == null) return false;

            transaction.Status = status;
            await _repository.UpdateAsync(transaction);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> GetTotalSalesAsync(DateTime startDate, DateTime endDate)
        {
            return await _repository.Query()
                .Where(t => t.TransactionType == TransactionType.Sale
                       && t.Status == TransactionStatus.Completed
                       && t.TransactionDate >= startDate
                       && t.TransactionDate <= endDate)
                .SumAsync(t => t.TotalAmount);
        }

        public async Task<TransactionDTO> ProcessSaleAsync(TransactionDTO transactionDto)
        {
            using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Validate stock levels
                foreach (var detail in transactionDto.Details)
                {
                    var product = await _productService.GetByIdAsync(detail.ProductId);
                    if (product == null || product.CurrentStock < detail.Quantity)
                    {
                        throw new InvalidOperationException($"Insufficient stock for product: {product?.Name ?? "Unknown"}");
                    }
                }

                var transaction = _mapper.Map<Transaction>(transactionDto);
                transaction.TransactionDetails = transactionDto.Details.Select(d => new TransactionDetail
                {
                    ProductId = d.ProductId,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Discount = d.Discount
                }).ToList();

                await _repository.AddAsync(transaction);
                await _unitOfWork.SaveChangesAsync();

                // Update stock levels
                foreach (var detail in transactionDto.Details)
                {
                    await _productService.UpdateStockAsync(detail.ProductId, -detail.Quantity);

                    var inventoryHistory = new InventoryHistory
                    {
                        ProductId = detail.ProductId,
                        QuantityChanged = -detail.Quantity,
                        OperationType = TransactionType.Sale,
                        Date = DateTime.Now,
                        Reference = $"Sale-{transaction.TransactionId}",
                        Notes = $"Sale transaction #{transaction.TransactionId}"
                    };

                    await _unitOfWork.Context.Set<InventoryHistory>().AddAsync(inventoryHistory);
                }

                // Update customer balance if applicable
                if (transaction.CustomerId.HasValue)
                {
                    var customer = await _unitOfWork.Customers.GetByIdAsync(transaction.CustomerId.Value);
                    if (customer != null)
                    {
                        customer.Balance += transaction.Balance;
                        await _unitOfWork.Customers.UpdateAsync(customer);
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return _mapper.Map<TransactionDTO>(transaction);
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<TransactionDTO> ProcessRefundAsync(TransactionDTO transactionDto)
        {
            using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                transactionDto.TransactionType = TransactionType.Return;
                transactionDto.TransactionDate = DateTime.Now;
                transactionDto.Status = TransactionStatus.Completed;

                var transaction = _mapper.Map<Transaction>(transactionDto);
                await _repository.AddAsync(transaction);
                await _unitOfWork.SaveChangesAsync();

                // Update stock levels
                foreach (var detail in transactionDto.Details)
                {
                    await _productService.UpdateStockAsync(detail.ProductId, detail.Quantity);
                }

                // Update customer balance if applicable
                if (transaction.CustomerId.HasValue)
                {
                    var customer = await _unitOfWork.Customers.GetByIdAsync(transaction.CustomerId.Value);
                    if (customer != null)
                    {
                        customer.Balance += transaction.Balance;
                        await _unitOfWork.Customers.UpdateAsync(customer);
                        await _unitOfWork.SaveChangesAsync();
                    }
                }

                await dbTransaction.CommitAsync();
                return _mapper.Map<TransactionDTO>(transaction);
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public override async Task<IEnumerable<TransactionDTO>> GetAllAsync()
        {
            var transactions = await _repository.Query()
                .AsNoTracking()
                .Include(t => t.Customer)
                .Include(t => t.TransactionDetails)
                    .ThenInclude(td => td.Product)
                .ToListAsync();

            return _mapper.Map<IEnumerable<TransactionDTO>>(transactions);
        }
    }
}