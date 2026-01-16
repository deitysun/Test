using StockTenDayLineWarning.Models.Entities;

namespace StockTenDayLineWarning.Models.ViewModels
{
    /// <summary>
    /// 股票首页展示视图模型（含成交量）
    /// </summary>
    public class StockIndexViewModel
    {
        /// <summary>
        /// 股票基本信息
        /// </summary>
        public string StockCode { get; set; } = string.Empty;
        public string StockName { get; set; } = string.Empty;

        /// <summary>
        /// 最新行情数据
        /// </summary>
        public decimal LatestClosePrice { get; set; }
        public decimal? LatestTenDayAverage { get; set; }
        public DateTime LatestTradeDate { get; set; }

        /// <summary>
        /// 成交量相关（前端展示）
        /// </summary>
        public long LatestVolume { get; set; } = 0; // 最新交易日成交量
        public long Avg5DayVolume { get; set; } = 0; // 近5日平均成交量

        /// <summary>
        /// 近15日行情与10日均线数据（用于页面绘制趋势图）
        /// </summary>
        public List<StockQuote> RecentStockQuotes { get; set; } = new List<StockQuote>();

        /// <summary>
        /// 未处理预警列表
        /// </summary>
        public List<StockWarning> UnhandledWarnings { get; set; } = new List<StockWarning>();

        /// <summary>
        /// 最新预警信息（用于页面弹窗提示）
        /// </summary>
        public string LatestWarningMsg { get; set; } = string.Empty;
    }
}