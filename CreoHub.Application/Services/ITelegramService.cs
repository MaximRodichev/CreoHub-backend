namespace CreoHub.Application.Services;

public interface ITelegramService
{
    Task Notify<T>(T data);
}