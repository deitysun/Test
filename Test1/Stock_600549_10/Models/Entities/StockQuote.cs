using System.ComponentModel.DataAnnotations;

namespace StockTenDayLineWarning.Models.Entities
{
    /// <summary>
    /// 股票每日行情数据实体（含成交量）
    /// </summary>
    public class StockQuote
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 股票代码（如 600549.SH 厦门钨业）
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string StockCode { get; set; } = string.Empty;

        /// <summary>
        /// 股票名称
        /// </summary>
        [MaxLength(50)]
        public string StockName { get; set; } = string.Empty;

        /// <summary>
        /// 交易日期
        /// </summary>
        [Required]
        public DateTime TradeDate { get; set; }

        /// <summary>
        /// 当日收盘价
        /// </summary>
        [Required]
        public decimal ClosePrice { get; set; }

        /// <summary>
        /// 当日开盘价
        /// </summary>
        public decimal OpenPrice { get; set; }

        /// <summary>
        /// 当日最高价
        /// </summary>
        public decimal HighPrice { get; set; }

        /// <summary>
        /// 当日最低价
        /// </summary>
        public decimal LowPrice { get; set; }

        /// <summary>
        /// 计算得到的10日均线值
        /// </summary>
        public decimal? TenDayAverage { get; set; }

        /// <summary>
        /// 当日成交量（单位：手）
        /// </summary>
        public long Volume { get; set; } = 0;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}