using System.ComponentModel.DataAnnotations;

namespace MySystem.Areas.Identity.Models
{
    public class RegisterViewModel
    {
        #region 基础字段（必选）
        /// <summary>
        /// 用户名
        /// </summary>
        [Required(ErrorMessage = "用户名不能为空")]
        [Display(Name = "用户名")]
        [MaxLength(50, ErrorMessage = "用户名长度不能超过50个字符")]
        [MinLength(3, ErrorMessage = "用户名长度不能少于3个字符")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "用户名只能包含字母、数字和下划线，不能有特殊字符")]
        public string UserName { get; set; } = string.Empty; // 初始化避免空引用

        /// <summary>
        /// 邮箱（用于登录/找回密码）
        /// </summary>
        [Required(ErrorMessage = "邮箱不能为空")]
        [Display(Name = "邮箱")]
        [EmailAddress(ErrorMessage = "请输入合法的邮箱格式")] // 内置邮箱格式验证
        [MaxLength(100, ErrorMessage = "邮箱长度不能超过100个字符")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 密码
        /// </summary>
        [Required(ErrorMessage = "密码不能为空")]
        [Display(Name = "密码")]
        [DataType(DataType.Password)] // 渲染为 password 输入框，隐藏输入内容
        [MinLength(6, ErrorMessage = "密码长度不能少于6个字符")]
        [MaxLength(20, ErrorMessage = "密码长度不能超过20个字符")]
        [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*\d).+$", ErrorMessage = "密码必须包含字母和数字，提升安全性")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 确认密码（核心：与密码保持一致）
        /// </summary>
        [Required(ErrorMessage = "请确认密码")]
        [Display(Name = "确认密码")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "两次输入的密码不一致，请重新输入")] // 对比 Password 字段，验证一致性
        public string ConfirmPassword { get; set; } = string.Empty;
        #endregion

        #region 可选字段（根据业务需求扩展）
        /// <summary>
        /// 手机号
        /// </summary>
        [Display(Name = "手机号（可选）")]
        [Phone(ErrorMessage = "请输入合法的手机号格式")] // 内置手机号格式验证
        [MaxLength(11, ErrorMessage = "手机号长度不能超过11位")]
        public string? PhoneNumber { get; set; } // 可选字段，使用可空类型

        /// <summary>
        /// 同意用户协议（必选，合规要求）
        /// </summary>
        [Display(Name = "同意《用户协议》和《隐私政策》")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "必须同意用户协议才能完成注册")] // 强制必须勾选为 true
        public bool AgreeTerms { get; set; } = false;
        #endregion
    }
}
