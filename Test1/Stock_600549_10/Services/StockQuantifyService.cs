using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using StockTenDayLineWarning.Data;
using StockTenDayLineWarning.Models.Entities;
using StockTenDayLineWarning.Models.ViewModels;
using System.Globalization;

namespace StockTenDayLineWarning.Services
{
    /// <summary>
    /// 股票量化服务实现类（10日线战法+量价配合精准预警 + LocalDB + 微信预警）
    /// </summary>
    public class StockQuantifyService : IStockQuantifyService
    {
        private readonly StockDbContext _dbContext;
        private readonly HttpClient _httpClient;
        private readonly INotifyService _notifyService;
        // 新浪财经API基础地址（行情+成交量）
        private const string SinaDailyApi = "https://finance.sina.com.cn/stock/chartdata/index.php";
        private const string SinaRealTimeApi = "https://hq.sinajs.cn/list=";

        #region 10日线战法+量价配合 核心可配置参数（按需微调）
        // 10日线基础参数
        private const decimal BreakthroughRate = 0.005m; // 有效突破/破位比例：0.5%
        private const int MaxBelowDays = 3; // 买入前线下最大天数：3天
        private const int MaxAboveDays = 5; // 卖出前线上最大天数：5天
        private const decimal TrendFlatRate = 0.003m; // 趋势走平比例：0.3%
        private const int TrendCheckDays = 3; // 趋势判定天数：近3日
        private const int RepeatWarnInterval = 2; // 重复预警间隔：2个交易日

        // 成交量辅助过滤参数（量价配合）
        private const int VolAvgDays = 5; // 成交量平均天数：近5日
        private const decimal BuyVolAvgRate = 1.5m; // 买入：当日量≥近5日均量1.5倍
        private const decimal BuyVolPrevRate = 1.2m; // 买入：当日量≥前一日量1.2倍
        private const decimal SellVolAvgRate = 1.3m; // 卖出：当日量≥近5日均量1.3倍
        private const decimal SellVolPrevRate = 1.1m; // 卖出：当日量≥前一日量1.1倍
        #endregion

        public StockQuantifyService(StockDbContext dbContext, INotifyService notifyService)
        {
            _dbContext = dbContext;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _notifyService = notifyService;
        }

        #region 获取股票历史行情+成交量数据（新浪免费API）
        public async Task<List<StockQuote>> GetStockHistoryQuotesAsync(string stockCode)
        {
            var sinaStockCode = ConvertToSinaStockCode(stockCode);
            if (string.IsNullOrEmpty(sinaStockCode))
                throw new ArgumentException("股票代码格式错误，示例：600549.SH 或 000001.SZ");

            // 先查本地LocalDB，避免重复调用API
            var localQuotes = await _dbContext.StockQuotes
                .Where(q => q.StockCode == stockCode)
                .OrderBy(q => q.TradeDate)
                .ToListAsync();

            if (localQuotes.Any() && localQuotes.Max(q => q.TradeDate) >= DateTime.Now.AddDays(-7))
                return localQuotes;

            // 调用新浪财经API（返回行情+成交量数据）
            var requestUrl = $"{SinaDailyApi}?symbol={sinaStockCode}&begin_date=20240101&end_date=20990101&type=oneajson";
            var response = await _httpClient.GetAsync(requestUrl);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"获取股票数据失败，API返回：{response.StatusCode}");

            var jsonStr = await response.Content.ReadAsStringAsync();
            var apiData = JsonConvert.DeserializeObject<SinaDailyStockData>(jsonStr);
            if (apiData?.Data == null || !apiData.Data.Items.Any())
                throw new Exception("API返回数据为空，股票代码可能无效");

