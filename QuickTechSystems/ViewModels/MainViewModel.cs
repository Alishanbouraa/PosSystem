using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using QuickTechSystems.WPF.Commands;

namespace QuickTechSystems.WPF.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IServiceProvider _serviceProvider;
        private ViewModelBase? _currentViewModel;

        public ViewModelBase? CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public ICommand NavigateCommand { get; }

        public MainViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            NavigateCommand = new RelayCommand(ExecuteNavigation);
            ExecuteNavigation("Products"); // Default view
        }

        private void ExecuteNavigation(object? parameter)
        {
            try
            {
                if (parameter is string destination)
                {
                    CurrentViewModel = destination switch
                    {
                        "Products" => _serviceProvider.GetRequiredService<ProductViewModel>(),
                        "Categories" => _serviceProvider.GetRequiredService<CategoryViewModel>(),
                        "Customers" => _serviceProvider.GetRequiredService<CustomerViewModel>(),
                        "Transactions" => _serviceProvider.GetRequiredService<TransactionViewModel>(),
                        "Settings" => _serviceProvider.GetRequiredService<SettingsViewModel>(),
                        "Suppliers" => _serviceProvider.GetRequiredService<SupplierViewModel>(),
                        "Inventory" => _serviceProvider.GetRequiredService<InventoryViewModel>(),
                        "TransactionHistory" => _serviceProvider.GetRequiredService<TransactionHistoryViewModel>(),
                        "Debts" => _serviceProvider.GetRequiredService<DebtViewModel>(),
                        "Profit" => _serviceProvider.GetRequiredService<ProfitViewModel>(),
                        _ => CurrentViewModel
                    };

                    StatusMessage = $"Navigated to {destination}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Navigation failed";
                MessageBox.Show($"Navigation Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }
    }
}