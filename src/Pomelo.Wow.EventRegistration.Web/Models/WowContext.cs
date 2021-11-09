using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Pomelo.Wow.EventRegistration.Web.Models
{
    public class WowContext : DbContext
    {
        public WowContext(DbContextOptions<WowContext> opt) : base(opt)
        { }

        public DbSet<Activity> Activities { get; set; }

        public DbSet<Charactor> Charactors { get; set; }

        public DbSet<Item> Items { get; set; }

        public DbSet<Raid> Raids { get; set; }

        public DbSet<Registration> Registrations { get; set; }

        public DbSet<User> Users { get; set; }

        public async ValueTask InitAsync()
        {
            if (Database.EnsureCreated())
            {
                Users.Add(new User
                { 
                    Id = 1,
                    Username = "yuko",
                    PasswordHash = "19931101",
                    Role = UserRole.Admin,
                    DisplayName = "萌小柚"
                });

                Activities.Add(new Activity
                {
                    Deadline = Convert.ToDateTime("2021-11-12 11:00"),
                    Server = ActivityServer.Official,
                    Description = "晚7点开组，自备全程合剂药水烹饪，115+装等进本，DPS60%分G",
                    Raids = "2",
                    Realm = "龙之召唤",
                    UserId = 1,
                    Name = "Mirai公会团11月12日风暴全通"
                });

                Activities.Add(new Activity
                {
                    Deadline = Convert.ToDateTime("2021-11-13 11:00"),
                    Server = ActivityServer.Official,
                    Description = "晚7点开组，自备全程合剂药水烹饪，115+装等进本，DPS60%分G",
                    Raids = "1",
                    Realm = "龙之召唤",
                    UserId = 1,
                    Name = "Mirai公会团11月13日毒蛇全通"
                });

                Raids.Add(new Raid
                {
                    Id = 1,
                    BossList = "不稳定的海度斯,鱼斯拉,盲眼者莱欧瑟拉斯,深水领主卡拉瑟雷斯,莫洛格里·踏潮者,瓦丝琪",
                    Name = "毒蛇神殿",
                    ItemLevelEntrance = 115,
                    ItemLevelPreference = 120,
                    ItemLevelGraduated = 128
                });

                Raids.Add(new Raid
                {
                    Id = 2,
                    BossList = "奥,空灵机甲,大星术师索兰莉安,凯尔萨斯·逐日者",
                    Name = "风暴要塞",
                    ItemLevelEntrance = 115,
                    ItemLevelPreference = 120,
                    ItemLevelGraduated = 128
                });

                await SaveChangesAsync();
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Charactor>(e =>
            {
                e.HasIndex(x => new { x.Name, x.Realm });
                e.HasMany<Registration>().WithOne(x => x.Charactor).OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<User>(e =>
            {
                e.HasIndex(x => x.Username).IsUnique();
            });
        }
    }
}
