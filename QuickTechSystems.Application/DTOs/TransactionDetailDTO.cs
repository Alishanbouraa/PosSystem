namespace QuickTechSystems.Application.DTOs
{
    public class TransactionDetailDTO
    {
        public int TransactionDetailId { get; set; }
        public int TransactionId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductBarcode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal PurchasePrice { get; set; } // Add this property
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
    }
}