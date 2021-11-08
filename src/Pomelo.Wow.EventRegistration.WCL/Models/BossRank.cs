using System;

namespace Pomelo.Wow.EventRegistration.WCL.Models
{
    public class BossRank
    {
        public string Name { get; set; }

        public float Highest { get; set; }

        public TimeSpan Fastest { get; set; }

        public int Parse { get; set; }

        public int ItemLevel { get; set; }
    }
}
