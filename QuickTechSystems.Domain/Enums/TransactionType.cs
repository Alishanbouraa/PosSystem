namespace QuickTechSystems.Domain.Enums
{
    public enum TransactionType
    {
        Sale,
        Return,
        Purchase,
        Adjustment
    }

    public enum TransactionStatus
    {
        Pending,
        Completed,
        Cancelled
    }
}