using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using StockTenDayLineWarning.Data;
using StockTenDayLineWarning.Jobs;
using StockTenDayLineWarning.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. 添加 MVC 控制器和视图支持
builder.Services.AddControllersWithViews();

// 2. 注册 LocalDB 数据库（集成Identity）
builder.Services.AddDbContext<StockDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("StockDbContext"));
});

// 3. 注册 ASP.NET Core Identity 服务（使用现有LocalDB，默认UI，简化密码策略）
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    // 简化密码策略（开发/测试环境，生产环境可恢复严格策略）
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<StockDbContext>(); // 关联现有StockDbContext，用户表存储在现有数据库中

// 4. 注册量化服务
builder.Services.AddScoped<IStockQuantifyService, StockQuantifyService>();

// 5. 注册预警推送服务（包含微信预警）
builder.Services.AddScoped<INotifyService, NotifyService>();

// 6. 注册 Quartz 定时任务
builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("StockMonitorJob");
    q.AddJob<StockMonitorJob>(opts => opts.WithIdentity(jobKey));
    // 每日15:30执行（A股收盘后）
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("StockMonitorTrigger")
        .WithCronSchedule("0 30 15 * * ?"));
});

// 7. 启动 Quartz
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

// 8. 配置全局授权策略：所有页面默认需要授权（未登录无法访问，自动跳转登录页）
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser() // 要求用户已认证（登录）
        .Build();
});

var app = builder.Build();

// 中间件配置（顺序不可乱：Authentication → Authorization → MVC/Razor Pages）
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 9. 启用Identity认证中间件
app.UseAuthentication();

// 10. 启用授权中间件
app.UseAuthorization();

// 11. 配置 MVC 默认路由
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 12. 配置Identity默认路由（支持注册/登录/注销等Razor Pages页面）
app.MapRazorPages();

app.Run();