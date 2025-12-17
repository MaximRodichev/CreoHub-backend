namespace CreoHub.Application.Exceptions;

[Serializable]
class AccessOrderException : Exception
{
    public AccessOrderException() {  }

    public AccessOrderException(Guid id)
        : base(String.Format("Access exception: Not rights to see full info of order {0}", id))
    {
        
    }
}