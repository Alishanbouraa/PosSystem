using QuickTechSystems.Application.DTOs;
using QuickTechSystems.Domain.Enums;

namespace QuickTechSystems.Application.Services.Interfaces
{
    public interface ITransactionService : IBaseService<TransactionDTO>
    {
        Task<IEnumerable<TransactionDTO>> GetByCustomerAsync(int customerId);
        Task<IEnumerable<TransactionDTO>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<TransactionDTO>> GetByTypeAsync(TransactionType type);
        Task<bool> UpdateStatusAsync(int id, TransactionStatus status);
        Task<decimal> GetTotalSalesAsync(DateTime startDate, DateTime endDate);
        Task<TransactionDTO> ProcessSaleAsync(TransactionDTO transaction);
        Task<TransactionDTO> ProcessRefundAsync(TransactionDTO transaction);
        Task<TransactionDTO?> GetLastCompletedTransactionAsync();
    }
}