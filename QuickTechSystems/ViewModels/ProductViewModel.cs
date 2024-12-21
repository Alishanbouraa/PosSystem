using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using QuickTechSystems.Application.DTOs;
using QuickTechSystems.Application.Services.Interfaces;
using QuickTechSystems.WPF.Commands;

namespace QuickTechSystems.WPF.ViewModels
{
    public class ProductViewModel : ViewModelBase
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IBarcodeService _barcodeService;
        private readonly ISupplierService _supplierService;
        private ObservableCollection<ProductDTO> _products;
        private ObservableCollection<CategoryDTO> _categories;
        private ObservableCollection<SupplierDTO> _suppliers;
        private ProductDTO? _selectedProduct;
        private bool _isEditing;
        private BitmapImage? _barcodeImage;
        private string _searchText = string.Empty;
        private bool _isLoading;
        private string _statusMessage = string.Empty;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ObservableCollection<ProductDTO> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        public ObservableCollection<CategoryDTO> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        public ObservableCollection<SupplierDTO> Suppliers
        {
            get => _suppliers;
            set => SetProperty(ref _suppliers, value);
        }

        public ProductDTO? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                SetProperty(ref _selectedProduct, value);
                IsEditing = value != null;
                if (value?.BarcodeImage != null)
                {
                    LoadBarcodeImage(value.BarcodeImage);
                }
                else
                {
                    BarcodeImage = null;
                }
            }
        }

        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        public BitmapImage? BarcodeImage
        {
            get => _barcodeImage;
            set => SetProperty(ref _barcodeImage, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                _ = SearchProductsAsync();
            }
        }

        public ICommand LoadCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand GenerateBarcodeCommand { get; }
        public ICommand GenerateAutomaticBarcodeCommand { get; }

        public ProductViewModel(
            IProductService productService,
            ICategoryService categoryService,
            IBarcodeService barcodeService,
            ISupplierService supplierService)
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
            _barcodeService = barcodeService ?? throw new ArgumentNullException(nameof(barcodeService));
            _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
            _products = new ObservableCollection<ProductDTO>();
            _categories = new ObservableCollection<CategoryDTO>();
            _suppliers = new ObservableCollection<SupplierDTO>();

            LoadCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
            AddCommand = new RelayCommand(_ => AddNew());
            SaveCommand = new AsyncRelayCommand(async _ => await SaveAsync());
            DeleteCommand = new AsyncRelayCommand(async _ => await DeleteAsync());
            GenerateBarcodeCommand = new RelayCommand(_ => GenerateBarcode());
            GenerateAutomaticBarcodeCommand = new RelayCommand(_ => GenerateAutomaticBarcode());

            _ = LoadDataAsync();
        }

        protected override async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading data...";

                var products = await _productService.GetAllAsync();
                var categories = await _categoryService.GetAllAsync();
                var suppliers = await _supplierService.GetAllAsync();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Products = new ObservableCollection<ProductDTO>(products.OrderBy(p => p.Name));
                    Categories = new ObservableCollection<CategoryDTO>(categories.OrderBy(c => c.Name));
                    Suppliers = new ObservableCollection<SupplierDTO>(suppliers.OrderBy(s => s.Name));

                    StatusMessage = $"Loaded {Products.Count} products";
                });
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"Error loading data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
        private void AddNew()
        {
            SelectedProduct = new ProductDTO
            {
                IsActive = true,
                CreatedAt = DateTime.Now,
                CurrentStock = 0,
                MinimumStock = 0
            };
            BarcodeImage = null;
            StatusMessage = "Creating new product";
        }

        private async Task SaveAsync()
        {
            try
            {
                if (SelectedProduct == null) return;

                var validationErrors = ValidateProduct(SelectedProduct);
                if (validationErrors.Any())
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        MessageBox.Show(string.Join("\n", validationErrors), "Validation Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning));
                    return;
                }

                NormalizeProductData(SelectedProduct);

                if (await IsDuplicateBarcode(SelectedProduct))
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        MessageBox.Show("A product with this barcode already exists.", "Duplicate Barcode",
                            MessageBoxButton.OK, MessageBoxImage.Warning));
                    return;
                }

                StatusMessage = "Saving product...";
                IsLoading = true;

                if (SelectedProduct.ProductId == 0)
                {
                    await _productService.CreateAsync(SelectedProduct);
                    StatusMessage = "Product created successfully";
                }
                else
                {
                    SelectedProduct.UpdatedAt = DateTime.Now;
                    await _productService.UpdateAsync(SelectedProduct);
                    StatusMessage = "Product updated successfully";
                }

                await LoadDataAsync();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    MessageBox.Show(StatusMessage, "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"Error saving product: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private List<string> ValidateProduct(ProductDTO product)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(product.Name))
                errors.Add("Product name is required.");

            if (product.CategoryId == 0)
                errors.Add("Please select a category.");

            if (product.SalePrice < 0)
                errors.Add("Sale price cannot be negative.");

            if (product.PurchasePrice < 0)
                errors.Add("Purchase price cannot be negative.");

            if (product.MinimumStock < 0)
                errors.Add("Minimum stock cannot be negative.");

            if (product.SalePrice < product.PurchasePrice)
                errors.Add("Sale price cannot be less than purchase price.");

            return errors;
        }

        private void NormalizeProductData(ProductDTO product)
        {
            product.Name = product.Name.Trim();
            product.Barcode = string.IsNullOrWhiteSpace(product.Barcode) ?
                GenerateDefaultBarcode() : product.Barcode.Trim();
            product.Description = product.Description?.Trim();
        }

        private string GenerateDefaultBarcode()
        {
            return $"P{DateTime.Now:yyyyMMddHHmmss}";
        }

        private async Task<bool> IsDuplicateBarcode(ProductDTO product)
        {
            if (string.IsNullOrWhiteSpace(product.Barcode)) return false;

            var existingProduct = await _productService.GetByBarcodeAsync(product.Barcode);
            return existingProduct != null && existingProduct.ProductId != product.ProductId;
        }

        private async Task DeleteAsync()
        {
            try
            {
                if (SelectedProduct == null) return;

                // Check if product has any related transactions or inventory history
                var canDelete = await _productService.CanDeleteProductAsync(SelectedProduct.ProductId);
                if (!canDelete)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        MessageBox.Show("This product cannot be deleted because it has related transactions. Consider deactivating it instead.",
                            "Cannot Delete", MessageBoxButton.OK, MessageBoxImage.Warning));
                    return;
                }

                if (MessageBox.Show("Are you sure you want to delete this product? This action cannot be undone.",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    StatusMessage = "Deleting product...";
                    IsLoading = true;
                    await _productService.DeleteAsync(SelectedProduct.ProductId);
                    await LoadDataAsync();
                    StatusMessage = "Product deleted successfully";
                }
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"Error deleting product: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SearchProductsAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    await LoadDataAsync();
                    return;
                }

                StatusMessage = "Searching products...";
                IsLoading = true;

                var searchTerms = SearchText.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var filteredProducts = Products.Where(p =>
                    searchTerms.All(term =>
                        p.Name.ToLower().Contains(term) ||
                        p.Barcode.ToLower().Contains(term) ||
                        p.CategoryName.ToLower().Contains(term) ||
                        (p.Description?.ToLower().Contains(term) ?? false) ||
                        (p.SupplierName?.ToLower().Contains(term) ?? false)));

                Products = new ObservableCollection<ProductDTO>(filteredProducts);
                StatusMessage = $"Found {Products.Count} matching products";
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"Error searching products: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void GenerateBarcode()
        {
            if (SelectedProduct == null || string.IsNullOrWhiteSpace(SelectedProduct.Barcode))
            {
                MessageBox.Show("Please enter a barcode value first.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                StatusMessage = "Generating barcode...";
                var barcodeData = _barcodeService.GenerateBarcode(SelectedProduct.Barcode);
                LoadBarcodeImage(barcodeData);
                SelectedProduct.BarcodeImage = barcodeData;
                StatusMessage = "Barcode generated successfully";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating barcode: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Error generating barcode";
            }
        }

        private void GenerateAutomaticBarcode()
        {
            if (SelectedProduct == null)
            {
                MessageBox.Show("Please select a product first.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                StatusMessage = "Generating automatic barcode...";
                var timestamp = DateTime.Now.ToString("yyMMddHHmmss");
                var categoryPrefix = SelectedProduct.CategoryId.ToString().PadLeft(3, '0');
                var barcode = $"{categoryPrefix}{timestamp}";

                SelectedProduct.Barcode = barcode;
                var barcodeData = _barcodeService.GenerateBarcode(barcode);
                LoadBarcodeImage(barcodeData);
                SelectedProduct.BarcodeImage = barcodeData;
                StatusMessage = "Automatic barcode generated successfully";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating automatic barcode: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Error generating automatic barcode";
            }
        }

        private void LoadBarcodeImage(byte[] imageData)
        {
            try
            {
                var image = new BitmapImage();
                using (var ms = new MemoryStream(imageData))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                }
                BarcodeImage = image;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading barcode image: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                BarcodeImage = null;
            }
        }
    }
}