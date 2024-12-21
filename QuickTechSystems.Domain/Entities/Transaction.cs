using QuickTechSystems.Domain.Entities;
using QuickTechSystems.Domain.Enums;

namespace QuickTechSystems.Domain.Entities
{
    public class Transaction
    {

        public int TransactionId { get; set; }
        public DateTime TransactionDate { get; set; }
        public virtual ICollection<TransactionDetail> Details { get; set; } = new List<TransactionDetail>();
        public int? CustomerId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Balance { get; set; }
        public TransactionType TransactionType { get; set; }
        public TransactionStatus Status { get; set; }

        // Navigation properties
        public virtual Customer? Customer { get; set; }
        public virtual ICollection<TransactionDetail> TransactionDetails { get; set; } = new List<TransactionDetail>();
    }
}