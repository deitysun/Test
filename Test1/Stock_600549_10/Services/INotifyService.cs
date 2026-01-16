using StockTenDayLineWarning.Models.Entities;

namespace StockTenDayLineWarning.Services
{
    public interface INotifyService
    {
        // 邮件预警（保留）
        Task SendEmailWarningAsync(StockWarning warning);

        // 微信预警（免费测试号）
        Task SendWeChatWarningAsync(StockWarning warning);
    }
}