            var stockQuotes = new List<StockQuote>();
            var stockName = apiData.Data.Name;
            foreach (var item in apiData.Data.Items)
            {
                if (!DateTime.TryParse(item[0].ToString(), out var tradeDate))
                    continue;

                // 解析核心行情数据
                var closePrice = Convert.ToDecimal(item[1], CultureInfo.InvariantCulture);
                var openPrice = Convert.ToDecimal(item[2], CultureInfo.InvariantCulture);
                var highPrice = Convert.ToDecimal(item[3], CultureInfo.InvariantCulture);
                var lowPrice = Convert.ToDecimal(item[4], CultureInfo.InvariantCulture);
                // 解析成交量数据（新浪API第5位为成交量，单位：手）
                var volume = long.TryParse(item[5]?.ToString(), out var vol) ? vol : 0;

                stockQuotes.Add(new StockQuote
                {
                    StockCode = stockCode,
                    StockName = stockName,
                    TradeDate = tradeDate,
                    ClosePrice = Math.Round(closePrice, 2),
                    OpenPrice = Math.Round(openPrice, 2),
                    HighPrice = Math.Round(highPrice, 2),
                    LowPrice = Math.Round(lowPrice, 2),
                    Volume = volume,
                    CreateTime = DateTime.Now
                });
            }

            // 覆盖本地旧数据，写入新行情+成交量
            _dbContext.StockQuotes.RemoveRange(localQuotes);
            await _dbContext.StockQuotes.AddRangeAsync(stockQuotes);
            await _dbContext.SaveChangesAsync();

            return stockQuotes;
        }
        #endregion

        #region 计算10日均线（核心逻辑）
        public List<StockQuote> CalculateTenDayAverage(List<StockQuote> stockQuotes)
        {
            if (stockQuotes.Count < 10) return stockQuotes;
            var sortedQuotes = stockQuotes.OrderBy(q => q.TradeDate).ToList();

            for (int i = 9; i < sortedQuotes.Count; i++)
            {
                var tenDayClose = sortedQuotes.Skip(i - 9).Take(10).Sum(q => q.ClosePrice);
                sortedQuotes[i].TenDayAverage = Math.Round(tenDayClose / 10, 2);
            }
            return sortedQuotes;
        }
        #endregion

        #region 核心：10日线战法+量价配合 精准预警判定
        public async Task<StockWarning?> JudgeTradeWarningAsync(List<StockQuote> stockQuotes)
        {
            // 1. 基础校验：数据量足够（10日线+趋势3日+成交量5日）
            var validQuotes = stockQuotes
                .Where(q => q.TenDayAverage.HasValue && q.Volume > 0) // 过滤无成交量/无10日线数据
                .OrderBy(q => q.TradeDate)
                .ToList();
            if (validQuotes.Count < TrendCheckDays + VolAvgDays + 1) return null;

            var latest = validQuotes.Last(); // 最新交易日（突破/破位当日）
            var prev = validQuotes[validQuotes.Count - 2]; // 前一交易日
            var trendQuotes = validQuotes.Skip(validQuotes.Count - TrendCheckDays).Take(TrendCheckDays).ToList(); // 近3日趋势
            var volQuotes = validQuotes.Skip(validQuotes.Count - VolAvgDays).Take(VolAvgDays).ToList(); // 近5日成交量（用于计算均量）
            string? warningType = null;

            // 2. 判定10日线趋势：向上/向下/走平（横盘震荡直接过滤）
            var trendType = GetTenDayTrendType(trendQuotes);
            if (trendType == TrendType.Flat) return null;

            // 3. 买入信号：10日线战法 + 量价齐升（双重过滤）
            // 修正：移除多余的 avgVol 参数，仅传入3个已定义的参数（prev、latest、trendQuotes）
            if (IsValidBuySignal(prev, latest, trendQuotes) && IsBuyVolumeMatch(latest, prev, volQuotes))
            {
                warningType = "买入";
            }
            // 4. 卖出信号：10日线战法 + 量价齐跌（双重过滤）
            else if (IsValidSellSignal(prev, latest, trendQuotes) && IsSellVolumeMatch(latest, prev, volQuotes))
            {
                warningType = "卖出";
            }

            // 5. 无有效信号直接返回
            if (string.IsNullOrEmpty(warningType)) return null;

            // 6. 高级过滤：剔除2个交易日内的重复同类型预警（避免频繁信号）
            var lastWarnTime = latest.TradeDate.AddDays(-RepeatWarnInterval);
            var isRepeatWarn = await _dbContext.StockWarnings
                .AnyAsync(w => w.StockCode == latest.StockCode
                && w.WarningType == warningType
                && w.TriggerTime >= lastWarnTime);
            if (isRepeatWarn) return null;

            // 7. 构建预警记录 + 触发微信预警
            var warning = new StockWarning
            {
                StockCode = latest.StockCode,
                StockName = latest.StockName,
                WarningType = warningType,
                WarningPrice = latest.ClosePrice,
                TenDayAverage = latest.TenDayAverage.Value,
                TriggerTime = DateTime.Now,
                Status = "未处理",
                TriggerVolume = latest.Volume
            };
            _dbContext.StockWarnings.Add(warning);
            await _dbContext.SaveChangesAsync();

            // 微信实时推送预警
            await _notifyService.SendWeChatWarningAsync(warning);

            return warning;
        }

