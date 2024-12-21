using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace QuickTechSystems.WPF.Views
{
    public partial class ReturnItemsDialog : Window
    {
        public ObservableCollection<ReturnItemViewModel> SelectedItems { get; private set; }

        public ReturnItemsDialog(IEnumerable<TransactionDetailDTO> originalItems)
        {
            InitializeComponent();

            SelectedItems = new ObservableCollection<ReturnItemViewModel>(
                originalItems.Select(item => new ReturnItemViewModel
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName ?? string.Empty,
                    ProductBarcode = item.ProductBarcode ?? string.Empty,
                    Quantity = item.Quantity,
                    ReturnQuantity = 0,
                    UnitPrice = item.UnitPrice,
                    PurchasePrice = item.PurchasePrice
                }));

            ItemsGrid.ItemsSource = SelectedItems;
        }

        private async void ProcessReturn_Click(object sender, RoutedEventArgs e)
        {
            if (!SelectedItems.Any(i => i.ReturnQuantity > 0))
            {
                MessageBox.Show("Please select at least one item to return",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedItems.Any(i => i.ReturnQuantity > i.Quantity))
            {
                MessageBox.Show("Return quantity cannot exceed original quantity",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }
    }

    public class ReturnItemViewModel : INotifyPropertyChanged
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductBarcode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        private int _returnQuantity;
        public int ReturnQuantity
        {
            get => _returnQuantity;
            set
            {
                _returnQuantity = value;
                OnPropertyChanged();
            }
        }
        public decimal UnitPrice { get; set; }
        public decimal PurchasePrice { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}