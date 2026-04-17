namespace CreoHub.Application.DTO.BalanceDTOs;

public class BalanceDTO
{
    public decimal AvailableAmount { get; set; }
    public decimal PendingAmount   { get; set; }
}

public class TopUpResultDTO
{
    public string PaymentUrl { get; set; } = string.Empty;
    public string TrackId    { get; set; } = string.Empty;
}

public class TopUpBalanceRequest
{
    public decimal Amount { get; set; }
}
