using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.DTO.StatsDTOs;

namespace CreoHub.Application.DTO.ShopDTOs;

public class ShopDashboardDTO
{
    public ShopStatsDTO Stats { get; set; }
    public List<ProductStatsDTO> TopProducts { get; set; }
    public List<OrderShortInfoDTO> RecentDeals { get; set; }
    public List<TagStatsDTO> TagAnalytics { get; set; }
}