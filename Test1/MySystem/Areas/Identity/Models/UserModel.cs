using Microsoft.AspNetCore.Identity;

namespace MySystem.Areas.Identity.Models
{

    // 核心：继承内置IdentityUser，添加自定义字段
    public class UserModel:IdentityUser
    {

        [PersonalData]
        public string EmployeeId { get; set; } = string.Empty; // 员工工号（ERP唯一标识）
    }
}
