using System.Windows.Controls;
using System.Windows.Input;

namespace QuickTechSystems.WPF.Views
{
    public partial class TransactionView : UserControl
    {
        public TransactionView()
        {
            InitializeComponent();
        }

        private void BarcodeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                var vm = DataContext as TransactionViewModel;
                vm?.ProcessBarcodeInput();
            }
        }
        private void ProductSearchGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid grid && grid.SelectedItem is ProductDTO selectedProduct)
            {
                var viewModel = DataContext as TransactionViewModel;
                viewModel?.OnProductSelected(selectedProduct);
            }
        }

        private void ProductSearchGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is DataGrid grid && grid.SelectedItem is ProductDTO selectedProduct)
            {
                var viewModel = DataContext as TransactionViewModel;
                viewModel?.OnProductSelected(selectedProduct);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                var viewModel = DataContext as TransactionViewModel;
                if (viewModel != null)
                {
                    viewModel.IsProductSearchVisible = false;
                }
                e.Handled = true;
            }


        }
    }
}