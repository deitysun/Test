using System.ComponentModel.DataAnnotations;

namespace StockTenDayLineWarning.Models.Entities
{
    /// <summary>
    /// 股票买卖预警记录实体（含触发量能）
    /// </summary>
    public class StockWarning
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 股票代码
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
        /// 预警类型（买入/卖出）
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string WarningType { get; set; } = string.Empty; // "买入" 或 "卖出"

        /// <summary>
        /// 预警价格（触发预警时的收盘价）
        /// </summary>
        [Required]
        public decimal WarningPrice { get; set; }

        /// <summary>
        /// 10日均线值（触发预警时的十日线）
        /// </summary>
        [Required]
        public decimal TenDayAverage { get; set; }

        /// <summary>
        /// 预警触发时间
        /// </summary>
        [Required]
        public DateTime TriggerTime { get; set; }

        /// <summary>
        /// 预警状态（未处理/已查看）
        /// </summary>
        [MaxLength(10)]
        public string Status { get; set; } = "未处理";

        /// <summary>
        /// 触发预警时的成交量（单位：手）
        /// </summary>
        public long TriggerVolume { get; set; } = 0;
    }
}