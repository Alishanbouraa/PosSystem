using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32;
using QuickTechSystems.Application.DTOs;
using QuickTechSystems.Application.Services.Interfaces;
using QuickTechSystems.WPF.Commands;
using QuickTechSystems.Domain.Enums;
using System.Windows;
using System.Windows.Media;
using System.Windows;

namespace QuickTechSystems.WPF.ViewModels
{
    public class DebtViewModel : ViewModelBase
    {
        private readonly ICustomerService _customerService;
        private readonly ITransactionService _transactionService;
        private ObservableCollection<CustomerDTO> _debtors;
        private CustomerDTO? _selectedDebtor;
        private ObservableCollection<DebtPaymentDTO> _paymentHistory;
        private decimal _paymentAmount;
        private string _searchText = string.Empty;
        private bool _isDebtorSelected;
        private decimal _totalDebtAmount;
        private int _totalDebtors;

        public ObservableCollection<CustomerDTO> Debtors
        {
            get => _debtors;
            set => SetProperty(ref _debtors, value);
        }

        public CustomerDTO? SelectedDebtor
        {
            get => _selectedDebtor;
            set
            {
                if (SetProperty(ref _selectedDebtor, value))
                {
                    IsDebtorSelected = value != null;
                    _ = LoadPaymentHistoryAsync();
                }
            }
        }

        public ObservableCollection<DebtPaymentDTO> PaymentHistory
        {
            get => _paymentHistory;
            set => SetProperty(ref _paymentHistory, value);
        }

        public decimal PaymentAmount
        {
            get => _paymentAmount;
            set => SetProperty(ref _paymentAmount, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                SearchDebtors();
            }
        }

        public bool IsDebtorSelected
        {
            get => _isDebtorSelected;
            set => SetProperty(ref _isDebtorSelected, value);
        }

        public decimal TotalDebtAmount
        {
            get => _totalDebtAmount;
            set => SetProperty(ref _totalDebtAmount, value);
        }

        public int TotalDebtors
        {
            get => _totalDebtors;
            set => SetProperty(ref _totalDebtors, value);
        }

        public ICommand LoadCommand { get; }
        public ICommand ProcessPaymentCommand { get; }
        public ICommand ExportReportCommand { get; }
        public ICommand PrintReportCommand { get; }

        public DebtViewModel(ICustomerService customerService, ITransactionService transactionService)
        {
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
            _debtors = new ObservableCollection<CustomerDTO>();
            _paymentHistory = new ObservableCollection<DebtPaymentDTO>();

            LoadCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
            ProcessPaymentCommand = new AsyncRelayCommand(async _ => await ProcessPaymentAsync());
            ExportReportCommand = new AsyncRelayCommand(async _ => await ExportDebtReportAsync());
            PrintReportCommand = new AsyncRelayCommand(async _ => await PrintDebtReportAsync());

            Task.Run(async () => await LoadDataAsync());
        }

        protected override async Task LoadDataAsync()
        {
            try
            {
                var debtors = await _customerService.GetDebtorsAsync();
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Debtors = new ObservableCollection<CustomerDTO>(debtors);
                    TotalDebtors = Debtors.Count;
                    TotalDebtAmount = Debtors.Sum(d => d.Balance);
                });
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"Error loading debtors: {ex.Message}");
            }
        }

        private async Task LoadPaymentHistoryAsync()
        {
            if (SelectedDebtor == null) return;

            try
            {
                var transactions = await _transactionService.GetByCustomerAsync(SelectedDebtor.CustomerId);
                var payments = transactions
                    .Where(t => t.PaidAmount > 0)
                    .Select(t => new DebtPaymentDTO
                    {
                        Date = t.TransactionDate,
                        Amount = t.PaidAmount,
                        Reference = $"Payment - #{t.TransactionId}",
                        CustomerId = t.CustomerId ?? 0,
                        CustomerName = t.CustomerName,
                        TransactionId = t.TransactionId,
                        RemainingBalance = t.Balance
                    })
                    .OrderByDescending(p => p.Date)
                    .ToList();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    PaymentHistory = new ObservableCollection<DebtPaymentDTO>(payments);
                });
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"Error loading payment history: {ex.Message}");
            }
        }

        private async Task ProcessPaymentAsync()
        {
            if (SelectedDebtor == null || PaymentAmount <= 0)
            {
                await ShowErrorMessageAsync("Please select a customer and enter a valid payment amount.");
                return;
            }

            try
            {
                var transaction = new TransactionDTO
                {
                    CustomerId = SelectedDebtor.CustomerId,
                    CustomerName = SelectedDebtor.Name,
                    TransactionDate = DateTime.Now,
                    PaidAmount = PaymentAmount,
                    TotalAmount = PaymentAmount,
                    Balance = -PaymentAmount,
                    Status = TransactionStatus.Completed
                };

                await _transactionService.ProcessSaleAsync(transaction);
                await _customerService.UpdateBalanceAsync(SelectedDebtor.CustomerId, -PaymentAmount);

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    PaymentAmount = 0;
                    MessageBox.Show("Payment processed successfully", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                });

                await LoadDataAsync();
                await LoadPaymentHistoryAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"Error processing payment: {ex.Message}");
            }
        }

        private void SearchDebtors()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                Task.Run(async () => await LoadDataAsync());
                return;
            }

            var filtered = Debtors.Where(d =>
                d.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                d.Phone?.Contains(SearchText) == true)
                .ToList();

            Debtors = new ObservableCollection<CustomerDTO>(filtered);
        }

        private async Task ExportDebtReportAsync()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                DefaultExt = "csv",
                FileName = $"Debt_Report_{DateTime.Now:yyyyMMdd}"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    await using var writer = new StreamWriter(saveFileDialog.FileName);
                    await writer.WriteLineAsync("Customer,Phone,Balance,Email");

                    foreach (var debtor in Debtors)
                    {
                        await writer.WriteLineAsync(
                            $"{debtor.Name},{debtor.Phone},{debtor.Balance:F2},{debtor.Email}");
                    }

                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show("Report exported successfully", "Success",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
                catch (Exception ex)
                {
                    await ShowErrorMessageAsync($"Error exporting report: {ex.Message}");
                }
            }
        }

        private async Task PrintDebtReportAsync()
        {
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                try
                {
                    var document = new FlowDocument
                    {
                        PagePadding = new Thickness(50)
                    };

                    var header = new Paragraph(new Run("Debt Report"))
                    {
                        FontSize = 24,
                        FontWeight = FontWeights.Bold,
                        TextAlignment = TextAlignment.Center
                    };
                    document.Blocks.Add(header);

                    document.Blocks.Add(new Paragraph(new Run($"Total Debtors: {TotalDebtors}")));
                    document.Blocks.Add(new Paragraph(new Run($"Total Debt: {TotalDebtAmount:C2}")));

                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        printDialog.PrintDocument(
                            ((IDocumentPaginatorSource)document).DocumentPaginator,
                            "Debt Report");
                    });
                }
                catch (Exception ex)
                {
                    await ShowErrorMessageAsync($"Error printing report: {ex.Message}");
                }
            }
        }
    }
}