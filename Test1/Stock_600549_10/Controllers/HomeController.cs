using Microsoft.AspNetCore.Mvc;
using StockTenDayLineWarning.Models.ViewModels;
using StockTenDayLineWarning.Services;

namespace StockTenDayLineWarning.Controllers
{
    public class HomeController : Controller
    {
        private readonly IStockQuantifyService _stockQuantifyService;

        public HomeController(IStockQuantifyService stockQuantifyService)
        {
            _stockQuantifyService = stockQuantifyService;
        }

        /// <summary>
        /// 首页：展示股票10日线+量价配合数据和预警信息（需登录才能访问）
        /// </summary>
        public async Task<IActionResult> Index(string stockCode = "600549.SH")
        {
            var viewModel = await _stockQuantifyService.GetStockIndexViewModelAsync(stockCode);
            return View(viewModel);
        }
    }
}