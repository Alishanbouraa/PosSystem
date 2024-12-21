using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using QuickTechSystems.Domain.Entities;
using QuickTechSystems.Domain.Interfaces.Repositories;
using QuickTechSystems.Infrastructure.Data;

namespace QuickTechSystems.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private readonly ConcurrentDictionary<Type, object> _repositories;
        private bool _disposed;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            _repositories = new ConcurrentDictionary<Type, object>();
        }

        public IGenericRepository<Product> Products =>
            (IGenericRepository<Product>)_repositories.GetOrAdd(
                typeof(Product),
                _ => new GenericRepository<Product>(_context));

        public IGenericRepository<Category> Categories =>
            (IGenericRepository<Category>)_repositories.GetOrAdd(
                typeof(Category),
                _ => new GenericRepository<Category>(_context));

        public IGenericRepository<Customer> Customers =>
            (IGenericRepository<Customer>)_repositories.GetOrAdd(
                typeof(Customer),
                _ => new GenericRepository<Customer>(_context));

        public IGenericRepository<Transaction> Transactions =>
            (IGenericRepository<Transaction>)_repositories.GetOrAdd(
                typeof(Transaction),
                _ => new GenericRepository<Transaction>(_context));

        public IGenericRepository<BusinessSetting> BusinessSettings =>
            (IGenericRepository<BusinessSetting>)_repositories.GetOrAdd(
                typeof(BusinessSetting),
                _ => new GenericRepository<BusinessSetting>(_context));

        public IGenericRepository<SystemPreference> SystemPreferences =>
            (IGenericRepository<SystemPreference>)_repositories.GetOrAdd(
                typeof(SystemPreference),
                _ => new GenericRepository<SystemPreference>(_context));

        public IGenericRepository<Supplier> Suppliers =>
            (IGenericRepository<Supplier>)_repositories.GetOrAdd(
                typeof(Supplier),
                _ => new GenericRepository<Supplier>(_context));

        public DbContext Context => _context;

        public async Task<int> SaveChangesAsync()
        {
            try
            {
                return await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Log the exception details
                throw new Exception("A concurrency error occurred while saving changes.", ex);
            }
            catch (Exception ex)
            {
                // Log the exception details
                throw new Exception("An error occurred while saving changes.", ex);
            }
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            try
            {
                return await _context.Database.BeginTransactionAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error starting database transaction.", ex);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _context.Dispose();
                foreach (var repository in _repositories.Values)
                {
                    if (repository is IDisposable disposableRepo)
                    {
                        disposableRepo.Dispose();
                    }
                }
                _repositories.Clear();
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~UnitOfWork()
        {
            Dispose(false);
        }
    }
}