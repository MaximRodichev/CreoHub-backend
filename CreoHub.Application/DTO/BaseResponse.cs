namespace CreoHub.Application.DTO;

public enum ResponseStatus
{
    Success,
    Error,
    NotFound,
}

public record BaseResponse<T>
{
    public ResponseStatus Status { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public T Data { get; set; }

    public static BaseResponse<T> Success(T data)
    {
        return new BaseResponse<T>()
        {
            Data = data,
            Status = ResponseStatus.Success,
        };
    }

    public static BaseResponse<T> Fail(string errorMessage)
    {
        return new BaseResponse<T>()
        {
            Data = default,
            ErrorMessage = errorMessage,
            Status = ResponseStatus.Error
        };
    }
}