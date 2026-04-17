namespace CreoHub.Application.Services;

public interface IThumbnailGenerationService
{
    Task GenerateAsync(Guid storageObjectId, CancellationToken cancellationToken);
}