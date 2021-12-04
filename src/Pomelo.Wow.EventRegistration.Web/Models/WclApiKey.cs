using System.ComponentModel.DataAnnotations;

namespace Pomelo.Wow.EventRegistration.Web.Models
{
    public class WclApiKey
    {
        [MaxLength(32)]
        public string Id { get; set; }
    }
}
