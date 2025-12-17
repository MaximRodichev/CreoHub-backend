namespace CreoHub.Application.Exceptions;

[Serializable]
class InvalidAccountIdException : Exception
{
    public InvalidAccountIdException() {  }

    public InvalidAccountIdException(Guid id)
        : base(String.Format("Yout account not found, provided Id is {0}", id))
    {

    }
}