        #region 子方法：10日线趋势判定
        /// <summary>
        /// 判定10日线趋势类型：向上/向下/走平
        /// </summary>
        private TrendType GetTenDayTrendType(List<StockQuote> trendQuotes)
        {
            var first = trendQuotes.First().TenDayAverage.Value;
            var last = trendQuotes.Last().TenDayAverage.Value;
            var changeRate = Math.Abs((last - first) / first);

            if (changeRate < TrendFlatRate) return TrendType.Flat;
            return last > first ? TrendType.Up : TrendType.Down;
        }

        /// <summary>
        /// 10日线趋势类型枚举
        /// </summary>
        private enum TrendType
        {
            Up,    // 向上
            Down,  // 向下
            Flat   // 走平
        }
        #endregion

        #region 子方法：10日线战法 买入/卖出信号判定
        /// <summary>
        /// 验证有效买入信号（10日线战法4大条件）
        /// </summary>
        private bool IsValidBuySignal(StockQuote prev, StockQuote latest, List<StockQuote> trendQuotes)
        {
            var tenDayAvg = latest.TenDayAverage.Value;
            var prevTenDayAvg = prev.TenDayAverage.Value;

            // 条件1：基础金叉（前一日线下，当日线上）
            var isCrossUp = prev.ClosePrice < prevTenDayAvg && latest.ClosePrice > tenDayAvg;
            if (!isCrossUp) return false;

            // 条件2：10日线向上趋势
            if (GetTenDayTrendType(trendQuotes) != TrendType.Up) return false;

            // 条件3：有效突破（收盘价高于10日线0.5%以上，剔除假突破）
            var isEffectiveBreak = (latest.ClosePrice - tenDayAvg) / tenDayAvg >= BreakthroughRate;
            if (!isEffectiveBreak) return false;

            // 条件4：金叉前线下天数≤3天（短期回踩，而非深度下跌）
            var belowDays = 0;
            var quotes = trendQuotes.OrderByDescending(q => q.TradeDate).ToList();
            foreach (var q in quotes)
            {
                if (q.ClosePrice < q.TenDayAverage.Value) belowDays++;
                else break;
            }
            return belowDays <= MaxBelowDays;
        }

        /// <summary>
        /// 验证有效卖出信号（10日线战法4大条件）
        /// </summary>
        private bool IsValidSellSignal(StockQuote prev, StockQuote latest, List<StockQuote> trendQuotes)
        {
            var tenDayAvg = latest.TenDayAverage.Value;
            var prevTenDayAvg = prev.TenDayAverage.Value;

            // 条件1：基础死叉（前一日线上，当日线下）
            var isCrossDown = prev.ClosePrice > prevTenDayAvg && latest.ClosePrice < tenDayAvg;
            if (!isCrossDown) return false;

            // 条件2：10日线向下趋势
            if (GetTenDayTrendType(trendQuotes) != TrendType.Down) return false;

            // 条件3：有效破位（收盘价低于10日线0.5%以上，剔除假破位）
            var isEffectiveBreak = (tenDayAvg - latest.ClosePrice) / tenDayAvg >= BreakthroughRate;
            if (!isEffectiveBreak) return false;

            // 条件4：死叉前线上天数≤5天（短期破位，而非趋势反转）
            var aboveDays = 0;
            var quotes = trendQuotes.OrderByDescending(q => q.TradeDate).ToList();
            foreach (var q in quotes)
            {
                if (q.ClosePrice > q.TenDayAverage.Value) aboveDays++;
                else break;
            }
            return aboveDays <= MaxAboveDays;
        }
        #endregion

