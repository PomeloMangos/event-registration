using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pomelo.Wow.EventRegistration.Web.Models
{
    public enum ActivityServer
    { 
        Official,
        Private
    }

    public enum ActivityVisibility
    {
        Public,
        Internal
    }

    public class Activity
    {
        public long Id { get; set; }

        [MaxLength(64)]
        public string Name { get; set; }

        public string Description { get; set; }

        [MaxLength(64)]
        public string Realm { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime Deadline { get; set; }

        public DateTime Begin { get; set; }

        public float EstimatedDurationInHours { get; set; }

        public ActivityServer Server { get; set; }

        public string Raids { get; set; }

        public string Extension1 { get; set; } = "{}";

        public string Extension2 { get; set; } = "{}";

        public string Extension3 { get; set; } = "{}";

        [ForeignKey(nameof(User))]
        public int UserId { get; set; }

        public virtual User User { get; set; }

        public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();

        [MaxLength(32)]
        public string GuildId { get; set; }

        public virtual Guild Guild { get; set; }

        public ActivityVisibility Visibility { get; set; }

        [MaxLength(64)]
        public string Password { get; set; }

        public int ItemLevelEntrance { get; set; }

        public int ItemLevelPreference { get; set; }

        public int ItemLevelGraduated { get; set; }

        public int ItemLevelFarm { get; set; }
    }
}
