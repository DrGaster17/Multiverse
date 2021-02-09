using System.Collections.Generic;
using System.IO;
using Multiverse.API;
using Newtonsoft.Json;
using System;

namespace Multiverse
{
    public static class Configs
    {
        public static string Dir = $"{Paths.BotPath}/Configs";
        public static string BotConfigPath = $"{Dir}/Bot.json";
        public static string StaffConfigPath = $"{Dir}/Staff.json";
        public static BotConfig BotConfig = null;
        public static StaffConfig StaffConfig = null;

        public static void Reload()
        {
            try
            {
                if (!Directory.Exists(Dir))
                    Directory.CreateDirectory(Dir);
                CreateDefault();
                BotConfig = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText(BotConfigPath));
                StaffConfig = JsonConvert.DeserializeObject<StaffConfig>(File.ReadAllText(StaffConfigPath));
            }
            catch (Exception e)
            {
                Logger.Error("UPDATE YOUR CONFIG!");
                Logger.Exception(e);
            }
        }

        public static void CreateDefault()
        {
            try
            {
                if (!File.Exists(BotConfigPath))
                {
                    File.Create(BotConfigPath).Close();
                    BotConfig = new BotConfig();
                    BotConfig.AllowLavaLink = false;
                    BotConfig.Debug = false;
                    BotConfig.ModLogChannel = 0;
                    BotConfig.Prefix = "!";
                    BotConfig.Token = "empty";
                    BotConfig.LavalinkPassword = "56TEq56TEq";
                    BotConfig.CommandsToDelete = new List<string>();
                    BotConfig.VerifiedRoleId = 0;
                    BotConfig.UnverifiedRoleId = 0;
                    BotConfig.ServerLogChannel = 0;
                    BotConfig.AutoMemeChannel = 0;
                    BotConfig.FactChannel = 0;
                    BotConfig.GiveawayChannel = 0;
                    BotConfig.Subreddit = "dankmemes";
                    BotConfig.InfoEmbed = new InfoEmbed
                    {
                        ChannelId = 0,
                        MessageId = 0,
                        ServerId = 0
                    };
                    BotConfig.RadioStations = new Dictionary<string, string>();
                    BotConfig.ReactionRoles = new Dictionary<ulong, Dictionary<string, ulong>>();
                    BotConfig.RolePersists = new Dictionary<ulong, List<ulong>>();
                    BotConfig.SelfRoles = new List<ulong>();
                    BotConfig.IgnoredIds = new List<ulong>();
                    Writer.Write(JsonConvert.SerializeObject(BotConfig), BotConfigPath);
                }

                if (!File.Exists(StaffConfigPath))
                {
                    File.Create(StaffConfigPath).Close();
                    StaffConfig = new StaffConfig();
                    StaffConfig.BotMasters = new List<ulong>();
                    StaffConfig.StaffIds = new List<ulong>();
                    Writer.Write(JsonConvert.SerializeObject(StaffConfig), StaffConfigPath);
                }
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }

        public static void SaveBotConfig()
        {
            try
            {
                if (File.Exists(BotConfigPath))
                    File.Delete(BotConfigPath);
                Writer.Write(JsonConvert.SerializeObject(BotConfig), BotConfigPath);
                Reload();
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }

        public static void SaveStaffConfig()
        {
            try
            {
                if (File.Exists(StaffConfigPath))
                    File.Delete(StaffConfigPath);
                Writer.Write(JsonConvert.SerializeObject(StaffConfig), StaffConfigPath);
                Reload();
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }

        public static void AddModerator(ulong id)
        {
            if (!StaffConfig.StaffIds.Contains(id))
            {
                StaffConfig.StaffIds.Add(id);
                SaveStaffConfig();
            }
        }

        public static void RemoveModerator(ulong id)
        {
            if (StaffConfig.StaffIds.Contains(id))
            {
                StaffConfig.StaffIds.Remove(id);
                SaveStaffConfig();
            }
        }

        public static void AddMaster(ulong id)
        {
            if (!StaffConfig.BotMasters.Contains(id))
            {
                StaffConfig.BotMasters.Add(id);
                SaveStaffConfig();
            }
        }

        public static void RemoveMaster(ulong id)
        {
            if (StaffConfig.BotMasters.Contains(id))
            {
                StaffConfig.BotMasters.Remove(id);
                SaveStaffConfig();
            }
        }
    }

    public class BotConfig
    {
        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("prefix")]
        public string Prefix { get; set; }

        [JsonProperty("debug")]
        public bool Debug { get; set; }

        [JsonProperty("modLogChannel")]
        public ulong ModLogChannel { get; set; }

        [JsonProperty("allowLavalink")]
        public bool AllowLavaLink { get; set; }

        [JsonProperty("lavalinkPassword")]
        public string LavalinkPassword { get; set; }

        [JsonProperty("deleteAfterUsage")]
        public List<string> CommandsToDelete { get; set; }

        [JsonProperty("verifiedRoleId")]
        public ulong VerifiedRoleId { get; set; }

        [JsonProperty("unverifiedRoleId")]
        public ulong UnverifiedRoleId { get; set; }

        [JsonProperty("serverLogChannel")]
        public ulong ServerLogChannel { get; set; }

        [JsonProperty("factOfTheDay")]
        public ulong FactChannel { get; set; }

        [JsonProperty("autoMeme")]
        public ulong AutoMemeChannel { get; set; }

        [JsonProperty("giveawayChannel")]
        public ulong GiveawayChannel { get; set; }

        [JsonProperty("autoMemeSubreddit")]
        public string Subreddit { get; set; }

        [JsonProperty("infoEmbed")]
        public InfoEmbed InfoEmbed { get; set; }

        [JsonProperty("radioStations")]
        public Dictionary<string, string> RadioStations { get; set; }

        [JsonProperty("reactionRoles")]
        public Dictionary<ulong, Dictionary<string, ulong>> ReactionRoles { get; set; }

        [JsonProperty("rolePersists")]
        public Dictionary<ulong, List<ulong>> RolePersists { get; set; }

        [JsonProperty("selfRoles")]
        public List<ulong> SelfRoles { get; set; }

        [JsonProperty("ignoredIDsCommands")]
        public List<ulong> IgnoredIds { get; set; }
    }

    public class StaffConfig
    {
        [JsonProperty("moderators")]
        public List<ulong> StaffIds { get; set; } = new List<ulong>();

        [JsonProperty("masters")]
        public List<ulong> BotMasters { get; set; } = new List<ulong>();
    }
}
