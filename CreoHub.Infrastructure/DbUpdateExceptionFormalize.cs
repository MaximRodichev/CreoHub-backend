using Microsoft.EntityFrameworkCore;

namespace CreoHub.Application.Exceptions;

public class DbUpdateExceptionFormalize 
{
    public static string Formalize(DbUpdateException ex)
    {

        // PostgreSQL
        /*
        if (ex.InnerException is PostgresException pgEx)
        {
            if (pgEx.SqlState == "23505")
                return "Запись с таким уникальным ключом уже существует.";
        }*/
        var constraintName = ex.InnerException.Data["ConstraintName"];
        var sqlState = ex.InnerException.Data["SqlState"];
        int state = sqlState != null ? int.Parse(sqlState.ToString()) : 0;
        switch (state)
        {
            case 23505:
                switch (constraintName)
                {
                    case "IX_Orders_ProductId_CustomerId":
                        return
                            "Ошибка при сохранении данных: пара значений Product+Customer. Возможно вы уже заказывали такой товар";
                    default:
                        return "Ошибка при сохранении данных: Уникальность ключа";
                    
                }
                
            
            case 0:
            default:
                return "Ошибка при сохранении данных: " + ex.Message;
        }
        // По умолчанию
        return "Ошибка при сохранении данных: " + ex.Message;
    }
}