using System.Collections.ObjectModel;
using System.Windows.Input;
using QuickTechSystems.Application.DTOs;
using QuickTechSystems.Application.Services.Interfaces;
using QuickTechSystems.WPF.Commands;

namespace QuickTechSystems.WPF.ViewModels
{
    public class SupplierViewModel : ViewModelBase
    {
        private readonly ISupplierService _supplierService;
        private ObservableCollection<SupplierDTO> _suppliers;
        private ObservableCollection<SupplierTransactionDTO> _supplierTransactions;
        private SupplierDTO? _selectedSupplier;
        private bool _isEditing;
        private string _searchText = string.Empty;
        private decimal _paymentAmount;
        private string _notes = string.Empty;

        public ObservableCollection<SupplierDTO> Suppliers
        {
            get => _suppliers;
            set => SetProperty(ref _suppliers, value);
        }

        public ObservableCollection<SupplierTransactionDTO> SupplierTransactions
        {
            get => _supplierTransactions;
            set => SetProperty(ref _supplierTransactions, value);
        }

        public SupplierDTO? SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                if (SetProperty(ref _selectedSupplier, value))
                {
                    IsEditing = value != null;
                    _ = LoadSupplierTransactionsAsync();
                }
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
                _ = SearchSuppliersAsync();
            }
        }

        public decimal PaymentAmount
        {
            get => _paymentAmount;
            set => SetProperty(ref _paymentAmount, value);
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public ICommand LoadCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand AddPaymentCommand { get; }

        public SupplierViewModel(ISupplierService supplierService)
        {
            _supplierService = supplierService;
            _suppliers = new ObservableCollection<SupplierDTO>();
            _supplierTransactions = new ObservableCollection<SupplierTransactionDTO>();

            LoadCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
            AddCommand = new RelayCommand(_ => AddNew());
            SaveCommand = new AsyncRelayCommand(async _ => await SaveAsync());
            DeleteCommand = new AsyncRelayCommand(async _ => await DeleteAsync());
            AddPaymentCommand = new AsyncRelayCommand(async _ => await AddPaymentAsync());

            _ = LoadDataAsync();
        }

        protected override async Task LoadDataAsync()
        {
            try
            {
                var suppliers = await _supplierService.GetAllAsync();
                Suppliers = new ObservableCollection<SupplierDTO>(suppliers);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading suppliers: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadSupplierTransactionsAsync()
        {
            if (SelectedSupplier == null) return;

            try
            {
                var transactions = await _supplierService.GetSupplierTransactionsAsync(SelectedSupplier.SupplierId);
                SupplierTransactions = new ObservableCollection<SupplierTransactionDTO>(transactions);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading transactions: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddNew()
        {
            SelectedSupplier = new SupplierDTO
            {
                IsActive = true,
                Balance = 0
            };
        }

        private async Task SaveAsync()
        {
            try
            {
                if (SelectedSupplier == null) return;

                if (string.IsNullOrWhiteSpace(SelectedSupplier.Name))
                {
                    MessageBox.Show("Supplier name is required.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (SelectedSupplier.SupplierId == 0)
                {
                    await _supplierService.CreateAsync(SelectedSupplier);
                }
                else
                {
                    await _supplierService.UpdateAsync(SelectedSupplier);
                }

                await LoadDataAsync();
                MessageBox.Show("Supplier saved successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving supplier: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteAsync()
        {
            try
            {
                if (SelectedSupplier == null) return;

                if (MessageBox.Show("Are you sure you want to delete this supplier?",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    await _supplierService.DeleteAsync(SelectedSupplier.SupplierId);
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting supplier: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SearchSuppliersAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    await LoadDataAsync();
                    return;
                }

                var suppliers = await _supplierService.GetByNameAsync(SearchText);
                Suppliers = new ObservableCollection<SupplierDTO>(suppliers);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching suppliers: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task AddPaymentAsync()
        {
            try
            {
                if (SelectedSupplier == null || PaymentAmount <= 0)
                {
                    MessageBox.Show("Please select a supplier and enter a valid payment amount.",
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var transaction = new SupplierTransactionDTO
                {
                    SupplierId = SelectedSupplier.SupplierId,
                    Amount = -PaymentAmount, // Negative amount for payment
                    TransactionType = "Payment",
                    Notes = Notes,
                    TransactionDate = DateTime.Now
                };

                await _supplierService.AddTransactionAsync(transaction);
                await LoadSupplierTransactionsAsync();

                PaymentAmount = 0;
                Notes = string.Empty;

                MessageBox.Show("Payment recorded successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error recording payment: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}