using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace QuickTechSystems.WPF.Views
{
    public partial class QuantityDialog : Window, INotifyPropertyChanged
    {
        private string _productName = string.Empty;  // Initialize with empty string
        private int _currentQuantity;
        private int _newQuantity;
        private int _availableStock;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string ProductName
        {
            get => _productName;
            set
            {
                _productName = value;
                OnPropertyChanged(nameof(ProductName));
            }
        }

        public int CurrentQuantity
        {
            get => _currentQuantity;
            set
            {
                _currentQuantity = value;
                OnPropertyChanged(nameof(CurrentQuantity));
            }
        }

        public int NewQuantity
        {
            get => _newQuantity;
            set
            {
                _newQuantity = value;
                OnPropertyChanged(nameof(NewQuantity));
            }
        }

        public int AvailableStock
        {
            get => _availableStock;
            set
            {
                _availableStock = value;
                OnPropertyChanged(nameof(AvailableStock));
            }
        }

        public QuantityDialog(string productName, int currentQuantity, int availableStock)
        {
            InitializeComponent();
            DataContext = this;

            _productName = productName;
            _currentQuantity = currentQuantity;
            _newQuantity = currentQuantity;
            _availableStock = availableStock;
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (NewQuantity <= 0)
            {
                MessageBox.Show("Quantity must be greater than zero.",
                    "Invalid Input",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (NewQuantity > AvailableStock)
            {
                MessageBox.Show($"Quantity cannot exceed available stock ({AvailableStock}).",
                    "Invalid Input",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void IncrementQuantity(object sender, RoutedEventArgs e)
        {
            if (NewQuantity < AvailableStock)
                NewQuantity++;
        }

        private void IncrementQuantityBy5(object sender, RoutedEventArgs e)
        {
            NewQuantity = Math.Min(NewQuantity + 5, AvailableStock);
        }

        private void IncrementQuantityBy10(object sender, RoutedEventArgs e)
        {
            NewQuantity = Math.Min(NewQuantity + 10, AvailableStock);
        }

        private void SetMaxQuantity(object sender, RoutedEventArgs e)
        {
            NewQuantity = AvailableStock;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}