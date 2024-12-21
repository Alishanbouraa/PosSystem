using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows.Documents;
using System.Windows.Controls;
using QuickTechSystems.Application.DTOs;
using QuickTechSystems.Application.Services.Interfaces;
using QuickTechSystems.WPF.Commands;
using QuickTechSystems.WPF.Views;
using QuickTechSystems.Domain.Enums;

namespace QuickTechSystems.WPF.ViewModels
{
    public class InventoryViewModel : ViewModelBase
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private ObservableCollection<ProductDTO> _lowStockProducts;
        private ObservableCollection<InventoryHistoryDTO> _recentMovements;
        private ObservableCollection<ProductDTO> _allProducts;
        private ProductDTO? _selectedProduct;
        private string _searchText = string.Empty;
        private DateTime _startDate = DateTime.Today.AddDays(-30);
        private DateTime _endDate = DateTime.Today;
        private decimal _totalInventoryValue;
        private int _totalProducts;
        private int _lowStockCount;

        public ObservableCollection<ProductDTO> LowStockProducts
        {
            get => _lowStockProducts;
            set => SetProperty(ref _lowStockProducts, value);
        }

        public ObservableCollection<InventoryHistoryDTO> RecentMovements
        {
            get => _recentMovements;
            set => SetProperty(ref _recentMovements, value);
        }

        public ObservableCollection<ProductDTO> AllProducts
        {
            get => _allProducts;
            set => SetProperty(ref _allProducts, value);
        }

