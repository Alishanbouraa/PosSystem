using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using QuickTechSystems.Application.DTOs;
using QuickTechSystems.Application.Services.Interfaces;
using QuickTechSystems.Domain.Enums;
using QuickTechSystems.WPF.Commands;
using System.Text;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows.Media;
using System.Globalization;
using System.Windows.Data;
namespace QuickTechSystems.WPF.ViewModels
{
    public class TransactionViewModel : ViewModelBase
    {
        private readonly ITransactionService _transactionService;
        private readonly ICustomerService _customerService;
        private readonly IProductService _productService;
        private string _statusMessage = string.Empty;
        private string _connectionStatus = "Connected";
        private string _cashierName = "Default Cashier";
        private string _terminalNumber = "001";
        // Properties for tracking current transaction
        private string _currentTransactionNumber = string.Empty;
        private TransactionDTO? _currentTransaction;
        private CustomerDTO? _selectedCustomer;
        private decimal _paymentAmount;
        private decimal _changeDue;
        private int _itemCount;
        private decimal _subTotal;
        private decimal _taxAmount;
        private decimal _discountAmount;
        private decimal _totalAmount;

        // Search and input properties
        private string _barcodeText = string.Empty;
        private string _productSearchText = string.Empty;
        private string _customerSearchText = string.Empty;
        private bool _isCustomerSearchVisible;
        private ObservableCollection<CustomerDTO> _filteredCustomers;
        private CustomerDTO? _selectedCustomerFromSearch;
        private bool _isProductSearchVisible;

        private ObservableCollection<ProductDTO> _filteredProducts = new ObservableCollection<ProductDTO>();

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        public string CashierName
        {
            get => _cashierName;
            set => SetProperty(ref _cashierName, value);
        }

        public string TerminalNumber
        {
            get => _terminalNumber;
            set => SetProperty(ref _terminalNumber, value);
        }

        public DateTime CurrentDate => DateTime.Now;

        public ICommand ProcessBarcodeCommand { get; private set; }
        public ICommand? SearchProductsCommand { get; private set; }
        // Collections
        private ObservableCollection<TransactionDTO> _heldTransactions = new();


        public TransactionViewModel(
       ITransactionService transactionService,
       ICustomerService customerService,
       IProductService productService)
       : base()
        {
            _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));

            // Initialize collections
            _filteredProducts = new ObservableCollection<ProductDTO>();
            _filteredCustomers = new ObservableCollection<CustomerDTO>();

            InitializeCommands();
            InitializeCollections();
            StartNewTransaction();

