﻿using System.Threading;
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

        public async ValueTask InitAsync()
        {
            if (Database.EnsureCreated())
            {
                Raids.Add(new Raid
                {
                    BossList = "不稳定的海度斯,鱼斯拉,盲眼者莱欧瑟拉斯,深水领主卡拉瑟雷斯,莫洛格里·踏潮者,瓦丝琪",
                    Name = "毒蛇神殿",
                    ItemLevelEntrance = 110,
                    ItemLevelPreference = 118,
                    ItemLevelGraduated = 125
                });
                Raids.Add(new Raid
                {
                    BossList = "奥,空灵机甲,大星术师索兰莉安,凯尔萨斯·逐日者",
                    Name = "风暴要塞",
                    ItemLevelEntrance = 110,
                    ItemLevelPreference = 118,
                    ItemLevelGraduated = 125
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
            });
        }
    }
}
