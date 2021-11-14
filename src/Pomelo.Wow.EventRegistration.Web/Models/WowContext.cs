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

        public DbSet<Guild> Guilds { get; set; }

        public DbSet<GuildManager> GuildManagers { get; set; }

        public DbSet<Item> Items { get; set; }

        public DbSet<Raid> Raids { get; set; }

        public DbSet<Registration> Registrations { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<UserSession> UserSessions { get; set; }

        public async ValueTask InitAsync()
        {
            if (Database.EnsureCreated())
            {
                Raids.Add(new Raid
                {
                    Id = 1,
                    BossList = "不稳定的海度斯,鱼斯拉,盲眼者莱欧瑟拉斯,深水领主卡拉瑟雷斯,莫洛格里·踏潮者,瓦丝琪",
                    Name = "毒蛇神殿",
                    ItemLevelEntrance = 115,
                    ItemLevelPreference = 120,
                    ItemLevelGraduated = 128,
                    ItemLevelFarm = 130,
                    EstimatedDuration = 2.5f
                });

                Raids.Add(new Raid
                {
                    Id = 2,
                    BossList = "奥,空灵机甲,大星术师索兰莉安,凯尔萨斯·逐日者",
                    Name = "风暴要塞",
                    ItemLevelEntrance = 115,
                    ItemLevelPreference = 120,
                    ItemLevelGraduated = 128,
                    ItemLevelFarm = 130,
                    EstimatedDuration = 2f
                });

                await SaveChangesAsync();
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Activity>(e =>
            {
                e.HasMany(x => x.Registrations).WithOne(x => x.Activity).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Charactor>(e =>
            {
                e.HasIndex(x => new { x.Name, x.Realm });
                e.HasMany<Registration>().WithOne(x => x.Charactor).OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Guild>(e =>
            {
                e.HasIndex(x => x.Name).IsFullText();
                e.HasIndex(x => x.Faction);
                e.HasMany(x => x.Managers).WithOne(x => x.Guild).OnDelete(DeleteBehavior.Cascade);
                e.HasMany<Activity>().WithOne(x => x.Guild).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<GuildManager>(e =>
            {
                e.HasKey(x => new { x.GuildId, x.UserId });
            });

            builder.Entity<User>(e =>
            {
                e.HasIndex(x => x.Username).IsUnique();
                e.HasMany<UserSession>().WithOne(x => x.User).OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
