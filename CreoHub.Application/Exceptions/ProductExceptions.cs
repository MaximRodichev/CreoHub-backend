namespace CreoHub.Application.Exceptions;

[Serializable]
class InvalidProductIdException : Exception
{
    public InvalidProductIdException() {  }

    public InvalidProductIdException(Guid id)
        : base(String.Format("Invalid Shop Id: {0}", id))
    {

    }
}

[Serializable]
class ProductNotFoundException : Exception
{
    public ProductNotFoundException() {  }

    public ProductNotFoundException(Guid id)
    : base(String.Format("Product not found: {0}", id))
    {
        
    }
}