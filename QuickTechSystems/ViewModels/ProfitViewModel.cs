using System;
using System.Threading.Tasks;
using QuickTechSystems.Application.DTOs;
using QuickTechSystems.Application.Services.Interfaces;

namespace QuickTechSystems.WPF.ViewModels
{
    public class ProfitViewModel : ViewModelBase
    {
        private readonly IProfitService _profitService;
        private DateTime _startDate;
        private DateTime _endDate;
        private ProfitDTO? _profit;

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                {
                    _ = LoadProfitAsync();
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
                    _ = LoadProfitAsync();
                }
            }
        }

        public ProfitDTO? Profit
        {
            get => _profit;
            set => SetProperty(ref _profit, value);
        }

        public ProfitViewModel(IProfitService profitService)
        {
            _profitService = profitService;
            _startDate = DateTime.Today;
            _endDate = DateTime.Today;

            _ = LoadProfitAsync();
        }

        private async Task LoadProfitAsync()
        {
            try
            {
                Profit = await _profitService.GetProfitByDateAsync(StartDate, EndDate);
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"Error loading profit data: {ex.Message}");
            }
        }
    }
}