namespace QuickTechSystems.Application.DTOs
{
    public class ProfitDTO
    {
        public DateTime Date { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalCost { get; set; }
        public decimal GrossProfit { get; set; }
    }
}