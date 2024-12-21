using QuickTechSystems.Application.DTOs;

namespace QuickTechSystems.Application.Services.Interfaces
{
    public interface IProfitService
    {
        Task<ProfitDTO> GetProfitByDateAsync(DateTime startDate, DateTime endDate);
    }
}