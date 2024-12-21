using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QuickTechSystems.Application.DTOs;
using QuickTechSystems.Application.Services.Interfaces;
using QuickTechSystems.Domain.Entities;
using QuickTechSystems.Domain.Interfaces.Repositories;

namespace QuickTechSystems.Application.Services
{
    public class CustomerService : BaseService<Customer, CustomerDTO>, ICustomerService
    {
        public CustomerService(IUnitOfWork unitOfWork, IMapper mapper)
            : base(unitOfWork, mapper, unitOfWork.Customers)
        {
        }

        public async Task<IEnumerable<CustomerDTO>> GetByNameAsync(string name)
        {
            var customers = await _repository.Query()
                .Where(c => c.Name.Contains(name))
                .ToListAsync();
            return _mapper.Map<IEnumerable<CustomerDTO>>(customers);
        }

        public async Task<bool> UpdateBalanceAsync(int customerId, decimal amount)
        {
            var customer = await _repository.GetByIdAsync(customerId);
            if (customer == null) return false;

            customer.Balance += amount;
            await _repository.UpdateAsync(customer);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<CustomerDTO>> GetDebtorsAsync()
        {
            var customers = await _repository.Query()
                .Where(c => c.Balance > 0)
                .ToListAsync();
            return _mapper.Map<IEnumerable<CustomerDTO>>(customers);
        }
    }
}