using Microsoft.EntityFrameworkCore.Storage;
using QuickTechSystems.Domain.Entities;

namespace QuickTechSystems.Domain.Interfaces.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<Product> Products { get; }
        IGenericRepository<Category> Categories { get; }
        IGenericRepository<Customer> Customers { get; }
        IGenericRepository<Transaction> Transactions { get; }
        IGenericRepository<BusinessSetting> BusinessSettings { get; }
        IGenericRepository<SystemPreference> SystemPreferences { get; }
        IGenericRepository<Supplier> Suppliers { get; }
        DbContext Context { get; }
        Task<int> SaveChangesAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}