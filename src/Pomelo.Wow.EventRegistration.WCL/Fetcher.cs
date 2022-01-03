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
        private static Regex itemLevelRegex = new Regex("(?<=\"level\":)[0-9]{1,}");
        private static Regex imageUrlRegex = new Regex("(?<=<link rel=\"image_src\" href=\").*(?=\">)");
        private static Regex itemNameRegex = new Regex("(?<=<title>)((?!&mdash).)*");
        private static Regex qualityRegex = new Regex("(?<=\"quality\":)[0-9]{1,}");
        private static Regex classRegex = new Regex("(?<=<div id=\"character-class\" class=\").*(?=\">)");
        private static string apiKey = null;

        public static void SetApiKey(string key)
        {
            apiKey = key;
        }

        public static async ValueTask<Charactor> FetchAsync(string name, string realm, CharactorRole role, int partition)
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
            charactor.BossRanks = await GetBossRanksAsync(name, realm, role, partition);
            charactor.Class = classRegex.Match(html).Value;
            if (charactor.BossRanks.Count() > 0)
            {
                var ignoreHighest = charactor.BossRanks.Where(x => !x.Name.Contains("凯尔萨斯")).FirstOrDefault();
                if (ignoreHighest == null)
                {
                    charactor.HighestItemLevel = charactor.BossRanks.Where(x => !x.Name.Contains("凯尔萨斯")).Max(x => x.ItemLevel);
                }
                else
                {
                    charactor.HighestItemLevel = charactor.BossRanks.Where(x => !x.Name.Contains("凯尔萨斯") && x.ItemLevel != ignoreHighest.ItemLevel).Max(x => x.ItemLevel);
                }
            }

            return charactor;
        }

        private static async ValueTask<string> GetWclHomePageAsync(string name, string realm)
        {
            using (var response = await clientWcl.GetAsync($"/character/CN/{realm}/{name}"))
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
                    var quality = qualityRegex
                        .Matches(html)
                        .Cast<Match>()
                        .Select(x => Convert.ToInt32(x.Value))
                        .GroupBy(x => x)
                        .Select(x => new { x.Key, Count = x.Count() })
                        .OrderByDescending(x => x.Count)
                        .Select(x => x.Key)
                        .FirstOrDefault();
                    ret.Quality = (EquipmentQuality)quality;
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

        private static async ValueTask<List<BossRank>> GetBossRanksAsync(string name, string realm, CharactorRole role, int partition)
        {
            var metric = "dps";
            if (role == CharactorRole.Healer)
            {
                metric = "hps";
            }

            using (var response = await clientWclCN.GetAsync($"/v1/parses/character/{name}/{realm}/CN?includeCombatantInfo=true&metric={metric}&partition={partition}&api_key={apiKey}"))
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

                return ret.GroupBy(x => x.Name)
                    .Select(x => new BossRank
                    {
                        Name = x.Key,
                        Slowest = x.Max(x => x.Fastest),
                        Fastest = x.Min(x => x.Fastest),
                        AverageDuration = new TimeSpan(Convert.ToInt64(x.Average(x => x.Fastest.Ticks))),
                        Highest = x.Max(x => x.Highest),
                        Lowest = x.Min(x => x.Highest),
                        ItemLevel = x.Max(x => x.ItemLevel),
                        Killed = x.Count(),
                        Parse = x.Max(x => x.Parse)
                    })
                    .ToList();
            }
        }
    }
}
