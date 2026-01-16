using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StockTenDayLineWarning.Models.Entities;

namespace StockTenDayLineWarning.Data
{
    /// <summary>
    /// 股票系统数据上下文（集成ASP.NET Core Identity，基于VS2022 LocalDB）
    /// </summary>
    public class StockDbContext : IdentityDbContext<IdentityUser>
    {
        /// <summary>
        /// 股票行情数据表
        /// </summary>
        public DbSet<StockQuote> StockQuotes { get; set; }

        /// <summary>
        /// 股票预警记录表
        /// </summary>
        public DbSet<StockWarning> StockWarnings { get; set; }

        /// <summary>
        /// 构造函数：配置数据库连接（LocalDB）
        /// </summary>
        /// <param name="options"></param>
        public StockDbContext(DbContextOptions<StockDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// 模型创建时的配置（避免重复数据，先执行Identity的模型配置）
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 必须先调用基类方法，初始化Identity相关表结构
            base.OnModelCreating(modelBuilder);

            // 配置 StockQuote 表：按股票代码和交易日期建立唯一索引
            modelBuilder.Entity<StockQuote>()
                .HasIndex(q => new { q.StockCode, q.TradeDate })
                .IsUnique();
        }
    }
}