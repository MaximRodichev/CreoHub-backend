using System.ComponentModel;

namespace CreoHub.Domain.Types;

public enum OrderStatus
{
    [Description("Создан")]
    Created = 1,

    [Description("Отменён")]
    Canceled = 2,

    [Description("Завершён")]
    Completed = 3
}