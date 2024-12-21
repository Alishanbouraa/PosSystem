using System.Collections.ObjectModel;
using System.Windows.Input;
using QuickTechSystems.Application.DTOs;
using QuickTechSystems.Application.Services.Interfaces;
using QuickTechSystems.WPF.Commands;

namespace QuickTechSystems.WPF.ViewModels
{
    public class CustomerViewModel : ViewModelBase
    {
        private readonly ICustomerService _customerService;
        private ObservableCollection<CustomerDTO> _customers;
        private CustomerDTO? _selectedCustomer;
        private bool _isEditing;
        private string _searchText = string.Empty;

        public ObservableCollection<CustomerDTO> Customers
        {
            get => _customers;
            set => SetProperty(ref _customers, value);
        }

        public CustomerDTO? SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                SetProperty(ref _selectedCustomer, value);
                IsEditing = value != null;
            }
        }

        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                _ = SearchCustomersAsync();
            }
        }

        public ICommand LoadCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand SearchCommand { get; }

        public CustomerViewModel(ICustomerService customerService)
        {
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _customers = new ObservableCollection<CustomerDTO>();

            LoadCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
            AddCommand = new RelayCommand(_ => AddNew());
            SaveCommand = new AsyncRelayCommand(async _ => await SaveAsync());
            DeleteCommand = new AsyncRelayCommand(async _ => await DeleteAsync());
            SearchCommand = new AsyncRelayCommand(async _ => await SearchCustomersAsync());

            _ = LoadDataAsync();
        }

        protected override async Task LoadDataAsync()
        {
            try
            {
                var customers = await _customerService.GetAllAsync();
                Customers = new ObservableCollection<CustomerDTO>(customers);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading customers: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddNew()
        {
            SelectedCustomer = new CustomerDTO
            {
                IsActive = true,
                Balance = 0
            };
        }

        private async Task SaveAsync()
        {
            try
            {
                if (SelectedCustomer == null) return;

                if (string.IsNullOrWhiteSpace(SelectedCustomer.Name))
                {
                    MessageBox.Show("Customer name is required.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (SelectedCustomer.CustomerId == 0)
                {
                    await _customerService.CreateAsync(SelectedCustomer);
                }
                else
                {
                    await _customerService.UpdateAsync(SelectedCustomer);
                }

                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving customer: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteAsync()
        {
            try
            {
                if (SelectedCustomer == null) return;

                if (MessageBox.Show("Are you sure you want to delete this customer?",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    await _customerService.DeleteAsync(SelectedCustomer.CustomerId);
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting customer: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SearchCustomersAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    await LoadDataAsync();
                    return;
                }

                var customers = await _customerService.GetByNameAsync(SearchText);
                Customers = new ObservableCollection<CustomerDTO>(customers);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching customers: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}