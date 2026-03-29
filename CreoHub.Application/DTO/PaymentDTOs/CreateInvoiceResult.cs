namespace CreoHub.Application.DTO.PaymentDTOs;

public record CreateInvoiceResult(
    string TrackId,
    string PaymentUrl,
    DateTime ExpiredAt
);
