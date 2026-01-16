using StockTenDayLineWarning.Models.Entities;
using StockTenDayLineWarning.Models.ViewModels;

namespace StockTenDayLineWarning.Services
{
    /// <summary>
    /// 股票量化服务接口（10日线+量价配合）
    /// </summary>
    public interface IStockQuantifyService
    {
        /// <summary>
        /// 获取股票历史行情+成交量数据（新浪免费API）
        /// </summary>
        /// <param name="stockCode">股票代码</param>
        /// <returns></returns>
        Task<List<StockQuote>> GetStockHistoryQuotesAsync(string stockCode);

        /// <summary>
        /// 计算10日均线（核心量化逻辑）
        /// </summary>
        /// <param name="stockQuotes">股票行情列表（按交易日期升序排列）</param>
        /// <returns></returns>
        List<StockQuote> CalculateTenDayAverage(List<StockQuote> stockQuotes);

        /// <summary>
        /// 判定买卖预警信号（10日线战法+量价配合）
        /// </summary>
        /// <param name="stockQuotes">包含10日均线+成交量的行情列表</param>
        /// <returns></returns>
        Task<StockWarning?> JudgeTradeWarningAsync(List<StockQuote> stockQuotes);

        /// <summary>
        /// 组装首页视图模型（用于页面展示）
        /// </summary>
        /// <param name="stockCode">股票代码</param>
        /// <returns></returns>
        Task<StockIndexViewModel> GetStockIndexViewModelAsync(string stockCode);
    }
}