        #region 子方法：成交量辅助过滤（量价配合判定）
        /// <summary>
        /// 买入成交量匹配：量价齐升（2条规则同时满足）
        /// </summary>
        private bool IsBuyVolumeMatch(StockQuote latest, StockQuote prev, List<StockQuote> volQuotes)
        {
            // 计算近5日平均成交量
            decimal avgVol1 = (decimal)volQuotes.Average(q => q.Volume);
            // 规则1：当日量 ≥ 近5日均量1.5倍
            var isVolOverAvg = latest.Volume >= avgVol1 * BuyVolAvgRate;
            // 规则2：当日量 ≥ 前一日量1.2倍
            var isVolOverPrev = latest.Volume >= prev.Volume * BuyVolPrevRate;

            return isVolOverAvg && isVolOverPrev;
        }

        /// <summary>
        /// 卖出成交量匹配：量价齐跌（2条规则同时满足）
        /// </summary>
        private bool IsSellVolumeMatch(StockQuote latest, StockQuote prev, List<StockQuote> volQuotes)
        {
            // 计算近5日平均成交量
            var avgVol1 = (decimal)volQuotes.Average(q => q.Volume);
            // 规则1：当日量 ≥ 近5日均量1.3倍
            var isVolOverAvg = latest.Volume >= avgVol1 * SellVolAvgRate;
            // 规则2：当日量 ≥ 前一日量1.1倍
            var isVolOverPrev = latest.Volume >= prev.Volume * SellVolPrevRate;

            return isVolOverAvg && isVolOverPrev;
        }
        #endregion
        #endregion

        #region 组装首页视图模型（含成交量展示）
        public async Task<StockIndexViewModel> GetStockIndexViewModelAsync(string stockCode)
        {
            var history = await GetStockHistoryQuotesAsync(stockCode);
            var with10Day = CalculateTenDayAverage(history);
            var warning = await JudgeTradeWarningAsync(with10Day);
            var unhandled = await _dbContext.StockWarnings
                .Where(w => w.StockCode == stockCode && w.Status == "未处理")
                .OrderByDescending(w => w.TriggerTime)
                .ToListAsync();

            var latestQuote = with10Day.OrderByDescending(q => q.TradeDate).FirstOrDefault();
            var recent15 = with10Day.OrderByDescending(q => q.TradeDate).Take(15).Reverse().ToList();

            // 计算近5日平均成交量（前端展示用）
            var avgVol = recent15.Count >= VolAvgDays
                ? Math.Round(recent15.Skip(recent15.Count - VolAvgDays).Take(VolAvgDays).Average(q => q.Volume), 0)
                : 0;

            return new StockIndexViewModel
            {
                StockCode = stockCode,
                StockName = latestQuote?.StockName ?? string.Empty,
                LatestClosePrice = latestQuote?.ClosePrice ?? 0,
                LatestTenDayAverage = latestQuote?.TenDayAverage,
                LatestTradeDate = latestQuote?.TradeDate ?? DateTime.Now,
                LatestVolume = latestQuote?.Volume ?? 0,
                Avg5DayVolume = (long)avgVol,
                RecentStockQuotes = recent15,
                UnhandledWarnings = unhandled,
                LatestWarningMsg = warning != null
                    ? $"{DateTime.Now:HH:mm:ss} 【{warning.WarningType}预警】{warning.StockName}({stockCode}) 价格：{warning.WarningPrice:F2} | 10日线：{warning.TenDayAverage:F2} | 量能：{warning.TriggerVolume:N0}手（10日线战法+量价配合精准信号）"
                    : "当前无10日线战法+量价配合有效信号，持续监控中..."
            };
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 股票代码格式转换（600549.SH → sh600549）
        /// </summary>
        private string ConvertToSinaStockCode(string stockCode)
        {
            if (!stockCode.Contains(".")) return string.Empty;
            var parts = stockCode.Split('.');
            if (parts[1].ToUpper() == "SH") return $"sh{parts[0]}";
            if (parts[1].ToUpper() == "SZ") return $"sz{parts[0]}";
            return string.Empty;
        }

        /// <summary>
        /// 新浪财经API数据模型（行情+成交量）
        /// </summary>
        public class SinaDailyStockData
        {
            [JsonProperty("data")]
            public SinaStockData Data { get; set; } = new SinaStockData();
        }

        public class SinaStockData
        {
            [JsonProperty("name")]
            public string Name { get; set; } = string.Empty;
            [JsonProperty("items")]
            public List<object[]> Items { get; set; } = new List<object[]>();
        }
        #endregion
    }
}