            ProcessBarcodeCommand = new AsyncRelayCommand(async _ => await ProcessBarcodeInput());
            InitializeCustomerCommands();
        }
        private void InitializeCustomerCommands()
        {
            NewCustomerCommand = new AsyncRelayCommand(async _ => await ShowNewCustomerDialog());
        }
        #region Properties
    

        public bool IsProductSearchVisible
        {
            get => _isProductSearchVisible;
            set => SetProperty(ref _isProductSearchVisible, value);
        }


        public string CurrentTransactionNumber
        {
            get => _currentTransactionNumber;
            set => SetProperty(ref _currentTransactionNumber, value);
        }

        public TransactionDTO? CurrentTransaction
        {
            get => _currentTransaction;
            set => SetProperty(ref _currentTransaction, value);
        }

        public CustomerDTO? SelectedCustomer
        {
            get => _selectedCustomer;
            set => SetProperty(ref _selectedCustomer, value);
        }

        public decimal PaymentAmount
        {
            get => _paymentAmount;
            set
            {
                if (SetProperty(ref _paymentAmount, value))
                {
                    UpdateChangeDue();
                }
            }
        }
        private decimal ParsePaymentAmount(string value)
        {
            return decimal.TryParse(value, out decimal result) ? result : 0m;
        }
        public decimal ChangeDue
        {
            get => _changeDue;
            set => SetProperty(ref _changeDue, value);
        }

        public int ItemCount
        {
            get => _itemCount;
            set => SetProperty(ref _itemCount, value);
        }

        public decimal SubTotal
        {
            get => _subTotal;
            set => SetProperty(ref _subTotal, value);
        }

        public decimal TaxAmount
        {
            get => _taxAmount;
            set => SetProperty(ref _taxAmount, value);
        }

        public decimal DiscountAmount
        {
            get => _discountAmount;
            set => SetProperty(ref _discountAmount, value);
        }

        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }
        public string BarcodeText
        {
            get => _barcodeText;
            set => SetProperty(ref _barcodeText, value);
        }

        public string ProductSearchText
        {
            get => _productSearchText;
            set
            {
                if (SetProperty(ref _productSearchText, value))
                {
                    SearchProducts();
                }
            }
        }

        public string CustomerSearchText
        {
            get => _customerSearchText;
            set
            {
                if (SetProperty(ref _customerSearchText, value))
                {
                    SearchCustomers();
                }
            }
        }

        public bool IsCustomerSearchVisible
        {
            get => _isCustomerSearchVisible;
            set => SetProperty(ref _isCustomerSearchVisible, value);
        }

        public ObservableCollection<CustomerDTO> FilteredCustomers
        {
            get => _filteredCustomers;
            set => SetProperty(ref _filteredCustomers, value);
        }

        public CustomerDTO? SelectedCustomerFromSearch
        {
            get => _selectedCustomerFromSearch;
            set
            {
                if (SetProperty(ref _selectedCustomerFromSearch, value) && value != null)
                {
                    SelectedCustomer = value;
                    IsCustomerSearchVisible = false;
                    CustomerSearchText = string.Empty;
                }
            }
        }

        public ObservableCollection<TransactionDTO> HeldTransactions
        {
            get => _heldTransactions;
            set => SetProperty(ref _heldTransactions, value);
        }

        #endregion

        #region Commands

        public ICommand? HoldTransactionCommand { get; private set; }
        public ICommand? RecallTransactionCommand { get; private set; }
        public ICommand? VoidTransactionCommand { get; private set; }
        public ICommand? NewCustomerCommand { get; private set; }
        public ICommand? RemoveItemCommand { get; private set; }
        public ICommand? VoidLastItemCommand { get; private set; }
        public ICommand? PriceCheckCommand { get; private set; }
        public ICommand? ChangeQuantityCommand { get; private set; }
        public ICommand? AddDiscountCommand { get; private set; }
        public ICommand? ProcessReturnCommand { get; private set; }
        public ICommand? ReprintLastCommand { get; private set; }
        public ICommand? ClearTransactionCommand { get; private set; }
        public ICommand? CancelTransactionCommand { get; private set; }
        public ICommand? CashPaymentCommand { get; private set; }
        public ICommand? AccountPaymentCommand { get; private set; }
        public ICommand? PrintReceiptCommand { get; private set; }

        private void InitializeCommands()
        {
            HoldTransactionCommand = new AsyncRelayCommand(async _ => await HoldTransaction());
            RecallTransactionCommand = new AsyncRelayCommand(async _ => await RecallTransaction());
            VoidTransactionCommand = new AsyncRelayCommand(async _ => await VoidTransaction());
            NewCustomerCommand = new AsyncRelayCommand(async _ => await ShowNewCustomerDialog());
            RemoveItemCommand = new RelayCommand(RemoveItem);
            VoidLastItemCommand = new RelayCommand(_ => VoidLastItem());
            PriceCheckCommand = new AsyncRelayCommand(async _ => await CheckPrice());
            ChangeQuantityCommand = new AsyncRelayCommand(async _ => await ChangeQuantity());
            AddDiscountCommand = new RelayCommand(_ => AddDiscount());
            ProcessReturnCommand = new AsyncRelayCommand(async _ => await ProcessReturn());
            ReprintLastCommand = new AsyncRelayCommand(async _ => await ReprintLast());
            ClearTransactionCommand = new RelayCommand(_ => ClearTransaction());
            CancelTransactionCommand = new RelayCommand(_ => CancelTransaction());
            CashPaymentCommand = new AsyncRelayCommand(async _ => await ProcessCashPayment());
            AccountPaymentCommand = new AsyncRelayCommand(async _ => await ProcessAccountPayment());
            PrintReceiptCommand = new AsyncRelayCommand(async _ => await PrintReceipt());

            SearchProductsCommand = new AsyncRelayCommand(async _ =>
            {
                IsProductSearchVisible = true;
                SearchProducts();
            });
        }

        #endregion

        #region Initialization Methods

        private void InitializeCollections()
        {
            FilteredCustomers = new ObservableCollection<CustomerDTO>();
            HeldTransactions = new ObservableCollection<TransactionDTO>();
            CurrentTransaction = new TransactionDTO
            {
                Details = new ObservableCollection<TransactionDetailDTO>()
            };
        }

        private void StartNewTransaction()
        {
            CurrentTransactionNumber = DateTime.Now.ToString("yyyyMMddHHmmss");
            CurrentTransaction = new TransactionDTO
            {
                TransactionDate = DateTime.Now,
                Status = TransactionStatus.Pending,
                Details = new ObservableCollection<TransactionDetailDTO>()
            };
            ClearTotals();
        }

        // Add these methods in the Command Implementations section:

        private async Task HoldTransaction()
        {
            if (CurrentTransaction?.Details.Any() == true)
            {
                CurrentTransaction.Status = TransactionStatus.Pending;
                await Task.Run(() => HeldTransactions.Add(CurrentTransaction));
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    MessageBox.Show("Transaction has been held successfully", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information));
                StartNewTransaction();
            }
        }

        private async Task RecallTransaction()
        {
            var heldTransaction = HeldTransactions.LastOrDefault();
            if (heldTransaction != null)
            {
                await Task.Run(() =>
                {
                    CurrentTransaction = heldTransaction;
                    HeldTransactions.Remove(heldTransaction);
                });
                UpdateTotals();
            }
        }

        private async Task VoidTransaction()
        {
            if (CurrentTransaction == null) return;

            if (MessageBox.Show("Are you sure you want to void this transaction?", "Confirm Void",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await _transactionService.UpdateStatusAsync(CurrentTransaction.TransactionId, TransactionStatus.Cancelled);
                StartNewTransaction();
            }
        }

        private void RemoveItem(object? parameter)
        {
            if (parameter is TransactionDetailDTO detail && CurrentTransaction?.Details != null)
            {
                CurrentTransaction.Details.Remove(detail);
                UpdateTotals();
            }
        }

        private void VoidLastItem()
        {
            if (CurrentTransaction?.Details == null) return;

            var lastItem = CurrentTransaction.Details.LastOrDefault();
            if (lastItem != null)
            {
                CurrentTransaction.Details.Remove(lastItem);
                UpdateTotals();
            }
        }

        private async Task CheckPrice()
        {
            try
            {
                var barcode = await ShowInputDialog("Enter product barcode or code", "Price Check");
                if (!string.IsNullOrEmpty(barcode))
                {
                    var product = await _productService.GetByBarcodeAsync(barcode);
                    if (product != null)
                    {
                        var message = $"Product: {product.Name}\n" +
                                    $"Price: {product.SalePrice:C}\n" +
                                    $"Current Stock: {product.CurrentStock}\n" +
                                    $"Category: {product.CategoryName}";

                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                            MessageBox.Show(message, "Price Check", MessageBoxButton.OK, MessageBoxImage.Information));
                    }
                    else
                    {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                            MessageBox.Show("Product not found", "Error", MessageBoxButton.OK, MessageBoxImage.Warning));
                    }
                }
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    MessageBox.Show($"Error checking price: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        private async Task ChangeQuantity()
        {
            try
            {
                if (CurrentTransaction?.Details == null)
                {
                    await ShowErrorMessageAsync("No active transaction");
                    return;
                }

                if (SelectedTransactionDetail == null)
                {
                    await ShowErrorMessageAsync("Please select an item to change quantity");
                    return;
                }

                // Get the current stock of the product
                var product = await _productService.GetByIdAsync(SelectedTransactionDetail.ProductId);
                if (product == null)
                {
                    await ShowErrorMessageAsync("Product not found");
                    return;
                }

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    var dialog = new QuantityDialog(
                        SelectedTransactionDetail.ProductName,
                        SelectedTransactionDetail.Quantity,
                        product.CurrentStock);

                    dialog.Owner = System.Windows.Application.Current.MainWindow;

                    if (dialog.ShowDialog() == true)
                    {
                        // Check if the new quantity is valid
                        if (dialog.NewQuantity > product.CurrentStock)
                        {
                            MessageBox.Show(
                                $"Insufficient stock. Available: {product.CurrentStock}",
                                "Stock Warning",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return;
                        }

                        // Create updated detail
                        var updatedDetail = new TransactionDetailDTO
                        {
                            TransactionDetailId = SelectedTransactionDetail.TransactionDetailId,
                            TransactionId = SelectedTransactionDetail.TransactionId,
                            ProductId = SelectedTransactionDetail.ProductId,
                            ProductName = SelectedTransactionDetail.ProductName,
                            ProductBarcode = SelectedTransactionDetail.ProductBarcode,
                            Quantity = dialog.NewQuantity,
                            UnitPrice = SelectedTransactionDetail.UnitPrice,
                            PurchasePrice = SelectedTransactionDetail.PurchasePrice,
                            Discount = SelectedTransactionDetail.Discount,
                            Total = dialog.NewQuantity * SelectedTransactionDetail.UnitPrice
                        };

                        // Find and update the item in the collection
                        var index = CurrentTransaction.Details.IndexOf(SelectedTransactionDetail);
                        CurrentTransaction.Details.RemoveAt(index);
                        CurrentTransaction.Details.Insert(index, updatedDetail);

                        // Update selected item reference
                        SelectedTransactionDetail = updatedDetail;

                        // Update transaction totals
                        UpdateTotals();

                        // Force collection refresh
                        OnPropertyChanged(nameof(CurrentTransaction));

                        // Update status message
                        StatusMessage = $"Updated quantity of {updatedDetail.ProductName} to {dialog.NewQuantity}";
                    }
                });
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"Error changing quantity: {ex.Message}");
            }
        }

        private TransactionDetailDTO? _selectedTransactionDetail;
        public TransactionDetailDTO? SelectedTransactionDetail
        {
            get => _selectedTransactionDetail;
            set => SetProperty(ref _selectedTransactionDetail, value);
        }
        public ObservableCollection<ProductDTO> FilteredProducts
        {
            get => _filteredProducts;
            set => SetProperty(ref _filteredProducts, value);
        }

        private void AddDiscount()
        {
            if (CurrentTransaction?.Details == null || !CurrentTransaction.Details.Any())
            {
                MessageBox.Show("No items in transaction", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new DiscountDialog(TotalAmount);
            dialog.Owner = System.Windows.Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                DiscountAmount = dialog.DiscountAmount;
                UpdateTotals();
            }
        }

        private async Task ProcessReturn()
        {
            try
            {
                // Show dialog to get original transaction number
                var transactionNumber = await ShowInputDialog("Enter original transaction number", "Process Return");
                if (string.IsNullOrEmpty(transactionNumber)) return;

                // Get original transaction
                var originalTransaction = await _transactionService.GetByIdAsync(int.Parse(transactionNumber));
                if (originalTransaction == null)
                {
                    await ShowErrorMessageAsync("Transaction not found");
                    return;
                }

                // Create return transaction
                var returnTransaction = new TransactionDTO
                {
                    TransactionDate = DateTime.Now,
                    CustomerId = originalTransaction.CustomerId,
                    CustomerName = originalTransaction.CustomerName,
                    TransactionType = TransactionType.Return,
                    Status = TransactionStatus.Pending,
                    Details = new ObservableCollection<TransactionDetailDTO>()
                };

                // Display item selection dialog for return
                var returnDialog = new ReturnItemsDialog(originalTransaction.Details);
                var dialogResult = await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => returnDialog.ShowDialog());

                if (dialogResult == true)
                {
                    foreach (var item in returnDialog.SelectedItems.Where(i => i.ReturnQuantity > 0))
                    {
                        var returnDetail = new TransactionDetailDTO
                        {
                            ProductId = item.ProductId,
                            ProductName = item.ProductName,
                            ProductBarcode = item.ProductBarcode,
                            Quantity = item.ReturnQuantity,
                            UnitPrice = -item.UnitPrice, // Negative for returns
                            PurchasePrice = item.PurchasePrice,
                            Discount = 0,
                            Total = -item.UnitPrice * item.ReturnQuantity
                        };

                        returnTransaction.Details.Add(returnDetail);
                    }

                    // Calculate totals
                    returnTransaction.TotalAmount = returnTransaction.Details.Sum(d => d.Total);
                    returnTransaction.PaidAmount = -returnTransaction.TotalAmount; // Refund amount

                    // Process return
                    var result = await _transactionService.ProcessRefundAsync(returnTransaction);
                    if (result != null)
                    {
                        await PrintReturnReceipt(result);
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                            MessageBox.Show("Return processed successfully", "Success",
                                MessageBoxButton.OK, MessageBoxImage.Information));
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"Error processing return: {ex.Message}");
            }
        }
        private async Task PrintReturnReceipt(TransactionDTO returnTransaction)
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    var document = new FlowDocument();

                    // Add header
                    var header = new Paragraph(new Run("RETURN RECEIPT"))
                    {
                        FontSize = 16,
                        FontWeight = FontWeights.Bold,
                        TextAlignment = TextAlignment.Center
                    };
                    document.Blocks.Add(header);

                    // Add transaction details
                    document.Blocks.Add(new Paragraph(new Run($"Return Transaction #: {returnTransaction.TransactionId}")));
                    document.Blocks.Add(new Paragraph(new Run($"Date: {returnTransaction.TransactionDate:g}")));
                    document.Blocks.Add(new Paragraph(new Run($"Customer: {returnTransaction.CustomerName}")));

                    // Add items table
                    var table = new Table();
                    var headerRow = new TableRow();
                    headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Item"))));
                    headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Qty"))));
                    headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Price"))));
                    headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Total"))));

                    var rowGroup = new TableRowGroup();
                    rowGroup.Rows.Add(headerRow);

                    foreach (var detail in returnTransaction.Details)
                    {
                        var row = new TableRow();
                        row.Cells.Add(new TableCell(new Paragraph(new Run(detail.ProductName))));
                        row.Cells.Add(new TableCell(new Paragraph(new Run(detail.Quantity.ToString()))));
                        row.Cells.Add(new TableCell(new Paragraph(new Run(detail.UnitPrice.ToString("C")))));
                        row.Cells.Add(new TableCell(new Paragraph(new Run(detail.Total.ToString("C")))));
                        rowGroup.Rows.Add(row);
                    }

                    table.RowGroups.Add(rowGroup);
                    document.Blocks.Add(table);

                    // Add total
                    document.Blocks.Add(new Paragraph(new Run($"Total Refund Amount: {returnTransaction.TotalAmount:C}"))
                    {
                        FontWeight = FontWeights.Bold
                    });

                    printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "Return Receipt");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"Error printing return receipt: {ex.Message}");
            }
        }
        private async Task ReprintLast()
        {
            try
            {
                // Get the last completed transaction
                var lastTransaction = await _transactionService.GetLastCompletedTransactionAsync();

                if (lastTransaction == null)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        MessageBox.Show("No previous transaction found.", "Information",
                            MessageBoxButton.OK, MessageBoxImage.Information));
                    return;
                }

                LastCompletedTransaction = lastTransaction;
                await PrintReceipt(lastTransaction);
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"Error reprinting last receipt: {ex.Message}");
            }
        }

        private void ClearTransaction()
        {
            if (MessageBox.Show("Are you sure you want to clear this transaction?", "Confirm Clear",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                StartNewTransaction();
            }
        }

        private TransactionDTO? _lastCompletedTransaction;
        public TransactionDTO? LastCompletedTransaction
        {
            get => _lastCompletedTransaction;
            set => SetProperty(ref _lastCompletedTransaction, value);
        }

        private void CancelTransaction()
        {
            if (MessageBox.Show("Are you sure you want to cancel this transaction?", "Confirm Cancel",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                StartNewTransaction();
            }
        }


        private async Task ProcessCashPayment()
        {
            if (CurrentTransaction?.Details == null || !CurrentTransaction.Details.Any())
            {
                await ShowErrorMessageAsync("No items in transaction");
                return;
            }

            if (PaymentAmount < TotalAmount)
            {
                await ShowErrorMessageAsync("Payment amount is less than total amount");
                return;
            }

            try
            {
                // Calculate change
                decimal change = PaymentAmount - TotalAmount;

                // Create a new DTO for processing to ensure clean data
                var transactionToProcess = new TransactionDTO
                {
                    TransactionDate = DateTime.Now,
                    CustomerId = SelectedCustomer?.CustomerId,
                    CustomerName = SelectedCustomer?.Name ?? "Walk-in Customer",
                    PaidAmount = TotalAmount, // Only store the actual amount, not the overpayment
                    TotalAmount = TotalAmount,
                    Balance = 0, // No balance when paid in full
                    TransactionType = TransactionType.Sale,
                    Status = TransactionStatus.Completed,
                    Details = new ObservableCollection<TransactionDetailDTO>(CurrentTransaction.Details.Select(d => new TransactionDetailDTO
                    {
                        ProductId = d.ProductId,
                        ProductName = d.ProductName,
                        ProductBarcode = d.ProductBarcode,
                        Quantity = d.Quantity,
                        UnitPrice = d.UnitPrice,
                        PurchasePrice = d.PurchasePrice,
                        Discount = d.Discount,
                        Total = d.Total
                    }))
                };

                var result = await _transactionService.ProcessSaleAsync(transactionToProcess);

                if (result != null)
                {
                    // Display change amount to cashier
                    if (change > 0)
                    {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                            MessageBox.Show($"Please return change to customer: {change:C}", "Change Due",
                                MessageBoxButton.OK, MessageBoxImage.Information));
                    }

                    await PrintReceipt();
                    StartNewTransaction();

                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        MessageBox.Show("Transaction completed successfully", "Success",
                            MessageBoxButton.OK, MessageBoxImage.Information));
                }
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"Error processing payment: {ex.Message}");
            }
        }


        private async Task ProcessAccountPayment()
        {
            if (SelectedCustomer == null)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    MessageBox.Show("Please select a customer for account payment", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning));
                return;
            }

            try
            {
                if (CurrentTransaction == null) return;

                // For account payments, we only add the actual total to the customer's balance
                CurrentTransaction.PaidAmount = 0; // No immediate payment
                CurrentTransaction.CustomerId = SelectedCustomer.CustomerId;
                CurrentTransaction.CustomerName = SelectedCustomer.Name;
                CurrentTransaction.TransactionType = TransactionType.Sale;
                CurrentTransaction.Balance = TotalAmount; // Full amount goes to balance

                await _transactionService.ProcessSaleAsync(CurrentTransaction);
                await PrintReceipt();
                StartNewTransaction();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    MessageBox.Show($"Transaction added to customer account: {SelectedCustomer.Name}", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    MessageBox.Show($"Error processing account payment: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }


        private async void SearchCustomers()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(CustomerSearchText))
                {
                    FilteredCustomers.Clear();
                    IsCustomerSearchVisible = false;
                    return;
                }

                var customers = await _customerService.GetByNameAsync(CustomerSearchText);
                FilteredCustomers = new ObservableCollection<CustomerDTO>(customers
                    .Where(c => c.IsActive)
                    .Take(10)); // Limit to 10 suggestions
                IsCustomerSearchVisible = FilteredCustomers.Any();
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"Error searching customers: {ex.Message}");
            }
        }


        public async Task SearchProducts()
        {
            if (string.IsNullOrWhiteSpace(ProductSearchText))
            {
                FilteredProducts.Clear();
                IsProductSearchVisible = false;
                return;
            }

            try
            {
                var products = await _productService.GetAllAsync();
                var filtered = products.Where(p =>
                    p.Name.Contains(ProductSearchText, StringComparison.OrdinalIgnoreCase) ||
                    p.Barcode.Contains(ProductSearchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    FilteredProducts = new ObservableCollection<ProductDTO>(filtered);
                    IsProductSearchVisible = FilteredProducts.Any();
                });
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"Error searching products: {ex.Message}");
            }
        }

        public void OnProductSelected(ProductDTO product)
        {
            if (product != null)
            {
                AddProductToTransaction(product);
                ProductSearchText = string.Empty;
                IsProductSearchVisible = false;
            }
        }

        // Add this method to close the search popup when clicking outside
        public void CloseProductSearch()
        {
            IsProductSearchVisible = false;
            ProductSearchText = string.Empty;
        }
        private void ClearTotals()
        {
            ItemCount = 0;
            SubTotal = 0;
            TaxAmount = 0;
            DiscountAmount = 0;
            TotalAmount = 0;
            PaymentAmount = 0;
            ChangeDue = 0;
        }

        private void UpdateTotals()
        {
            if (CurrentTransaction?.Details == null) return;

            ItemCount = CurrentTransaction.Details.Sum(d => d.Quantity);
            SubTotal = CurrentTransaction.Details.Sum(d => d.Total);

            // Calculate tax (can be configurable in settings)
            TaxAmount = Math.Round(SubTotal * 0.15m, 2); // 15% tax rate

            // Calculate final total
            TotalAmount = SubTotal + TaxAmount - DiscountAmount;

            // Update change due if payment amount is set
            UpdateChangeDue();

            // Update transaction totals
            if (CurrentTransaction != null)
            {
                CurrentTransaction.TotalAmount = TotalAmount;
                CurrentTransaction.Balance = TotalAmount - PaymentAmount;
            }

            // Trigger property changed notifications
            OnPropertyChanged(nameof(ItemCount));
            OnPropertyChanged(nameof(SubTotal));
            OnPropertyChanged(nameof(TaxAmount));
            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(CurrentTransaction));
        }


        private void UpdateChangeDue()
        {
            if (PaymentAmount > TotalAmount)
            {
                ChangeDue = PaymentAmount - TotalAmount;
            }
            else
            {
                ChangeDue = 0;
            }
            OnPropertyChanged(nameof(ChangeDue));
        }


        public async Task ProcessBarcodeInput()
        {
            if (string.IsNullOrEmpty(BarcodeText)) return;

            try
            {
                var product = await _productService.GetByBarcodeAsync(BarcodeText);
                if (product != null)
                {
                    AddProductToTransaction(product);
                }
                else
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        MessageBox.Show("Product not found", "Error", MessageBoxButton.OK, MessageBoxImage.Warning));
                }
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    MessageBox.Show($"Error processing barcode: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                BarcodeText = string.Empty;
            }
        }

        private void AddProductToTransaction(ProductDTO product)
        {
            if (CurrentTransaction?.Details == null)
            {
                CurrentTransaction = new TransactionDTO
                {
                    Details = new ObservableCollection<TransactionDetailDTO>(),
                    TransactionDate = DateTime.Now,
                    Status = TransactionStatus.Pending
                };
            }

            // Check if the product already exists in the transaction
            var existingDetail = CurrentTransaction.Details
                .FirstOrDefault(d => d.ProductId == product.ProductId);

            if (existingDetail != null)
            {
                // Update existing item
                existingDetail.Quantity++;
                existingDetail.Total = existingDetail.Quantity * existingDetail.UnitPrice;

                // Force UI update for the specific item
                var index = CurrentTransaction.Details.IndexOf(existingDetail);
                CurrentTransaction.Details.RemoveAt(index);
                CurrentTransaction.Details.Insert(index, existingDetail);
            }
            else
            {
                // Add new item
                var detail = new TransactionDetailDTO
                {
                    ProductId = product.ProductId,
                    ProductName = product.Name,
                    ProductBarcode = product.Barcode,
                    Quantity = 1,
                    UnitPrice = product.SalePrice,
                    PurchasePrice = product.PurchasePrice,
                    Discount = 0,
                    Total = product.SalePrice,
                    TransactionId = CurrentTransaction.TransactionId
                };

                CurrentTransaction.Details.Add(detail);
            }

            // Update totals
            UpdateTotals();

            // Update status message
            StatusMessage = $"Added {product.Name} to transaction";

            // Trigger property changed for the Details collection
            OnPropertyChanged(nameof(CurrentTransaction));
        }


        private async Task ShowNewCustomerDialog()
        {
            try
            {
                // Show name input dialog
                var nameDialog = new InputDialog("New Customer", "Enter customer name:");
                if (System.Windows.Application.Current.MainWindow != null)
                {
                    nameDialog.Owner = System.Windows.Application.Current.MainWindow;
                }

                if (nameDialog.ShowDialog() != true || string.IsNullOrWhiteSpace(nameDialog.Input))
                {
                    return;
                }

                // Show phone input dialog
                var phoneDialog = new InputDialog("New Customer", "Enter customer phone number:");
                if (System.Windows.Application.Current.MainWindow != null)
                {
                    phoneDialog.Owner = System.Windows.Application.Current.MainWindow;
                }

                if (phoneDialog.ShowDialog() != true)
                {
                    return;
                }

                // Show email input dialog (optional)
                var emailDialog = new InputDialog("New Customer", "Enter customer email (optional):");
                if (System.Windows.Application.Current.MainWindow != null)
                {
                    emailDialog.Owner = System.Windows.Application.Current.MainWindow;
                }

                string? email = null;
                if (emailDialog.ShowDialog() == true)
                {
                    email = emailDialog.Input;
                }

                var newCustomer = new CustomerDTO
                {
                    Name = nameDialog.Input.Trim(),
                    Phone = phoneDialog.Input.Trim(),
                    Email = email?.Trim(),
                    IsActive = true,
                    Balance = 0,
                    CreatedAt = DateTime.Now
                };

                // Validate customer data
                if (!ValidateNewCustomer(newCustomer))
                {
                    return;
                }

                // Save customer to database
                var createdCustomer = await _customerService.CreateAsync(newCustomer);
                if (createdCustomer != null)
                {
                    SelectedCustomer = createdCustomer;
                    await ShowSuccessMessage("Customer created successfully!");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"Error creating customer: {ex.Message}");
            }
        }

        private bool ValidateNewCustomer(CustomerDTO customer)
        {
            if (string.IsNullOrWhiteSpace(customer.Name))
            {
                MessageBox.Show("Customer name is required.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(customer.Phone))
            {
                MessageBox.Show("Phone number is required.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(customer.Email) &&
                !IsValidEmail(customer.Email))
            {
                MessageBox.Show("Invalid email format.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private async Task ShowSuccessMessage(string message)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                MessageBox.Show(message, "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information));
        }

        private async Task PrintReceipt()
        {
            if (CurrentTransaction == null || !CurrentTransaction.Details.Any())
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    MessageBox.Show("No transaction to print", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning));
                return;
            }

            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    var document = new FlowDocument();
                    var paragraph = new Paragraph();

                    // Add header
                    paragraph.Inlines.Add(new Bold(new Run("QUICK TECH SYSTEMS\n")) { FontSize = 16 });
                    paragraph.Inlines.Add(new Run($"Transaction #: {CurrentTransactionNumber}\n"));
                    paragraph.Inlines.Add(new Run($"Date: {DateTime.Now:g}\n"));
                    paragraph.Inlines.Add(new Run($"Cashier: {CashierName}\n\n"));

                    if (SelectedCustomer != null)
                    {
                        paragraph.Inlines.Add(new Run($"Customer: {SelectedCustomer.Name}\n\n"));
                    }

                    // Add items
                    paragraph.Inlines.Add(new Bold(new Run("Items:\n")));
                    foreach (var detail in CurrentTransaction.Details)
                    {
                        paragraph.Inlines.Add(new Run(
                            $"{detail.ProductName}\n" +
                            $"{detail.Quantity} x {detail.UnitPrice:C} = {detail.Total:C}\n"));
                    }

                    // Add totals
                    paragraph.Inlines.Add(new Run("\n"));
                    paragraph.Inlines.Add(new Run($"Subtotal: {SubTotal:C}\n"));
                    if (DiscountAmount > 0)
                    {
                        paragraph.Inlines.Add(new Run($"Discount: -{DiscountAmount:C}\n"));
                    }
                    paragraph.Inlines.Add(new Bold(new Run($"Total: {TotalAmount:C}\n")));
                    paragraph.Inlines.Add(new Run($"Paid: {PaymentAmount:C}\n"));

                    // Add change or balance
                    if (PaymentAmount > TotalAmount)
                    {
                        paragraph.Inlines.Add(new Bold(new Run($"Change Due: {ChangeDue:C}\n")));
                    }
                    else if (PaymentAmount < TotalAmount)
                    {
                        paragraph.Inlines.Add(new Bold(new Run($"Balance: {(TotalAmount - PaymentAmount):C}\n")));
                    }

                    // Add footer
                    paragraph.Inlines.Add(new Run("\nThank you for your business!"));

                    document.Blocks.Add(paragraph);
                    document.PagePadding = new Thickness(50);

                    printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "Receipt");
                }
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    MessageBox.Show($"Error printing receipt: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }
        private async Task PrintReceipt(TransactionDTO transaction)
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    var document = new FlowDocument();

                    // Store Logo
                    var header = new Paragraph(new Run("QUICK TECH SYSTEMS"))
                    {
                        FontSize = 20,
                        FontWeight = FontWeights.Bold,
                        TextAlignment = TextAlignment.Center
                    };
                    document.Blocks.Add(header);

                    // Transaction Info
                    document.Blocks.Add(new Paragraph(new Run($"Transaction #: {transaction.TransactionId}")));
                    document.Blocks.Add(new Paragraph(new Run($"Date: {transaction.TransactionDate:g}")));
                    document.Blocks.Add(new Paragraph(new Run($"Cashier: {CashierName}")));

                    if (!string.IsNullOrEmpty(transaction.CustomerName))
                    {
                        document.Blocks.Add(new Paragraph(new Run($"Customer: {transaction.CustomerName}")));
                    }

                    // Items Table
                    var table = new Table();
                    var rowGroup = new TableRowGroup();

                    // Add header row
                    var headerRow = new TableRow();
                    headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Item"))));
                    headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Qty"))));
                    headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Price"))));
                    headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Total"))));
                    rowGroup.Rows.Add(headerRow);

                    // Add item rows
                    foreach (var detail in transaction.Details)
                    {
                        var row = new TableRow();
                        row.Cells.Add(new TableCell(new Paragraph(new Run(detail.ProductName))));
                        row.Cells.Add(new TableCell(new Paragraph(new Run(detail.Quantity.ToString()))));
                        row.Cells.Add(new TableCell(new Paragraph(new Run(detail.UnitPrice.ToString("C")))));
                        row.Cells.Add(new TableCell(new Paragraph(new Run(detail.Total.ToString("C")))));
                        rowGroup.Rows.Add(row);
                    }

                    table.RowGroups.Add(rowGroup);
                    document.Blocks.Add(table);

                    // Totals
                    document.Blocks.Add(new Paragraph(new Run($"Subtotal: {transaction.TotalAmount:C}")));

                    if (transaction.DiscountAmount > 0)
                    {
                        document.Blocks.Add(new Paragraph(new Run($"Discount: -{transaction.DiscountAmount:C}")));
                    }

                    document.Blocks.Add(new Paragraph(new Run($"Total: {transaction.TotalAmount:C}"))
                    {
                        FontWeight = FontWeights.Bold
                    });
                    document.Blocks.Add(new Paragraph(new Run($"Paid: {transaction.PaidAmount:C}")));
                    document.Blocks.Add(new Paragraph(new Run($"Change: {transaction.PaidAmount - transaction.TotalAmount:C}")));

                    // Footer
                    document.Blocks.Add(new Paragraph(new Run("Thank you for your business!"))
                    {
                        TextAlignment = TextAlignment.Center,
                        FontStyle = FontStyles.Italic
                    });

                    document.PagePadding = new Thickness(40);
                    printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "Receipt");

                    StatusMessage = "Receipt reprinted successfully";
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error printing receipt: {ex.Message}");
            }
        }
        private async Task<string> ShowInputDialog(string prompt, string title)
        {
            return await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var dialog = new InputDialog(title, prompt);
                if (System.Windows.Application.Current.MainWindow != null)
                {
                    dialog.Owner = System.Windows.Application.Current.MainWindow;
                }
                return dialog.ShowDialog() == true ? dialog.Input : string.Empty;
            });
        }

        #endregion
    }
}