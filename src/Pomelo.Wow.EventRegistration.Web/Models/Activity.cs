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

        public ActivityServer Server { get; set; }

        public string Raids { get; set; }

        public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    }
}
