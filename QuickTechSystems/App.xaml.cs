using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuickTechSystems.Application.Mappings;
using QuickTechSystems.Application.Services;
using QuickTechSystems.Application.Services.Interfaces;
using QuickTechSystems.Domain.Interfaces.Repositories;
using QuickTechSystems.Infrastructure.Data;
using QuickTechSystems.Infrastructure.Repositories;
using QuickTechSystems.WPF.ViewModels;
using QuickTechSystems.WPF.Views;
using System.Windows;

namespace QuickTechSystems.WPF
{
    public partial class App : System.Windows.Application
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public App()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Database
            services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(

            _configuration.GetConnectionString("DefaultConnection"),
            b => b.MigrationsAssembly("QuickTechSystems.Infrastructure"))
            .EnableSensitiveDataLogging() // Add this for debugging
            .EnableDetailedErrors()); // Add this for debugging
            // AutoMapper
            services.AddAutoMapper(typeof(MappingProfile));

            // Repositories
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Application Services
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<ITransactionService, TransactionService>();
            services.AddScoped<IBusinessSettingsService, BusinessSettingsService>();
            services.AddScoped<ISystemPreferencesService, SystemPreferencesService>();
            services.AddScoped<ISupplierService, SupplierService>();
            services.AddScoped<IBarcodeService, BarcodeService>();
            services.AddScoped<IProfitService, ProfitService>();
            services.AddScoped<ProfitViewModel>();
            // ViewModels
            services.AddScoped<MainViewModel>();
            services.AddScoped<ProductViewModel>();
            services.AddScoped<CategoryViewModel>();
            services.AddScoped<CustomerViewModel>();
            services.AddScoped<TransactionViewModel>();
            services.AddScoped<SettingsViewModel>();
            services.AddScoped<SystemPreferencesViewModel>();
            services.AddScoped<SupplierViewModel>();
            services.AddScoped<InventoryViewModel>();
            services.AddScoped<TransactionHistoryViewModel>();
            // Views
            services.AddTransient<MainWindow>();
            services.AddTransient<ProductView>();
            services.AddTransient<CategoryView>();
            services.AddTransient<CustomerView>();
            services.AddTransient<TransactionView>();
            services.AddTransient<SettingsView>();
            services.AddTransient<SystemPreferencesView>();
            services.AddTransient<SupplierView>();
            services.AddTransient<InventoryView>();
            services.AddTransient<TransactionHistoryView>();
            services.AddScoped<DebtViewModel>();
            services.AddScoped<DebtViewModel>();
            services.AddTransient<DebtView>();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                mainWindow.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();

                // Initialize database
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    await context.Database.MigrateAsync();
                }

                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while starting the application: {ex.Message}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}