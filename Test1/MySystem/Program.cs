using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using MySystem.Areas.Identity.Data;
using MySystem.Areas.Identity.Models;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 2. 配置Identity数据库上下文（连接SQL Server，使用内置IdentityUser）
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ERPConnection")));

// 3. 注册Identity服务（核心：使用内置IdentityUser模型）
builder.Services.AddDefaultIdentity<UserModel>(options =>
{
    // 密码复杂度配置（ERP安全要求，可根据需求调整）
    options.Password.RequireDigit = true; // 要求包含数字
    options.Password.RequireLowercase = true; // 要求小写字母
    options.Password.RequireUppercase = true; // 要求大写字母
    options.Password.RequireNonAlphanumeric = false; // 不要求特殊字符
    options.Password.RequiredLength = 6; // 密码最小长度6
    // 登录配置
    options.SignIn.RequireConfirmedAccount = false; // 无需验证邮箱即可登录
})
.AddEntityFrameworkStores<ApplicationDbContext>(); // 关联数据库上下文

// 4. 配置认证Cookie（可选：调整登录跳转、有效期）
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login"; // 未登录跳转地址
    options.ExpireTimeSpan = TimeSpan.FromHours(2); // 登录有效期
    options.Cookie.HttpOnly = true; // 禁止前端JS访问Cookie
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // 生产环境强制HTTPS
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
// 4. 启用认证中间件（必须在UseRouting之后，UseAuthorization之前）
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    // ① 配置 Area 传统路由（必须包含 {area} 占位符）
    endpoints.MapAreaControllerRoute(
        name: "AdminArea", // 路由名称（唯一，自定义）
        areaName: "Identity", // 关联的 Area 名称（与目录/特性一致）
        pattern: "Identity/{controller=Account}/{action=Login}/{id?}" // 路由模板
    );

    // ② 配置普通 MVC 路由（非 Area 控制器使用）
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}"
    );
});

app.Run();
