using System.ComponentModel.DataAnnotations;

namespace MySystem.Areas.Identity.Models
{
    public class LoginViewModel
    {
        /// <summary>
        /// 用户名（支持邮箱/手机号/账号登录）
        /// </summary>
        [Required(ErrorMessage = "用户名不能为空")] // 必填项验证
        [Display(Name = "用户名")] // 视图中表单标签的显示名称（替代属性名）
        [MaxLength(50, ErrorMessage = "用户名长度不能超过50个字符")] // 最大长度限制
        public string UserName { get; set; } = string.Empty; // 初始化避免空引用

        /// <summary>
        /// 密码
        /// </summary>
        [Required(ErrorMessage = "密码不能为空")] // 必填项验证
        [Display(Name = "密码")]
        [DataType(DataType.Password)] // 标记为密码类型，视图中会渲染为 password 输入框（隐藏输入内容）
        [MinLength(6, ErrorMessage = "密码长度不能少于6个字符")] // 最小长度限制
        [MaxLength(20, ErrorMessage = "密码长度不能超过20个字符")] // 最大长度限制
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 记住我（可选字段，用于持久化登录）
        /// </summary>
        [Display(Name = "记住我")]
        public bool RememberMe { get; set; } = false; // 默认不记住
    }
}
