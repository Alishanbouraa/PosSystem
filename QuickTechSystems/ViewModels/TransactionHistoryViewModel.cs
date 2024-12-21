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
using System.Text;

namespace QuickTechSystems.WPF.ViewModels
{
    public class TransactionHistoryViewModel : ViewModelBase
    {
        private readonly ITransactionService _transactionService;
        private ObservableCollection<TransactionDTO> _transactions;
        private ObservableCollection<TransactionDTO> _allTransactions;
        private DateTime _startDate;
        private DateTime _endDate;
        private decimal _totalProfit;
        private decimal _totalSales;
        private string _searchText = string.Empty;
        private int _currentPage = 1;
        private int _pageSize = 100;
        private int _totalPages;
        private int _totalRecords;
        private string _selectedDateFilter = "Today";
        private string _dateRangeDisplay = string.Empty;

        public ObservableCollection<TransactionDTO> Transactions
        {
            get => _transactions;
            set => SetProperty(ref _transactions, value);
        }

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                {
                    UpdateDateRangeDisplay();
                    _ = LoadTransactionsAsync();
                }
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                {
                    UpdateDateRangeDisplay();
                    _ = LoadTransactionsAsync();
                }
            }
        }

        public decimal TotalProfit
        {
            get => _totalProfit;
            set => SetProperty(ref _totalProfit, value);
        }

        public decimal TotalSales
        {
            get => _totalSales;
            set => SetProperty(ref _totalSales, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterTransactions();
                }
            }
        }

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    UpdatePagedTransactions();
                }
            }
        }

        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (SetProperty(ref _pageSize, value))
                {
                    CalculatePagination();
                    UpdatePagedTransactions();
                }
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        public int TotalRecords
        {
            get => _totalRecords;
            set => SetProperty(ref _totalRecords, value);
        }

        public string SelectedDateFilter
        {
            get => _selectedDateFilter;
            set
            {
                if (SetProperty(ref _selectedDateFilter, value))
                {
                    ApplyDateFilter();
                }
            }
        }

        public string DateRangeDisplay
        {
            get => _dateRangeDisplay;
            set => SetProperty(ref _dateRangeDisplay, value);
        }

        public ObservableCollection<string> DateFilters { get; } = new()
        {
            "Today",
            "Yesterday",
            "This Week",
            "Last Week",
            "This Month",
            "Last Month",
            "This Year",
            "Custom Range"
        };

        public ObservableCollection<int> PageSizes { get; } = new()
        {
            50,
            100,
            200,
            500
        };

        public ICommand LoadCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand PrintReportCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand FirstPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand LastPageCommand { get; }

        public TransactionHistoryViewModel(ITransactionService transactionService)
        {
            _transactionService = transactionService;
            _transactions = new ObservableCollection<TransactionDTO>();
            _allTransactions = new ObservableCollection<TransactionDTO>();

            // Initialize date range to today
            StartDate = DateTime.Today;
            EndDate = DateTime.Today.AddDays(1).AddSeconds(-1);

            // Initialize commands
            LoadCommand = new AsyncRelayCommand(async _ => await LoadTransactionsAsync());
            ExportCommand = new AsyncRelayCommand(async _ => await ExportTransactionsAsync());
            PrintReportCommand = new AsyncRelayCommand(async _ => await PrintTransactionReportAsync());
            RefreshCommand = new AsyncRelayCommand(async _ => await LoadTransactionsAsync());

            FirstPageCommand = new RelayCommand(_ => GoToFirstPage(), _ => CanNavigatePrevious());
            PreviousPageCommand = new RelayCommand(_ => GoToPreviousPage(), _ => CanNavigatePrevious());
            NextPageCommand = new RelayCommand(_ => GoToNextPage(), _ => CanNavigateNext());
            LastPageCommand = new RelayCommand(_ => GoToLastPage(), _ => CanNavigateNext());

            _ = LoadTransactionsAsync();
        }

        private void UpdateDateRangeDisplay()
        {
            DateRangeDisplay = $"{StartDate:MMM dd, yyyy} - {EndDate:MMM dd, yyyy}";
        }

        private async Task LoadTransactionsAsync()
        {
            try
            {
                var transactions = await _transactionService.GetByDateRangeAsync(StartDate, EndDate);
                _allTransactions = new ObservableCollection<TransactionDTO>(transactions);

                TotalRecords = _allTransactions.Count;
                CalculatePagination();
                UpdatePagedTransactions();
                CalculateTotals();
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"Error loading transactions: {ex.Message}");
            }
        }

        private void CalculatePagination()
        {
            TotalPages = (TotalRecords + PageSize - 1) / PageSize;
            CurrentPage = Math.Min(CurrentPage, TotalPages);
            if (CurrentPage <= 0 && TotalPages > 0) CurrentPage = 1;
        }

        private void UpdatePagedTransactions()
        {
            var pagedTransactions = _allTransactions
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            Transactions = new ObservableCollection<TransactionDTO>(pagedTransactions);
        }

        private void CalculateTotals()
        {
            TotalSales = _allTransactions.Sum(t => t.TotalAmount);
            TotalProfit = _allTransactions.Sum(t =>
                t.Details?.Sum(d => (d.UnitPrice - d.PurchasePrice) * d.Quantity) ?? 0);
        }

        private void GoToFirstPage()
        {
            CurrentPage = 1;
        }

        private void GoToPreviousPage()
        {
            if (CurrentPage > 1)
                CurrentPage--;
        }

        private void GoToNextPage()
        {
            if (CurrentPage < TotalPages)
                CurrentPage++;
        }

        private void GoToLastPage()
        {
            CurrentPage = TotalPages;
        }

        private bool CanNavigatePrevious()
        {
            return CurrentPage > 1;
        }

        private bool CanNavigateNext()
        {
            return CurrentPage < TotalPages;
        }

        private void ApplyDateFilter()
        {
            DateTime now = DateTime.Now;
            switch (SelectedDateFilter)
            {
                case "Today":
                    StartDate = DateTime.Today;
                    EndDate = DateTime.Today.AddDays(1).AddSeconds(-1);
                    break;

                case "Yesterday":
                    StartDate = DateTime.Today.AddDays(-1);
                    EndDate = DateTime.Today.AddSeconds(-1);
                    break;

                case "This Week":
                    StartDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                    EndDate = StartDate.AddDays(7).AddSeconds(-1);
                    break;

                case "Last Week":
                    StartDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek - 7);
                    EndDate = StartDate.AddDays(7).AddSeconds(-1);
                    break;

                case "This Month":
                    StartDate = new DateTime(now.Year, now.Month, 1);
                    EndDate = StartDate.AddMonths(1).AddSeconds(-1);
                    break;

                case "Last Month":
                    StartDate = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
                    EndDate = new DateTime(now.Year, now.Month, 1).AddSeconds(-1);
                    break;

                case "This Year":
                    StartDate = new DateTime(now.Year, 1, 1);
                    EndDate = StartDate.AddYears(1).AddSeconds(-1);
                    break;

                case "Custom Range":
                    // Keep existing date range
                    break;
            }

            UpdateDateRangeDisplay();
        }

        private void FilterTransactions()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                UpdatePagedTransactions();
                return;
            }

            var filtered = _allTransactions.Where(t =>
                (t.CustomerName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                t.TransactionId.ToString().Contains(SearchText) ||
                (t.Details?.Any(d => d.ProductName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ?? false))
                .ToList();

            _allTransactions = new ObservableCollection<TransactionDTO>(filtered);
            TotalRecords = filtered.Count;
            CalculatePagination();
            UpdatePagedTransactions();
            CalculateTotals();
        }

        private async Task ExportTransactionsAsync()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                    DefaultExt = "csv",
                    FileName = $"Transaction_History_{DateTime.Now:yyyyMMdd}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var csv = new StringBuilder();
                    csv.AppendLine("Transaction ID,Date,Customer,Type,Items,Total Amount,Profit,Status");

                    foreach (var transaction in _allTransactions)
                    {
                        var profit = transaction.Details?.Sum(d =>
                            (d.UnitPrice - d.PurchasePrice) * d.Quantity) ?? 0;

                        var itemCount = transaction.Details?.Count ?? 0;

                        csv.AppendLine($"{transaction.TransactionId}," +
                            $"\"{transaction.TransactionDate:g}\"," +
                            $"\"{transaction.CustomerName}\"," +
                            $"{transaction.TransactionType}," +
                            $"{itemCount}," +
                            $"{transaction.TotalAmount:F2}," +
                            $"{profit:F2}," +
                            $"{transaction.Status}");
                    }

                    await File.WriteAllTextAsync(saveFileDialog.FileName, csv.ToString());
                    MessageBox.Show("Export completed successfully.", "Export Complete",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"Error exporting transactions: {ex.Message}");
            }
        }

        private async Task PrintTransactionReportAsync()
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    var document = new FlowDocument
                    {
                        PagePadding = new Thickness(50)
                    };

                    // Add header
                    document.Blocks.Add(new Paragraph(new Run("Transaction History Report"))
                    {
                        FontSize = 24,
                        FontWeight = FontWeights.Bold,
                        TextAlignment = TextAlignment.Center
                    });

                    // Add date range
                    document.Blocks.Add(new Paragraph(new Run($"Period: {StartDate:d} - {EndDate:d}"))
                    {
                        TextAlignment = TextAlignment.Center
                    });

                    // Add summary
                    var summarySection = new Section();
                    summarySection.Blocks.Add(new Paragraph(new Bold(new Run("Summary"))) { FontSize = 16 });
                    summarySection.Blocks.Add(new Paragraph(new Run($"Total Transactions: {TotalRecords}")));
                    summarySection.Blocks.Add(new Paragraph(new Run($"Total Sales: {TotalSales:C}")));
                    summarySection.Blocks.Add(new Paragraph(new Run($"Total Profit: {TotalProfit:C}")));
                    document.Blocks.Add(summarySection);

                    // Add transactions table
                    var table = new Table();
                    table.Columns.Add(new TableColumn { Width = new GridLength(80) });  // ID
                    table.Columns.Add(new TableColumn { Width = new GridLength(150) }); // Date
                    table.Columns.Add(new TableColumn { Width = new GridLength(200) }); // Customer
                    table.Columns.Add(new TableColumn { Width = new GridLength(100) }); // Total

                    // Add header row
                    var headerRow = new TableRow();
                    headerRow.Cells.Add(new TableCell(new Paragraph(new Run("ID"))));
                    headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Date"))));
                    headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Customer"))));
                    headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Total"))));

                    var rowGroup = new TableRowGroup();
                    rowGroup.Rows.Add(headerRow);

                    // Add data rows
                    foreach (var transaction in _allTransactions)
                    {
                        var row = new TableRow();
                        row.Cells.Add(new TableCell(new Paragraph(new Run(transaction.TransactionId.ToString()))));
                        row.Cells.Add(new TableCell(new Paragraph(new Run(transaction.TransactionDate.ToString("g")))));
                        row.Cells.Add(new TableCell(new Paragraph(new Run(transaction.CustomerName ?? ""))));
                        row.Cells.Add(new TableCell(new Paragraph(new Run(transaction.TotalAmount.ToString("C")))));
                        rowGroup.Rows.Add(row);
                    }

                    table.RowGroups.Add(rowGroup);
                    document.Blocks.Add(table);

                    printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "Transaction History Report");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"Error printing report: {ex.Message}");
            }
        }
    }
}