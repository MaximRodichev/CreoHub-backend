namespace CreoHub.Application.Services;

public interface IVideoConversionService
{
    Task ConvertAsync(Guid storageObjectId, CancellationToken cancellationToken);
}