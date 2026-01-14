using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MySystem.Areas.Identity.Models;

namespace MySystem.Areas.Identity.Controllers
{
    public class AccountController : Controller
    {
        // 注入Identity核心服务
        private readonly UserManager<UserModel> _userManager;
        private readonly SignInManager<UserModel> _signInManager;

        public AccountController(UserManager<UserModel> userManager, SignInManager<UserModel> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        #region 注册功能
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. 实例化自定义用户模型，赋值基础字段和扩展字段
                var user = new UserModel
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    //RealName = model.RealName,
                    //BirthDate = model.BirthDate,
                    //Address = model.Address,
                    EmailConfirmed = true // 简化示例，直接标记邮箱已验证（实际项目需发送验证邮件）
                };

                // 2. 调用UserManager.CreateAsync创建用户（自动处理密码哈希加密）
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // 3. 用户创建成功，调用SignInManager.SignInAsync实现自动登陆
                    await _signInManager.SignInAsync(user, isPersistent: false); // isPersistent：是否持久化登陆（记住我）
                    return RedirectToAction("Index", "Home"); // 跳转首页
                }

                // 4. 注册失败，将错误信息添加到ModelState，返回视图展示
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // 模型验证失败或注册失败，返回注册视图
            return View(model);
        }
        #endregion

        //#region 登陆功能
        //// GET: /Account/Login
        //[AllowAnonymous]
        //public IActionResult Login(string returnUrl = null)
        //{
        //    ViewData["ReturnUrl"] = returnUrl; // 保存跳转前的URL，登陆成功后返回
        //    return View();
        //}

    

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                // 1. 调用SignInManager.PasswordSignInAsync实现密码登陆
                // lockoutOnFailure：登陆失败是否锁定账户（需配合Identity锁定策略）
                var result = await _signInManager.PasswordSignInAsync(
                    model.UserName,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    // 2. 登陆成功，跳转回原请求页面（若无则跳转首页）
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }

                // 3. 登陆失败，添加错误信息
                ModelState.AddModelError(string.Empty, "无效的用户名或密码，请重试。");
            }

            // 验证失败或登陆失败，返回登陆视图
            return View(model);
        }


        //#region 登出功能
        //// POST: /Account/Logout
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Logout()
        //{
        //    // 1. 调用SignInManager.SignOutAsync实现登出
        //    await _signInManager.SignOutAsync();
        //    return RedirectToAction("Index", "Home");
        //}
        //#endregion

        //#region 权限不足页面
        //[AllowAnonymous]
        //public IActionResult AccessDenied()
        //{
        //    return View();
        //}
        //#endregion
    
}
}
