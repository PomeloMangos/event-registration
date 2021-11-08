using System;

namespace Pomelo.Wow.EventRegistration.Web.Models
{
    [Flags]
    public enum Class
    {
        Warrior = 1,
        Paladin = 2,
        Hunter = 4,
        Shaman = 8,
        Rogue = 16,
        Druid = 32,
        Warlock = 64,
        Mage = 128,
        Priest = 256,


        Tank = Warrior | Paladin | Druid | Warlock,
        Melee = Warrior | Paladin | Shaman | Rogue,
        Ranged = Hunter | Shaman | Druid | Warlock | Mage | Priest,
        DPS = Melee & Ranged,
        Healer = Paladin | Shaman | Druid | Priest
    }
}
