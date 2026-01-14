using Microsoft.EntityFrameworkCore;
using MySystem.Areas.Identity.Models;

namespace MySystem.Areas.Identity.Data
{
    
        // 继承 DbContext
        public class ApplicationDbContext : DbContext
        {
            // 构造函数：接收 DbContextOptions<ApplicationDbContext> 并传递给父类
            public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
                : base(options)
            {
            }

            // 定义数据库表对应的 DbSet（示例：产品表）
            public DbSet<UserModel> users { get; set; }
        }
    
}
