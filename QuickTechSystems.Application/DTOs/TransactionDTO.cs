using System.Collections.ObjectModel;
using QuickTechSystems.Domain.Enums;

namespace QuickTechSystems.Application.DTOs
{
    public class TransactionDTO : BaseDTO
    {
        public int TransactionId { get; set; }
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Balance { get; set; }
        public decimal DiscountAmount { get; set; }  // Add this property
        public DateTime TransactionDate { get; set; }
        public TransactionType TransactionType { get; set; }
        public TransactionStatus Status { get; set; }
        public ObservableCollection<TransactionDetailDTO> Details { get; set; } = new ObservableCollection<TransactionDetailDTO>();
    }
}