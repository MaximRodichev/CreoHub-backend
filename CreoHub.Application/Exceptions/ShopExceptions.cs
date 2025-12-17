namespace CreoHub.Application.Exceptions;

[Serializable]
class InvalidShopIdException : Exception
{
    public InvalidShopIdException() {  }

    public InvalidShopIdException(Guid id)
        : base(String.Format("Invalid Shop Id: {0}", id))
    {

    }
}

[Serializable]
class NotFoundShopIdException : Exception
{
    public NotFoundShopIdException() {  }

    public NotFoundShopIdException(Guid id)
    : base(String.Format("Not found shop Id: {0}", id))
    {
        
    }
}