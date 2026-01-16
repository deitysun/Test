using Quartz;
using StockTenDayLineWarning.Services;

namespace StockTenDayLineWarning.Jobs
{
    /// <summary>
    /// 股票监控定时作业（10日线+量价配合）
    /// </summary>
    public class StockMonitorJob : IJob
    {
        private readonly IStockQuantifyService _quantifyService;
        // 要监控的股票列表
        private readonly List<string> _monitorStocks = new List<string> { "600549.SH", "000001.SZ" };

        public StockMonitorJob(IStockQuantifyService quantifyService)
        {
            _quantifyService = quantifyService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            foreach (var stockCode in _monitorStocks)
            {
                try
                {
                    var history = await _quantifyService.GetStockHistoryQuotesAsync(stockCode);
                    var with10Day = _quantifyService.CalculateTenDayAverage(history);
                    await _quantifyService.JudgeTradeWarningAsync(with10Day);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"【{DateTime.Now}】监控{stockCode}失败：{ex.Message}");
                }
            }
        }
    }
}