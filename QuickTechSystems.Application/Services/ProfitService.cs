using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuickTechSystems.Application.DTOs;
using QuickTechSystems.Application.Services.Interfaces;
using QuickTechSystems.Domain.Interfaces.Repositories;
using QuickTechSystems.Domain.Entities;
using QuickTechSystems.Domain.Enums;
namespace QuickTechSystems.Application.Services
{
    public class ProfitService : IProfitService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProfitService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ProfitDTO> GetProfitByDateAsync(DateTime startDate, DateTime endDate)
        {
            var transactions = await _unitOfWork.Context.Set<Transaction>()
                .Include(t => t.Details)
                .Where(t => t.TransactionDate >= startDate &&
                           t.TransactionDate <= endDate &&
                           t.Status == TransactionStatus.Completed)
                .ToListAsync();

            decimal totalSales = transactions.Sum(t => t.TotalAmount);
            decimal totalCost = transactions.SelectMany(t => t.Details)
                .Sum(d => d.PurchasePrice * d.Quantity);

            return new ProfitDTO
            {
                Date = DateTime.Now,
                TotalSales = totalSales,
                TotalCost = totalCost,
                GrossProfit = totalSales - totalCost
            };
        }
    }
}