namespace CreoHub.CRM.Domain.Entities;

public class Price
{
    public DateTime Date { get; set; }
    public double PriceAtMoment { get; set; }
    
    //FK
    public Product Product { get; set; }
}