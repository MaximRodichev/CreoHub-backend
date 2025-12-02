namespace CreoHub.CRM.Domain.Exceptions;

[Serializable]
public class NegativeOrZeroPriceException : Exception
{
    public NegativeOrZeroPriceException ()
    {}

    public NegativeOrZeroPriceException(string message)
        : base("Цена не может быть отрицательной или равна нуля")
    {
        
    }
}