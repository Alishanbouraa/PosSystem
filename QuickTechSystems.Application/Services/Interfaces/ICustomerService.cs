using QuickTechSystems.Application.DTOs;

namespace QuickTechSystems.Application.Services.Interfaces
{
    public interface ICustomerService : IBaseService<CustomerDTO>
    {
        Task<IEnumerable<CustomerDTO>> GetByNameAsync(string name);
        Task<bool> UpdateBalanceAsync(int customerId, decimal amount);
        Task<IEnumerable<CustomerDTO>> GetDebtorsAsync();
    }
}