using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using Pomelo.Wow.EventRegistration.WCL.Models;

namespace Pomelo.Wow.EventRegistration.WCL
{
    public static class Fetcher
    {
        private static HttpClient clientWcl = new HttpClient() { BaseAddress = new Uri("https://classic.warcraftlogs.com") };
        private static HttpClient clientWclCN = new HttpClient() { BaseAddress = new Uri("https://cn.classic.warcraftlogs.com") };
        private static HttpClient clientWowhead = new HttpClient() { BaseAddress = new Uri("https://tbc.wowhead.com") };
        private static HttpClient clientWowheadCN = new HttpClient() { BaseAddress = new Uri("https://cn.tbc.wowhead.com") };
        private static Regex equipmentRegex = new Regex("(?<=tbc.wowhead.com/item=)[0-9]{1,}");
        private static Regex itemLevelRegex = new Regex("(?<=item level )[0-9]{1,}");
        private static Regex imageUrlRegex = new Regex("(?<=<link rel=\"image_src\" href=\").*(?=\">)");
        private static Regex itemNameRegex = new Regex("(?<=<title>)((?!&mdash).)*");
        private static Regex qualityRegex = new Regex("(?<=\"quality\":)[0-9]{1,}");

        public static string ApiKey = "f6933f6f7489bdcda1f779fa7ee79f71";

        public static async ValueTask<Charactor> FetchAsync(string name, string realm, CharactorRole role)
        {
            var html = await GetWclHomePageAsync(name, realm);
            if (html == null)
            {
                return null;
            }

            var charactor = new Charactor();
            charactor.Name = name;
            charactor.Realm = realm;
            charactor.Role = role;
            charactor.Equipments = GetItemIds(html);
            charactor.BossRanks = await GetBossRanksAsync(name, realm, role);
            if (charactor.BossRanks.Count() > 0)
            {
                charactor.HighestItemLevel = charactor.BossRanks.Where(x => !x.Name.Contains("凯尔萨斯")).Max(x => x.ItemLevel);
            }

            return charactor;
        }

        private static async ValueTask<string> GetWclHomePageAsync(string name, string realm)
        {
            using (var response = await clientWcl.GetAsync($"/charactor/CN/{realm}/{name}"))
            {
                var html = await response.Content.ReadAsStringAsync();
                if (html.Contains("No charactor could be found"))
                {
                    return null;
                }

                return html;
            }
        }

        private static IEnumerable<int> GetItemIds(string html)
        {
            return equipmentRegex.Matches(html).Select(x => Convert.ToInt32(x.Value)).Distinct();
        }

        public static async ValueTask<Equipment> FetchEquipmentAsync(int itemId, int retry = 3)
        {
            try
            {
                Equipment ret = null;
                using (var response = await clientWowhead.GetAsync($"/item={itemId}"))
                {
                    ret = new Equipment();
                    var html = await response.Content.ReadAsStringAsync();
                    ret.Id = itemId;
                    ret.ItemLevel = Convert.ToInt32(itemLevelRegex.Match(html).Value);
                    ret.ImageUrl = imageUrlRegex.Match(html).Value;
                    ret.Name = await GetEquipmentNameCNAsync(itemId);
                    ret.Quality = (EquipmentQuality)Convert.ToInt32(qualityRegex.Match(html).Value);
                    ret.Position = GetEquipmentPosition(html);
                }

                return ret;
            }
            catch
            { 
                if (retry == 0)
                {
                    return null;
                }

                return await FetchEquipmentAsync(itemId, --retry);
            }
        }

        private static async ValueTask<string> GetEquipmentNameCNAsync(int itemId)
        {
            using (var response = await clientWowheadCN.GetAsync($"/item={itemId}"))
            {
                var html = await response.Content.ReadAsStringAsync();
                return itemNameRegex.Match(html).Value;
            }
        }

        private static EquipmentPosition GetEquipmentPosition(string html)
        {
            html = html.Split("\n").Where(x => x.Contains("tooltip_enus")).First();
            
            for (var i = 0; i < 11; ++i) 
            {
                var position = (EquipmentPosition)i;
                var str = position.ToString();
                if (html.Contains(str))
                {
                    return position;
                }
            }

            if (html.Contains("Main Hand"))
            {
                return EquipmentPosition.MainHand;
            }
            else if (html.Contains("Two-Hand"))
            {
                return EquipmentPosition.TwoHand;
            }
            else if (html.Contains("Held In Off-hand"))
            {
                return EquipmentPosition.HeldInOffHand;
            }
            else if (html.Contains("Off Hand"))
            {
                return EquipmentPosition.OffHand;
            }
            else if (html.Contains("One Hand"))
            {
                return EquipmentPosition.OneHand;
            }
            else
            {
                return EquipmentPosition.Unknown;
            }
        }

        private static async ValueTask<List<BossRank>> GetBossRanksAsync(string name, string realm, CharactorRole role)
        {
            var metric = "dps";
            if (role == CharactorRole.Healer)
            {
                metric = "hps";
            }

            using (var response = await clientWclCN.GetAsync($"/v1/rankings/character/{name}/{realm}/CN?includeCombatantInfo=true&metric={metric}&api_key={ApiKey}"))
            {
                var jsonStr = await response.Content.ReadAsStringAsync();
                var obj = JsonConvert.DeserializeObject<IEnumerable<WclBattleRecord>>(jsonStr);
                var ret = new List<BossRank>();
                foreach (var boss in obj)
                {
                    ret.Add(new BossRank
                    {
                        Parse = Convert.ToInt32(boss.Percentile),
                        Fastest = new TimeSpan(0, 0, boss.Duration / 1000),
                        Highest = boss.Total,
                        Name = boss.EncounterName,
                        ItemLevel = boss.IlvlKeyOrPatch
                    });
                }

                return ret;
            }
        }
    }
}
