using System.Collections.ObjectModel;
using System.Windows.Input;
using QuickTechSystems.Application.DTOs;
using QuickTechSystems.Application.Services.Interfaces;
using QuickTechSystems.WPF.Commands;

namespace QuickTechSystems.WPF.ViewModels
{
    public class CategoryViewModel : ViewModelBase
    {
        private readonly ICategoryService _categoryService;
        private ObservableCollection<CategoryDTO> _categories;
        private CategoryDTO? _selectedCategory;
        private bool _isEditing;

        public ObservableCollection<CategoryDTO> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        public CategoryDTO? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                SetProperty(ref _selectedCategory, value);
                IsEditing = value != null;
            }
        }

        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        public ICommand LoadCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }

        public CategoryViewModel(ICategoryService categoryService)
        {
            _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
            _categories = new ObservableCollection<CategoryDTO>();

            LoadCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
            AddCommand = new RelayCommand(_ => AddNew());
            SaveCommand = new AsyncRelayCommand(async _ => await SaveAsync());
            DeleteCommand = new AsyncRelayCommand(async _ => await DeleteAsync());

            _ = LoadDataAsync();
        }

        protected override async Task LoadDataAsync()
        {
            try
            {
                var categories = await _categoryService.GetAllAsync();
                Categories = new ObservableCollection<CategoryDTO>(categories);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading categories: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddNew()
        {
            SelectedCategory = new CategoryDTO
            {
                IsActive = true
            };
        }

        private async Task SaveAsync()
        {
            try
            {
                if (SelectedCategory == null) return;

                if (string.IsNullOrWhiteSpace(SelectedCategory.Name))
                {
                    MessageBox.Show("Category name is required.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (SelectedCategory.CategoryId == 0)
                {
                    await _categoryService.CreateAsync(SelectedCategory);
                }
                else
                {
                    await _categoryService.UpdateAsync(SelectedCategory);
                }

                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving category: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteAsync()
        {
            try
            {
                if (SelectedCategory == null) return;

                if (MessageBox.Show("Are you sure you want to delete this category?",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    await _categoryService.DeleteAsync(SelectedCategory.CategoryId);
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting category: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}