namespace CreoHub.Application.Exceptions;

class ProductAccessDeniedException : Exception
{
    public ProductAccessDeniedException() : base() { }
    
    public ProductAccessDeniedException(Guid productId) 
        : base($"Access denied error: You aren't owns a product with {productId}") { }
}