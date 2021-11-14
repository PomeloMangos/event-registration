using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pomelo.Wow.EventRegistration.Web.Models
{
    public class Guild
    {
        [MaxLength(32)]
        public string Id { get; set; }

        [MaxLength(64)]
        public string Name { get; set; }

        [MaxLength(64)]
        public string Realm { get; set; }

        public string Description { get; set; }

        [ForeignKey(nameof(User))]
        public int UserId { get; set; } // Owner

        [MaxLength(256)]
        public string GuildLogoUrl { get; set; }

        public virtual User User { get; set; }

        public virtual ICollection<GuildManager> Managers { get; set; } = new List<GuildManager>();
    }
}
