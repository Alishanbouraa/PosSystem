namespace QuickTechSystems.Application.DTOs
{
    public class DebtPaymentDTO
    {
        public int Id { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public decimal Amount { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public PaymentStatus Status { get; set; } = PaymentStatus.Completed;
        public int TransactionId { get; set; }
        public decimal RemainingBalance { get; set; }
    }

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Cancelled,
        Failed
    }
}