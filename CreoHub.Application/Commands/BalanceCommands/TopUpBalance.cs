using CreoHub.Application.DTO;
using CreoHub.Application.DTO.BalanceDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Commands.BalanceCommands;

public record TopUpBalanceCommand(Guid UserId, decimal Amount)
    : IRequest<BaseResponse<TopUpResultDTO>>;

public class TopUpBalanceHandler : IRequestHandler<TopUpBalanceCommand, BaseResponse<TopUpResultDTO>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserTransactionRepository _transactionRepository;
    private readonly IPaymentGatewayService _paymentService;

    public TopUpBalanceHandler(
        IUnitOfWork unitOfWork,
        IUserTransactionRepository transactionRepository,
        IPaymentGatewayService paymentService)
    {
        _unitOfWork = unitOfWork;
        _transactionRepository = transactionRepository;
        _paymentService = paymentService;
    }

    public async Task<BaseResponse<TopUpResultDTO>> Handle(
        TopUpBalanceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Amount <= 0)
                return BaseResponse<TopUpResultDTO>.Fail("Amount must be greater than zero.");

            // Создаём OxaPay инвойс (trackId используется как идентификатор в webhook)
            var invoice = await _paymentService.CreateInvoiceAsync(
                request.Amount,
                $"topup-{request.UserId}");

            var transaction = UserTransaction.CreateUpBalance(
                request.Amount,
                request.UserId,
                invoice.TrackId);

            await _transactionRepository.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return BaseResponse<TopUpResultDTO>.Success(new TopUpResultDTO
            {
                PaymentUrl = invoice.PaymentUrl,
                TrackId    = invoice.TrackId
            });
        }
        catch (Exception ex)
        {
            return BaseResponse<TopUpResultDTO>.Fail(ex.Message);
        }
    }
}