        public ProductDTO? SelectedProduct
        {
            get => _selectedProduct;
            set => SetProperty(ref _selectedProduct, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                SearchProducts();
            }
        }

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                SetProperty(ref _startDate, value);
                _ = LoadMovementsAsync();
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                SetProperty(ref _endDate, value);
                _ = LoadMovementsAsync();
            }
        }

        public decimal TotalInventoryValue
        {
            get => _totalInventoryValue;
            set => SetProperty(ref _totalInventoryValue, value);
        }

        public int TotalProducts
        {
            get => _totalProducts;
            set => SetProperty(ref _totalProducts, value);
        }

        public int LowStockCount
        {
            get => _lowStockCount;
            set => SetProperty(ref _lowStockCount, value);
        }

        public ICommand LoadCommand { get; }
        public ICommand AdjustStockCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand PrintReportCommand { get; }

        public InventoryViewModel(IProductService productService, ICategoryService categoryService)
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));

            _lowStockProducts = new ObservableCollection<ProductDTO>();
            _recentMovements = new ObservableCollection<InventoryHistoryDTO>();
            _allProducts = new ObservableCollection<ProductDTO>();

            LoadCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
            AdjustStockCommand = new AsyncRelayCommand(async _ => await AdjustStockAsync());
            ExportCommand = new AsyncRelayCommand(async _ => await ExportInventoryAsync());
            RefreshCommand = new AsyncRelayCommand(async _ => await RefreshDataAsync());
            PrintReportCommand = new AsyncRelayCommand(async _ => await PrintInventoryReportAsync());

            _ = LoadDataAsync();
        }

        protected override async Task LoadDataAsync()
        {
            try
            {
                var products = await _productService.GetAllAsync();
                AllProducts = new ObservableCollection<ProductDTO>(products);

                var lowStock = await _productService.GetLowStockProductsAsync();
                LowStockProducts = new ObservableCollection<ProductDTO>(lowStock);
                LowStockCount = lowStock.Count();

                CalculateInventoryStatistics();
                await LoadMovementsAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"Error loading inventory data: {ex.Message}");
            }
        }

        private void CalculateInventoryStatistics()
        {
            TotalProducts = AllProducts.Count;
            TotalInventoryValue = AllProducts.Sum(p => p.CurrentStock * p.PurchasePrice);
        }

        private async Task LoadMovementsAsync()
        {
            try
            {
                // This would need to be implemented in your services
                var movements = new List<InventoryHistoryDTO>
                {
                    new InventoryHistoryDTO
                    {
                        Date = DateTime.Now,
                        ProductName = "Sample Product",
                        OperationType = TransactionType.Purchase,
                        QuantityChanged = 10,
                        Reference = "PO-001"
                    }
                    // Add more sample data or actual data from your service
                };

                RecentMovements = new ObservableCollection<InventoryHistoryDTO>(movements);
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"Error loading movements: {ex.Message}");
            }
        }

        private async Task SearchProducts()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AllProducts = new ObservableCollection<ProductDTO>(_allProducts);
                });
                return;
            }

            var filteredProducts = _allProducts.Where(p =>
                p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                p.Barcode.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                p.CategoryName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                AllProducts = new ObservableCollection<ProductDTO>(filteredProducts);
            });
        }

        private async Task AdjustStockAsync()
        {
            if (SelectedProduct == null)
            {
                await ShowErrorMessageAsync("Please select a product first.");
                return;
            }

            try
            {
                var dialog = new QuantityDialog(
                    SelectedProduct.Name,
                    SelectedProduct.CurrentStock,
                    SelectedProduct.CurrentStock  // Add the available stock parameter
                );

                if (dialog.ShowDialog() == true)
                {
                    var difference = dialog.NewQuantity - SelectedProduct.CurrentStock;
                    await _productService.UpdateStockAsync(SelectedProduct.ProductId, difference);

                    // Add to inventory history
                    var historyEntry = new InventoryHistoryDTO
                    {
                        Date = DateTime.Now,
                        ProductId = SelectedProduct.ProductId,
                        ProductName = SelectedProduct.Name,
                        QuantityChanged = difference,
                        OperationType = difference > 0 ? TransactionType.Adjustment : TransactionType.Adjustment,
                        Reference = "Manual Adjustment"
                    };

                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        RecentMovements.Insert(0, historyEntry);
                    });

                    await LoadDataAsync();

                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show("Stock adjusted successfully.", "Success",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"Error adjusting stock: {ex.Message}");
            }
        }

        private async Task ExportInventoryAsync()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    DefaultExt = "csv",
                    FileName = $"Inventory_Export_{DateTime.Now:yyyyMMdd}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var csv = new StringBuilder();
                    csv.AppendLine("Barcode,Name,Category,Current Stock,Minimum Stock,Purchase Price,Sale Price,Value");

                    foreach (var product in AllProducts)
                    {
                        csv.AppendLine($"\"{product.Barcode}\",\"{product.Name}\",\"{product.CategoryName}\"," +
                            $"{product.CurrentStock},{product.MinimumStock},{product.PurchasePrice:F2}," +
                            $"{product.SalePrice:F2},{product.CurrentStock * product.PurchasePrice:F2}");
                    }

                    await File.WriteAllTextAsync(saveFileDialog.FileName, csv.ToString());
                    MessageBox.Show("Inventory exported successfully.", "Export Complete",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"Error exporting inventory: {ex.Message}");
            }
        }

        private async Task RefreshDataAsync()
        {
            try
            {
                await LoadDataAsync();
                MessageBox.Show("Inventory data refreshed successfully.", "Refresh Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"Error refreshing data: {ex.Message}");
            }
        }

        private async Task PrintInventoryReportAsync()
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    var document = new FlowDocument();
                    var paragraph = new Paragraph();

                    // Add report header
                    paragraph.Inlines.Add(new Bold(new Run("Inventory Report\n")) { FontSize = 18 });
                    paragraph.Inlines.Add(new Run($"Generated: {DateTime.Now:g}\n\n"));

                    // Add summary section
                    paragraph.Inlines.Add(new Bold(new Run("Summary:\n")));
                    paragraph.Inlines.Add(new Run($"Total Products: {TotalProducts}\n"));
                    paragraph.Inlines.Add(new Run($"Total Value: {TotalInventoryValue:C}\n"));
                    paragraph.Inlines.Add(new Run($"Low Stock Items: {LowStockCount}\n\n"));

                    // Add products table
                    var table = new Table();
                    table.Columns.Add(new TableColumn { Width = new GridLength(100) });
                    table.Columns.Add(new TableColumn { Width = new GridLength(200) });
                    table.Columns.Add(new TableColumn { Width = new GridLength(100) });
                    table.Columns.Add(new TableColumn { Width = new GridLength(100) });

                    var headerRow = new TableRow();
                    headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Code"))));
                    headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Product"))));
                    headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Stock"))));
                    headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Value"))));
                    table.RowGroups.Add(new TableRowGroup());
                    table.RowGroups[0].Rows.Add(headerRow);

                    foreach (var product in AllProducts)
                    {
                        var row = new TableRow();
                        row.Cells.Add(new TableCell(new Paragraph(new Run(product.Barcode))));
                        row.Cells.Add(new TableCell(new Paragraph(new Run(product.Name))));
                        row.Cells.Add(new TableCell(new Paragraph(new Run(product.CurrentStock.ToString()))));
                        row.Cells.Add(new TableCell(new Paragraph(new Run((product.CurrentStock * product.PurchasePrice).ToString("C")))));
                        table.RowGroups[0].Rows.Add(row);
                    }

                    document.Blocks.Add(paragraph);
                    document.Blocks.Add(table);

                    printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "Inventory Report");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"Error printing report: {ex.Message}");
            }
        }

       
    }
}