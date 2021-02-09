using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using System.Net;
using System.Text.RegularExpressions;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using HSNXT.DSharpPlus.Extended.Emoji;
using System.Threading;
using DSharpPlus.VoiceNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.EventHandling;
using DSharpPlus.Interactivity.Extensions;
using Newtonsoft.Json.Serialization;
using UrbanDictionary;
using UrbanDictionary.Core;
using UrbanDictionary.Core.Interfaces;
using UrbanDictionary.Core.Utilities;

namespace Multiverse.API
{
    public struct InfoEmbed
    {
        [JsonProperty("serverId")]
        public ulong ServerId { get; set; }

        [JsonProperty("channelId")]
        public ulong ChannelId { get; set; }

        [JsonProperty("messageId")]
        public ulong? MessageId { get; set; }
    }

    public static class ReactionRoles
    {
        public static void Add(DiscordMessage message, string emoji, DiscordRole role)
        {
            if (!Configs.BotConfig.ReactionRoles.ContainsKey(message.Id))
            {
                Configs.BotConfig.ReactionRoles.Add(message.Id, new Dictionary<string, ulong>
                {
                    { emoji, role.Id }
                });
            }
            else
            {
                if (!Configs.BotConfig.ReactionRoles[message.Id].ContainsKey(emoji))
                {
                    Configs.BotConfig.ReactionRoles[message.Id].Add(emoji, role.Id);
                }
                else
                {
                    Configs.BotConfig.ReactionRoles[message.Id][emoji] = role.Id;
                }
            }

            Configs.SaveBotConfig();
        }

        public static async Task Check(DiscordMessage message, DiscordUser usr, DiscordEmoji emoji, bool added)
        {
            DiscordMember user = usr.Id.ToString().GetUser();
            if (user == null) return;
            if (Configs.BotConfig.ReactionRoles == null) return;
            if (Configs.BotConfig.ReactionRoles.Count < 1) return;
            if (Configs.BotConfig.ReactionRoles.TryGetValue(message.Id, out Dictionary<string, ulong> reactions))
            {
                string emote = emoji.GetDiscordName().Replace(":", "");
                if (reactions.TryGetValue(emote, out ulong roleId))
                {
                    DiscordRole role = roleId.GetRole();
                    if (role != null)
                    {
                        if (added)
                            await user.GrantRoleAsync(role, "Reaction Roles");
                        else
                            await user.RevokeRoleAsync(role, "Reaction Roles");
                    }
                }
            }
        }
    }

    public static class Radio
    {
        public static StreamReceiver Streamer;
        public static bool sendingdatas = false;
        public static bool allowmodify = true;
        public static bool playing = false;
        public static bool Cancel = false;

        public static async Task Play(string url, DiscordChannel channel)
        {
            try
            {
                if (playing)
                {
                    Cancel = true;
                }

                Cancel = false;
                Streamer = new StreamReceiver(url);
                Helper.VoiceNextClient = Bot.Client.GetVoiceNext();
                Helper.VoiceNextConnection = await channel.ConnectAsync();
                await Helper.VoiceNextConnection.TargetChannel.Guild.CurrentMember.SetDeafAsync(true);
                Helper.Sink = Helper.VoiceNextConnection.GetTransmitSink();
                SpinWait.SpinUntil(() => allowmodify);
                await Helper.VoiceNextConnection.SendSpeakingAsync(true);
                await Helper.SetRadioStatus(Configs.BotConfig.RadioStations.Where(h => h.Value == url).FirstOrDefault().Key, url);
                if (!sendingdatas) await Send();
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }

        public static async Task Send()
        {
            try
            {
                sendingdatas = true;
                playing = true;
                Streamer.Start();

                while (Cancel == false)
                {
                    Logger.Debug($"[NEXT] Streaming ..");
                    await Streamer.FFMPEG.StandardOutput.BaseStream.CopyToAsync(Helper.Sink);
                }

                Logger.Debug($"[NEXT] Stopping stream ..");
                Streamer.Stop();
                await Helper.VoiceNextConnection.WaitForPlaybackFinishAsync();
                Helper.Sink.Pause();
                await Helper.Sink.FlushAsync();
                Helper.Sink.Dispose();
                Helper.VoiceNextConnection.Pause();
                Helper.VoiceNextConnection.Disconnect();
                sendingdatas = false;
                playing = false;
                Logger.Debug($"[NEXT] Disconnected.");
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }
    }

    public class StreamReceiver : IDisposable
    {
        Process ffmpeg;
        ProcessStartInfo info;
        string radio;
        bool active;

        public Stream AudioStream { get { return ffmpeg.StandardOutput.BaseStream; } }
        public Process FFMPEG { get { return ffmpeg; } }
        public string RadioURL { get { return radio; } }
        public bool Active { get { return active; } }


        public StreamReceiver(string radio_url)
        {
            radio = radio_url;
            info = new ProcessStartInfo()
            {
                FileName = Downloader.FFMPEG,
                Arguments = "-i \"" + radio + "\" -ac 2 -f s16le -ar 48000 pipe:1", 
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            active = false;
        }

        public void Start()
        {
            if (!active) ffmpeg = Process.Start(info);
            active = true;
        }

        public void Stop()
        {
            if (active) DeleteStreamer();
            active = false;
            Radio.playing = false;
        }

        void DeleteStreamer()
        {
            if (ffmpeg != null)
            {
                ffmpeg.Kill(true);
                ffmpeg.Dispose();
                ffmpeg = null;
            }
        }

        public void Dispose()
        {
            DeleteStreamer();
        }
    }

    public static class Paths
    {
        public static string FFMpeg => $"{BotPath}/ffmpeg{(Environment.OSVersion.Platform == PlatformID.Win32NT ? ".exe" : "")}";
        public static string BotPath => Directory.GetCurrentDirectory();
        public static string StaffConfigFile => $"{BotPath}/StaffConfig.json";
        public static string ModThingsPath => $"{BotPath}/Moderation";
        public static string ConfigFile => $"{BotPath}/BotConfig.json";

        public static void CheckAll()
        {
            if (!Directory.Exists(ModThingsPath))
                Directory.CreateDirectory(ModThingsPath);
            LevelManager.CheckDir();
        }

        public static string[] ReadAllLines(string path)
        {
            try
            {
                List<string> list = new List<string>();
                using (StreamReader streamReader = new StreamReader(path))
                {
                    string item;
                    while ((item = streamReader.ReadLine()) != null)
                    {
                        list.Add(item);
                    }
                }
                string[] result = list.ToArray();
                return result;
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return null;
            }
        }
    }

    public static class Suggestions
    {
        public class SuggestionProperties
        {
            public ulong Id = 0;
            public ulong AuthorId = 0;
            public string Suggestion = "";
            public DiscordMessage Message = null;
            
            public async Task Modify(string newSuggestion)
            {
                try
                {
                    Suggestion = newSuggestion;
                    if (Message == null) return;
                    await Message.ModifyAsync(null, Create(AuthorId.ToString().GetUser(), Suggestion, Id));
                }
                catch (Exception e)
                {
                    Logger.Exception(e);
                }
            }

            public static DiscordEmbed Create(DiscordMember usr, string suggestion, ulong id) => ClassBuilder.BuildEmbed("", "", "", "", "", "", usr.GetAvatar(), "", new List<Field>() { new Field(true, "Submitter", usr.Mention), new Field(false, "Suggestion", suggestion) }, $"ID: {id}", "", Helper.GetRandomColor(), false);
        }

        public static string SFile = $"{Paths.ModThingsPath}/Suggestions.json";
        public static Dictionary<ulong, ulong> Channels = new Dictionary<ulong, ulong>();
        public static List<SuggestionProperties> Cache = new List<SuggestionProperties>();

        public static void Read()
        {
            if (!File.Exists(SFile))
            {
                File.Create(SFile).Close();
                Save();
            }

            Channels = JsonConvert.DeserializeObject<Dictionary<ulong, ulong>>(Reader.ReadAll(SFile));
        }

        public static void Save()
        {
            if (File.Exists(SFile))
                File.Delete(SFile);
            Writer.Write(JsonConvert.SerializeObject(Channels), SFile);
        }

        public static void Add(ulong source, ulong channel)
        {
            if (!Channels.ContainsKey(source))
                Channels.Add(source, channel);
            else
                Channels[source] = channel;
            Save();
        }

        public static void Remove(ulong source, ulong channel)
        {
            if (Channels.ContainsKey(source))
                Channels.Remove(source);
            Save();
        }

        public static ulong? Get(ulong ch)
        {
            if (!Channels.ContainsKey(ch))
                return null;
            return Channels[ch];
        }

        public static bool Suggest(ulong source, DiscordMember usr, string suggestion, out string error, out string chann)
        {
            try
            {
                ulong? get = Get(source);
                if (get == null)
                {
                    error = ":x: There are no channels connected to this channel.";
                    chann = null;
                    return false;
                }

                DiscordChannel channel = get.Value.ToString().GetTextChannel();
                if (channel == null)
                {
                    error = ":x: There are no channels connected to this channel.";
                    chann = null;
                    return false;
                }

                if (string.IsNullOrEmpty(suggestion))
                {
                    error = ":x: Your suggestion can't be empty, dummy.";
                    chann = null;
                    return false;
                }
                SuggestionProperties props = new SuggestionProperties
                {
                    AuthorId = usr.Id,
                    Id = Cache.Count < 1 ? 1 : Cache.LastOrDefault().Id + 1,
                    Message = null,
                    Suggestion = suggestion
                };
                DiscordMessage msg = channel.SendMessageAsync(SuggestionProperties.Create(usr, suggestion, props.Id)).GetAwaiter().GetResult();
                DiscordEmoji cross = DiscordEmoji.FromUnicode(Emoji.Cross);
                DiscordEmoji check = DiscordEmoji.FromUnicode(Emoji.WhiteCheckMark);
                msg.CreateReactionAsync(cross);
                msg.CreateReactionAsync(check);
                props.Message = msg;
                error = "";
                chann = channel.Mention;
                Cache.Add(props);
                return true;
            }
            catch (Exception e) 
            {
                error = $":x: **An error occured!\nException of type {e.GetType().Name} was thrown.**";
                chann = null;
                return false;
            }
        }

        public static bool TryGet(ulong id, out SuggestionProperties props)
        {
            foreach (SuggestionProperties p in Cache)
            {
                if (p.Id == id)
                {
                    props = p;
                    return true;
                }
            }

            props = null;
            return false;
        }
    }

    public static class GiveawayManager
    {
        public static DiscordEmoji Tada = DiscordEmoji.FromUnicode("\uD83C\uDF89");
        public static string GFile = $"{Paths.ModThingsPath}/Giveaways.json";
        public static List<Giveaway> Giveaways = new List<Giveaway>();

        public class Giveaway
        {
            [JsonProperty("endTime")]
            public long EndTime { get; set; }

            [JsonProperty("id")]
            public ulong Id { get; set; }

            [JsonProperty("host")]
            public ulong Host { get; set; }

            [JsonProperty("item")]
            public string Item { get; set; }

            [JsonProperty("messageId")]
            public ulong Message { get; set; }

            [JsonProperty("ended")]
            public bool Ended { get; set; }

            public DiscordUser ChooseWinner()
            {
                DiscordMessage message = GetMessage();
                if (message == null) return null;
                IEnumerable<DiscordUser> users = message.GetReactionsAsync(Tada, message.Reactions.Where(h => h.Emoji.Name == Tada.Name).Count()).GetAwaiter().GetResult();
                users = users.Where(h => !h.IsBot);
                if (users.Count() < 1)
                    return null;
                DiscordUser selected = users.ToList()[new Random().Next(users.Count())];
                return selected;
            }

            public DiscordMessage GetMessage() => Configs.BotConfig.GiveawayChannel.ToString().GetTextChannel()?.GetMessageAsync(Message)?.GetAwaiter().GetResult();

            public void End()
            {
                DiscordUser winner = ChooseWinner();
                DiscordMessage message = GetMessage();
                if (winner == null) return;
                message.ModifyAsync(null, EndedEmbed(winner));

                Configs.BotConfig.GiveawayChannel.ToString().GetTextChannel().SendMessageAsync($":tada: Congratulations {winner.Mention}! You won the **{Item}**!");
                Ended = true;
            }

            public void Update()
            {
                DateTime date = new DateTime(EndTime);
                if (date.IsClose(DateTime.Now) && !Ended)
                    End();
            }

            public void ReRoll() => End();

            public DiscordEmbed Embed()
            {
                DiscordMember author = Host.ToString().GetUser();
                long ticks = EndTime - DateTime.Now.Ticks;
                DateTime endsAt = DateTime.Now.AddTicks(ticks);
                return ClassBuilder.BuildEmbed($"{author.Username}#{author.Discriminator}", "", author.GetAvatar(), Item, $"**React with :tada: to enter!**", "", "", "", new List<Field>() { new Field("Hosted by", author.Mention), new Field("Ends at", endsAt.ToString("F")) }, $"GiveawayID: {Id}", "", Helper.GetRandomColor(), false);
            }

            public DiscordEmbed EndedEmbed(DiscordUser winner)
            {
                DiscordUser author = Host.ToString().GetUser();
                return ClassBuilder.BuildEmbed($"{author.Username}#{author.Discriminator}", "", author.GetAvatar(), Item, "", "", "", "", new List<Field>() { new Field("Hosted by", author.Mention), new Field("Ends at", "Ended!"), new Field("Winner", winner.Mention)}, "", "", Helper.GetRandomColor(), false);
            }
        }

        public static string CreateGiveaway(string mention, string time, string item, DiscordMember host)
        {
            bool mentionSet = false;
            bool mentionForce = false;

            if (string.IsNullOrEmpty(mention) || mention.ToLower() == "null" || mention.ToLower() == "none")
            {
                mentionSet = false;
                mentionForce = true;
                mention = "";
            }
            
            if (mention.ToLower() == "here")
            {
                mention = "@here";
                mentionSet = true;
            }

            if (mention.ToLower() == "everyone")
            {
                mention = host.Guild.EveryoneRole.Mention;
                mentionSet = true;
            }

            if (!mentionSet)
            {
                DiscordRole role = mention.GetRole();
                if (role != null)
                {
                    mention = role.Mention;
                    mentionSet = true;
                }
            }
            
            DiscordChannel channel = Configs.BotConfig.GiveawayChannel.ToString().GetTextChannel();
            if (channel == null) return ":x: You have to set a giveaway channel before starting a giveaway.";
            ulong duration = Timer.ParseDuration(time);
            if (!Timer.TryParse(time, out Timer.Time unit)) return ":x: Select a valid time unit.";
            string str = Timer.ParseString(duration, unit);
            TimeSpan span = Timer.ParseTime(duration, unit);
            DateTime date = Timer.ParseDate(span);
            long ticks = (date - DateTime.Now).Ticks;
            DateTime end = DateTime.Now.AddTicks(ticks);
            string endDate = end.ToString("F");
            Giveaway giveaway = new Giveaway();
            giveaway.Item = item;
            giveaway.Host = host.Id;
            giveaway.EndTime = end.Ticks;
            giveaway.Id = Giveaways.Count < 1 ? 0 : Giveaways.LastOrDefault().Id + 1;
            if (mentionSet && !mentionForce && !string.IsNullOrEmpty(mention)) channel.SendMessageAsync(mention);
            DiscordMessage msg = channel.SendMessageAsync(giveaway.Embed()).GetAwaiter().GetResult();
            giveaway.Message = msg.Id;
            msg.CreateReactionAsync(Tada);
            Giveaways.Add(giveaway);
            Save();
            return $":white_check_mark: Nice! This giveaway of **{item}** will last **{duration} {str}** [**{endDate}**]!";
        }

        public static Giveaway GetGiveaway(ulong id)
        {
            foreach (Giveaway gw in Giveaways)
            {
                if (gw.Id == id)
                    return gw;
            }
            return null;
        }

        public static void Save()
        {
            if (File.Exists(GFile))
                File.Delete(GFile);
            Writer.Write(JsonConvert.SerializeObject(Giveaways), GFile);
        }

        public static void Read()
        {
            if (!File.Exists(GFile))
                Save();
            Giveaways = JsonConvert.DeserializeObject<List<Giveaway>>(Reader.ReadAll(GFile));
        }

        public static void Update() => Giveaways.ForEach((e) => e.Update());
    }

    public static class AutoModerator
    {
        public static string Path = $"{Paths.ModThingsPath}/AutoMod.json";

        public class Configuration
        {
            [JsonProperty("caps")]
            public int MaxCaps { get; set; }

            [JsonProperty("mentions")]
            public int Mentions { get; set; }

            [JsonProperty("filters")]
            public Dictionary<Filter, bool> Filters { get; set; }

            [JsonProperty("actions")]
            public Dictionary<Filter, Action> Actions { get; set; }

            [JsonProperty("sentences")]
            public Dictionary<Filter, string> Sentences { get; set; }

            [JsonProperty("length")]
            public Dictionary<Filter, string> Length { get; set; }

            [JsonProperty("ignoredIds")]
            public List<ulong> IgnoredIDs { get; set; }

            [JsonProperty("enabled")]
            public bool Enabled { get; set; }
        }

        public static Dictionary<Filter, bool> EnabledFilters = new Dictionary<Filter, bool>
        {
            { Filter.Caps,          false },
            { Filter.ExternalLinks, false },
            { Filter.Mentions,      false },
            { Filter.ServerInvites, false },
        };

        public static Dictionary<Filter, Action> Actions = new Dictionary<Filter, Action>
        {
            { Filter.Caps,          Action.None },
            { Filter.ExternalLinks, Action.None },
            { Filter.Mentions,      Action.None },
            { Filter.ServerInvites, Action.None },
        };

        public static Dictionary<Filter, string> Sentences = new Dictionary<Filter, string>
        {
            { Filter.Caps,          "Too many caps."          },
            { Filter.ExternalLinks, "You can't send links."   },
            { Filter.Mentions,      "Too many mentions."      },
            { Filter.ServerInvites, "You can't send invites." },
        };

        public static Dictionary<Filter, string> Lengths = new Dictionary<Filter, string>
        {
            { Filter.Caps,          "0s" },
            { Filter.ExternalLinks, "0s" },
            { Filter.Mentions,      "0s" },
            { Filter.ServerInvites, "0s" },
        };

        public static Configuration Config = null;
        public static Regex Regex = new Regex("[^A-Z]");

        public static void SetFilter(Filter filter, bool state)
        {
            Config.Filters[filter] = state;
            Save();
        }

        public static void SetAction(Filter filter, Action act)
        {
            Config.Actions[filter] = act;
            Save();
        }

        public static void SetSentence(Filter filter, string sentence)
        {
            Config.Sentences[filter] = sentence;
            Save();
        }

        public static void SetLength(Filter filter, string l)
        {
            Config.Length[filter] = l;
            Save();
        }

        public static void TakeAction(DiscordGuild g, DiscordMessage msg, Filter filter)
        {
            if (!Config.Filters[filter])
                return;
            Action act = Config.Actions[filter];
            if (act == Action.None)
                return;
            if (act == Action.Delete)
                msg.DeleteAsync();
            DiscordMember author = msg.Author.Id.ToString().GetUser();
            if (act == Action.Warn)
            {
                Field reason = new Field("Reason", Config.Sentences[filter]);            
                author.SendMessageAsync(ClassBuilder.BuildEmbed($"{msg.Author.Username}#{msg.Author.Discriminator}", "", msg.Author.GetAvatar(), "", $":warning: You were warned in **{g.Name}**!", "", "", "", new List<Field>() { reason }, "", "", Helper.GetRandomColor(), false));
            }

            if (act == Action.WarnAndDelete)
            {
                Field reason = new Field("Reason", Config.Sentences[filter]);
                author.SendMessageAsync(ClassBuilder.BuildEmbed($"{msg.Author.Username}#{msg.Author.Discriminator}", "", msg.Author.GetAvatar(), "", $":warning: You were warned in **{g.Name}**!", "", "", "", new List<Field>() { reason }, "", "", Helper.GetRandomColor(), false));
                msg.DeleteAsync();
            }

            if (act == Action.Mute)
            {
                Field reason = new Field("Reason", Config.Sentences[filter]);
                bool parsed = Timer.TryParse(Config.Length[filter], out Timer.Time timeUnit);
                ulong dur = Timer.ParseDuration(Config.Length[filter]);
                string str = parsed ? Timer.ParseString(dur, timeUnit) : "";
                author.SendMessageAsync(ClassBuilder.BuildEmbed($"{msg.Author.Username}#{msg.Author.Discriminator}", "", msg.Author.GetAvatar(), "", $":warning: You were muted in **{g.Name}** for {dur} {str}!", "", "", "", new List<Field>() { reason }, "", "", Helper.GetRandomColor(), false));
                MuteHandler.Mute(null, g, msg.Author.Id.ToString().GetUser(), null, dur, "Automod", timeUnit, false, false);
            }
            
            if (act == Action.MuteAndDelete)
            {
                Field reason = new Field("Reason", Config.Sentences[filter]);
                bool parsed = Timer.TryParse(Config.Length[filter], out Timer.Time timeUnit);
                ulong dur = Timer.ParseDuration(Config.Length[filter]);
                string str = parsed ? Timer.ParseString(dur, timeUnit) : "";
                author.SendMessageAsync(ClassBuilder.BuildEmbed($"{msg.Author.Username}#{msg.Author.Discriminator}", "", msg.Author.GetAvatar(), "", $":warning: You were muted in **{g.Name}** for {dur} {str}!", "", "", "", new List<Field>() { reason }, "", "", Helper.GetRandomColor(), false));
                MuteHandler.Mute(null, g, msg.Author.Id.ToString().GetUser(), null, dur, "Automod", timeUnit, false, false);
                msg.DeleteAsync();
            }

            if (act == Action.Kick)
            {
                Field reason = new Field("Reason", Config.Sentences[filter]);
                author.SendMessageAsync(ClassBuilder.BuildEmbed($"{msg.Author.Username}#{msg.Author.Discriminator}", "", msg.Author.GetAvatar(), "", $":warning: You were kicked from **{g.Name}**!", "", "", "", new List<Field>() { reason }, "", "", Helper.GetRandomColor(), false));
                author.RemoveAsync($"Kicked by AutoMod for {filter}");
            }

            if (act == Action.KickAndDelete)
            {
                Field reason = new Field("Reason", Config.Sentences[filter]);
                author.SendMessageAsync(ClassBuilder.BuildEmbed($"{msg.Author.Username}#{msg.Author.Discriminator}", "", msg.Author.GetAvatar(), "", $":warning: You were kicked from **{g.Name}**!", "", "", "", new List<Field>() { reason }, "", "", Helper.GetRandomColor(), false));
                author.RemoveAsync($"Kicked by AutoMod for {filter}");
                msg.DeleteAsync();
            }

            if (act == Action.Ban)
            {
                Field reason = new Field("Reason", Config.Sentences[filter]);
                author.SendMessageAsync(ClassBuilder.BuildEmbed($"{msg.Author.Username}#{msg.Author.Discriminator}", "", msg.Author.GetAvatar(), "", $":warning: You were banned in **{g.Name}**!", "", "", "", new List<Field>() { reason }, "", "", Helper.GetRandomColor(), false));
                Timer.TryParse(Config.Length[filter], out Timer.Time timeUnit);
                BanHandler.Ban(null, g, msg.Author.Id.ToString().GetUser(), null, Timer.ParseDuration(Config.Length[filter]), reason.Value, timeUnit, false, false);
            }

            if (act == Action.BanAndDelete)
            {
                Field reason = new Field("Reason", Config.Sentences[filter]);
                author.SendMessageAsync(ClassBuilder.BuildEmbed($"{msg.Author.Username}#{msg.Author.Discriminator}", "", msg.Author.GetAvatar(), "", $":warning: You were banned in **{g.Name}**!", "", "", "", new List<Field>() { reason }, "", "", Helper.GetRandomColor(), false));
                Timer.TryParse(Config.Length[filter], out Timer.Time timeUnit);
                BanHandler.Ban(null, g, msg.Author.Id.ToString().GetUser(), null, Timer.ParseDuration(Config.Length[filter]), reason.Value, timeUnit, false, false);
                msg?.DeleteAsync();
            }
        }

        public static bool CheckFor(DiscordMessage msg, DiscordGuild g, Filter fil)
        {
            if (!Config.Enabled)
                return false;
            if (!Config.Filters[fil])
                return false;
            Action act = Config.Actions[fil];
            if (act == Action.None)
                return false;
            if (fil == Filter.Caps)
            {
                if (Config.MaxCaps != -1 && msg.Content.GetCaps() >= Config.MaxCaps)
                {
                    TakeAction(g, msg, fil);
                    return true;
                }
            }

            if (fil == Filter.Mentions)
            {
                if (Config.Mentions != -1 && (msg.MentionedRoles.Count + msg.MentionedUsers.Count) >= Config.Mentions)
                {
                    TakeAction(g, msg, fil);
                    return true;
                }
            }

            if (fil == Filter.ServerInvites)
            {
                if (msg.Content.ContainsInvite())
                {
                    TakeAction(g, msg, fil);
                    return true;
                }
            }

            if (fil == Filter.ExternalLinks)
            {
                if (msg.Content.ContainsLink())
                {
                    TakeAction(g, msg, fil);
                    return true;
                }
            }

            return false;
        }

        public static bool ShouldIgnore(DiscordMember usr)
        {
            try
            {
                if (!Config.Enabled)
                    return true;
                bool? nullable = Config?.IgnoredIDs?.Contains(usr.Id);
                if (nullable.HasValue && nullable.Value)
                    return true;
                foreach (DiscordRole role in usr.Roles)
                {
                    if (Config.IgnoredIDs.Contains(role.Id))
                        return true;
                }

                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }

        public static bool CheckForAll(DiscordGuild g, DiscordMessage msg)
        {
            if (!Config.Enabled)
                return false;
            if (CheckFor(msg, g, Filter.Caps))
                return true;
            if (CheckFor(msg, g, Filter.ExternalLinks))
                return true;
            if (CheckFor(msg, g, Filter.Mentions))
                return true;
            if (CheckFor(msg, g, Filter.ServerInvites))
                return true;
            return false;
        }

        public static void Save()
        {
            if (File.Exists(Path))
                File.Delete(Path);
            Writer.Write(JsonConvert.SerializeObject(Config), Path);
        }

        public static void Read()
        {
            CheckDefault();
            Config = JsonConvert.DeserializeObject<Configuration>(Reader.ReadAll(Path));
            Debug();
        }

        public static void CheckDefault()
        {
            if (!File.Exists(Path))
            {
                Config = new Configuration();
                Config.Actions = Actions;
                Config.Filters = EnabledFilters;
                Config.MaxCaps = -1;
                Config.Mentions = -1;
                Config.Sentences = Sentences;
                Config.Length = Lengths;
                Config.IgnoredIDs = new List<ulong>();
                Config.Enabled = false;
                Save();
            }
        }

        public static void Debug()
        {
            if (Config == null)
            {
                Config = new Configuration();
                Config.Actions = Actions;
                Config.Filters = EnabledFilters;
                Config.MaxCaps = -1;
                Config.Mentions = -1;
                Config.Sentences = Sentences;
                Config.Length = Lengths;
                Config.IgnoredIDs = new List<ulong>();
                Config.Enabled = false;
                Save();
            }
        }

        public static Filter GetFilter(string str)
        {
            if (int.TryParse(str, out int id))
            {
                foreach (Filter filter in Enum.GetValues(typeof(Filter)).Cast<Filter>())
                {
                    if (id == (int)filter)
                        return filter;
                    if (filter.ToString().ToLower() == str.ToLower())
                        return filter;
                }
            }
            else
            {
                foreach (Filter filter in Enum.GetValues(typeof(Filter)).Cast<Filter>())
                {
                    if (filter.ToString().ToLower() == str.ToLower())
                        return filter;
                }
            }

            return Filter.Undefined;
        }

        public static Action GetAction(string str)
        {
            if (int.TryParse(str, out int id))
            {
                foreach (Action act in Enum.GetValues(typeof(Action)).Cast<Action>())
                {
                    if (id == (int)act)
                        return act;
                    if (act.ToString().ToLower() == str.ToLower())
                        return act;
                }
            }
            else
            {
                foreach (Action act in Enum.GetValues(typeof(Action)).Cast<Action>())
                {
                    if (act.ToString().ToLower() == str.ToLower())
                        return act;
                }
            }

            return Action.None;
        }

        public enum Action
        {
            None,
            Delete,
            Warn,
            WarnAndDelete,
            Mute,
            MuteAndDelete,
            Kick,
            KickAndDelete,
            Ban,
            BanAndDelete
        }

        public enum Filter
        {
            ServerInvites,
            ExternalLinks,
            Caps,
            Mentions,
            Undefined
        }
    }

    public static class Shop
    {
        public static string Dict = $"{Paths.BotPath}/Shop";
        public static string ItemsFile = $"{Dict}/Items.json";
        public static string MoneyFile = $"{Dict}/Money.json";
        public static string RoleIncomesFile = $"{Dict}/RoleIncomes.json";
        public static ulong PreviousId = 0;

        public static List<ShopItem> Items { get; set; } = new List<ShopItem>();
        public static List<Money> Money { get; set; } = new List<Money>();
        public static List<RoleIncome> RoleIncomes { get; set; } = new List<RoleIncome>();

        public static void CreateItem(string name, ulong price, string desc)
        {
            ShopItem item = new ShopItem();
            PreviousId++;
            item.Id = PreviousId;
            item.Name = name;
            item.Price = price;
            item.Description = desc;
            Items.Add(item);
            Save();
        }

        public static void CreateMoney(DiscordMember usr)
        {
            if (Money.Select(h => h.UserId).Contains(usr.Id))
                return;
            Money m = new Money();
            m.UserId = usr.Id;
            m.Amount = 0;
            Money.Add(m);
            Save();
        }

        public static void AddMoney(DiscordMember usr, ulong amount)
        {
            int index = IndexOf(usr);
            if (index == -1 || index < Money.Count || index < 0 || index > Money.Count)
                return;
            Money[index].Amount += amount;
            Save();
        }

        public static void RemoveMoney(DiscordMember usr, ulong amount)
        {
            int index = IndexOf(usr);
            if (index == -1 || index < Money.Count || index < 0 || index > Money.Count)
                return;
            Money[index].Amount -= amount;
            Save();
        }

        public static void SetMoney(DiscordMember usr, ulong amount)
        {
            int index = IndexOf(usr);
            if (index == -1 || index < Money.Count || index < 0 || index > Money.Count)
                return;
            Money[index].Amount = amount;
            Save();
        }

        public static int IndexOf(DiscordMember usr)
        {
            bool found = false;
            Money mm = null;
            for (int i = 0; i < Money.Count; i++)
            {
                if (Money[i] != null)
                {
                    if (Money[i].UserId == usr.Id)
                        return i;
                }
            }

            if (!found)
            {
                Money money = new Money();
                money.UserId = usr.Id;
                Money.Add(money);
                return Money.IndexOf(money);
            }
            else
            {
                if (mm != null)
                    return Money.IndexOf(mm);
                else
                    return -1;
            }
        }

        public static void Save()
        {
            try
            {
                if (!Directory.Exists(Dict))
                    Directory.CreateDirectory(Dict);
                if (File.Exists(ItemsFile))
                    File.Delete(ItemsFile);
                if (File.Exists(MoneyFile))
                    File.Delete(MoneyFile);
                if (File.Exists(RoleIncomesFile))
                    File.Delete(RoleIncomesFile);
                Writer.Write(JsonConvert.SerializeObject(Items), ItemsFile);
                Writer.Write(JsonConvert.SerializeObject(Money), MoneyFile);
                Writer.Write(JsonConvert.SerializeObject(RoleIncomes), RoleIncomesFile);
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }
    }

    public class ShopItem
    {
        [JsonProperty("id")]
        public ulong Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("price")]
        public ulong Price { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    public class Money
    {
        [JsonProperty("user")]
        public ulong UserId { get; set; }

        [JsonProperty("money")]
        public ulong Amount { get; set; }
    }

    public class RoleIncome
    {
        [JsonProperty("amount")]
        public ulong Amount { get; set; }

        [JsonProperty("interval")]
        public ulong Interval { get; set; }

        [JsonProperty("role")]
        public ulong Role { get; set; }
    }

    public static class LevelManager
    {
        public static string LevelDict = $"{Paths.BotPath}/LevelManager";
        public static string LevelExpsFile = $"{LevelDict}/LevelExps.json";
        public static string LevelsFile = $"{LevelDict}/Levels.json";
        public static string ExpFile = $"{LevelDict}/Experience.json";
        public static string RolesFile = $"{LevelDict}/Roles.json";
        public static string IgnoredIDsFile = $"{LevelDict}/Ignored.json";
        public static string SettingsFile = $"{LevelDict}/Settings.json";

        public static Dictionary<int, ulong> LevelExps = new Dictionary<int, ulong>();
        public static Dictionary<ulong, ulong> Experience = new Dictionary<ulong, ulong>();
        public static Dictionary<ulong, int> Levels = new Dictionary<ulong, int>();
        public static Dictionary<int, List<ulong>> LevelRoles = new Dictionary<int, List<ulong>>();
        public static List<ulong> IgnoredIDs = new List<ulong>();
        public static LevelSettings Settings = null;

        public static void Debug()
        {
            if (Settings == null)
            {
                Settings = new LevelSettings();
                Settings.Channel = 0;
                Settings.Enabled = false;
                Settings.Multiplier = 1;
                Save();
            }
        }

        public static void CheckDir()
        {
            if (!Directory.Exists(LevelDict))
                Directory.CreateDirectory(LevelDict);
        }

        public static void Read()
        {
            try
            {
                CheckDir();
                if (string.IsNullOrEmpty(Reader.ReadAll(LevelExpsFile)))
                {
                    LevelExps = new Dictionary<int, ulong>();
                    Save();
                }
                LevelExps = JsonConvert.DeserializeObject<Dictionary<int, ulong>>(Reader.ReadAll(LevelExpsFile));


                if (string.IsNullOrEmpty(Reader.ReadAll(LevelsFile)))
                {
                    Levels = new Dictionary<ulong, int>();
                    Save();
                }
                Levels = JsonConvert.DeserializeObject<Dictionary<ulong, int>>(Reader.ReadAll(LevelsFile));

                if (string.IsNullOrEmpty(Reader.ReadAll(ExpFile)))
                {
                    Experience = new Dictionary<ulong, ulong>();
                    Save();
                }
                Experience = JsonConvert.DeserializeObject<Dictionary<ulong, ulong>>(Reader.ReadAll(ExpFile));

                if (string.IsNullOrEmpty(Reader.ReadAll(RolesFile)))
                {
                    LevelRoles = new Dictionary<int, List<ulong>>();
                    Save();
                }
                LevelRoles = JsonConvert.DeserializeObject<Dictionary<int, List<ulong>>>(Reader.ReadAll(RolesFile));

                if (string.IsNullOrEmpty(Reader.ReadAll(IgnoredIDsFile)))
                {
                    IgnoredIDs = new List<ulong>();
                    Save();
                }
                IgnoredIDs = JsonConvert.DeserializeObject<List<ulong>>(Reader.ReadAll(IgnoredIDsFile));
                string str = Reader.ReadAll(SettingsFile);
                if (string.IsNullOrEmpty(str) || str == "null")
                {
                    Settings = new LevelSettings();
                    Settings.Channel = 0;
                    Settings.Enabled = false;
                    Settings.Multiplier = 1;
                    Save();
                }
                Settings = JsonConvert.DeserializeObject<LevelSettings>(Reader.ReadAll(SettingsFile));
                Save();
                Debug();
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }

        public static void Save()
        {
            try
            {
                try
                {
                    if (File.Exists(LevelsFile))
                        File.Delete(LevelsFile);
                    if (File.Exists(LevelExpsFile))
                        File.Delete(LevelExpsFile);
                    if (File.Exists(ExpFile))
                        File.Delete(ExpFile);
                    if (File.Exists(RolesFile))
                        File.Delete(RolesFile);
                    if (File.Exists(IgnoredIDsFile))
                        File.Delete(IgnoredIDsFile);
                    if (File.Exists(SettingsFile))
                        File.Delete(SettingsFile);
                    Writer.Write(JsonConvert.SerializeObject(Levels), LevelsFile);
                    Writer.Write(JsonConvert.SerializeObject(LevelExps), LevelExpsFile);
                    Writer.Write(JsonConvert.SerializeObject(Experience), ExpFile);
                    Writer.Write(JsonConvert.SerializeObject(LevelRoles), RolesFile);
                    Writer.Write(JsonConvert.SerializeObject(IgnoredIDs), IgnoredIDsFile);
                    Writer.Write(JsonConvert.SerializeObject(Settings), SettingsFile);
                }
                catch (Exception e)
                {
                    Logger.Exception(e);
                }
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }

        public static void SetLevels()
        {
            if (LevelExps.Count > 0)
                return;
            LevelExps.Clear();
            for (int i = 1; i < 1001; i++)
            {
                if (i == 1)
                    LevelExps.TryAdd(i, 200);
                else
                    LevelExps.TryAdd(i, LevelExps.Values.LastOrDefault() + 200);
            }
        }

        public static void ProcessLevel(DiscordMember usr, DiscordChannel chann, DiscordMessage msg)
        {
            if (!Settings.Enabled)
                return;
            if (IgnoredIDs.Contains(chann.Id) || IgnoredIDs.Contains(usr.Id))
                return;
            AddExp(usr, GetExpFromMessage(msg));
        }

        public static void AnnounceLevelUp(DiscordMember usr, int lvl)
        {
            if (!Settings.Enabled)
                return;
            ProcessRoles(usr);
            if (Settings.Channel < 1)
                return;
            DiscordChannel channel = Settings.Channel.ToString().GetTextChannel();
            if (channel == null)
                return;
            channel.SendMessageAsync($"**{usr.Username}#{usr.Discriminator}** has leveled up to level **{lvl}**!");
        }

        public static DiscordEmbed GetRankCommand(DiscordMember usr)
        {
            if (!Settings.Enabled)
                return null;
            Field level = new Field("Level", Levels[usr.Id].ToString());
            Field exp = new Field("Experience", $"{Experience[usr.Id]}/{LevelExps[Levels[usr.Id] + 1]}");
            Field pos = new Field("Position", Levels.Keys.OrderByDescending(h => Experience[h]).ToList().IndexOf(usr.Id) + 1);
            List<Field> fields = new List<Field>() { level, exp, pos };
            DiscordEmbed e = ClassBuilder.BuildEmbed($"{usr.Username}#{usr.Discriminator}", null, usr.AvatarUrl, "", "", "", "", "", fields, "", "", Helper.GetRandomColor(), false);
            return e;
        }

        public static DiscordEmbed GetLeaderboard(DiscordMember u)
        {
            if (!Settings.Enabled)
                return null;
            List<ulong> usersByLvl = Levels.Keys.OrderByDescending(h => Experience[h]).Take(10).ToList();
            List<Field> fields = new List<Field>();

            foreach (ulong usr in usersByLvl)
            {
                DiscordMember user = usr.ToString().GetUser();
                if (user == null)
                {
                    usersByLvl.RemoveAt(usersByLvl.IndexOf(usr));
                    continue;
                }

                Field name = new Field($"#{usersByLvl.IndexOf(user.Id) + 1} | {user.Username}#{user.Discriminator}", $"**Level**: {Levels[user.Id]}\n**Experience**: {Experience[user.Id]} / {LevelExps[Levels[user.Id]] + 200}");
                fields.Add(name);
            }

            return ClassBuilder.BuildEmbed($"{u.Username}#{u.Discriminator}", "", u.GetAvatar(), $"{u.Guild.Name}'s leaderboard", "", "", "", "", fields, "", "", Helper.GetRandomColor(), false);
        }

        public static void AddExp(DiscordMember usr, ulong exp)
        {
            if (!Settings.Enabled)
                return;
            if (!Levels.ContainsKey(usr.Id))
                Levels.Add(usr.Id, 0);
            if (!Experience.ContainsKey(usr.Id))
                Experience.Add(usr.Id, 0);
            ulong expToGain = exp;
            Experience[usr.Id] += expToGain;
            ulong curExp = Experience[usr.Id];
            int curLvl = Levels[usr.Id];
            ulong reqExp = LevelExps[curLvl + 1];
            if (curExp >= reqExp)
            {
                Levels[usr.Id]++;
                AnnounceLevelUp(usr, Levels[usr.Id]);
            }
        }

        public static void SetLvl(DiscordMember usr, int lvl)
        {
            if (!Settings.Enabled)
                return;
            Levels[usr.Id] = lvl;
            AnnounceLevelUp(usr, lvl);
        }

        public static void ProcessRoles(DiscordMember usr)
        {
            if (!Settings.Enabled)
                return;
            if (usr == null)
                return;
            int lvl = Levels[usr.Id];
            List<ulong> roles = new List<ulong>();
            if (!LevelRoles.TryGetValue(lvl, out roles))
                return;
            if (roles.Count > 0)
            {
                for (int i = 0; i < roles.Count; i++)
                {
                    DiscordRole role = roles[i].GetRole();
                    if (role != null)
                    {
                        usr.GrantRoleAsync(role, $"Added for reaching level {lvl}");
                    }
                }
            }
        }

        public static ulong GetExpFromMessage(DiscordMessage msg)
        {
            if (!Settings.Enabled)
                return 0;
            if (msg.Content.StartsWith(Configs.BotConfig.Prefix))
                return 0;
            ulong toEarn = (ulong)(3 * (Settings == null ? 0 : Settings.Multiplier));
            return toEarn;
        }
    }

    public class LevelSettings
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("multiplier")]
        public double Multiplier { get; set; }

        [JsonProperty("channel")]
        public ulong Channel { get; set; }
    }

    public class RandomFact
    {
        [JsonProperty("text")]
        public string Fact { get; set; }
    }

    public class RandomJoke
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("setup")]
        public string Setup { get; set; }

        [JsonProperty("punchline")]
        public string Punchline { get; set; }
    }

    public class YoMama
    {
        [JsonProperty("joke")]
        public string Joke { get; set; }
    }

    public class Urban
    {
        public string Term { get; set; }
        public string Definition { get; set; }
    }

    public class InsultPost
    {
        [JsonProperty("insult")]
        public string Insult { get; set; }
    }

    public class FriendsQuote
    {
        [JsonProperty("quote")]
        public string Quote { get; set; }


        [JsonProperty("character")]
        public string Character { get; set; }
    }

    public class RandomDogImage
    {
        [JsonProperty("message")]
        public string Url { get; set; }
    }

    public class RolledDice
    {
        [JsonProperty("dice")]
        public Newtonsoft.Json.Linq.JArray Value { get; set; }
    }

    public static class Client
    {

        public static UrbanDictionaryAPI Urban = new UrbanDictionaryAPI();

        public enum Rps
        {
            Rock,
            Paper,
            Scissors
        }

        public static Rps? Get(string arg)
        {
            foreach (Rps rps in Enum.GetValues(typeof(Rps)).Cast<Rps>())
            {
                if (int.TryParse(arg, out int index) && ((int)rps) == index) return rps;
                if (rps.ToString().ToLower() == arg.ToLower() || rps.ToString().ToLower().Contains(arg.ToLower())) return rps;
            }

            return null;
        }

        public static Rps Choose()
        {
            List<Rps> rps = Enum.GetValues(typeof(Rps)).Cast<Rps>().ToList();
            return rps[new Random().Next(rps.Count)];
        }

        public static async Task DoRps(DiscordChannel channel, Rps clientChoice)
        {
            DiscordMessage message = await channel.SendMessageAsync($":hourglass: Choosing ...");
            await Task.Delay(1000);
            Rps choice = Choose();
            bool? won = Decide(choice, clientChoice);
            if (won == null) await message.ModifyAsync($"You chose **{clientChoice}**\nI chose **{choice}**\nNobody won."); else { await message.ModifyAsync($"You chose **{clientChoice}**\nI chose **{choice}**.\n{(won.Value ? "I" : "You")} won{(won.Value ? " Better luck next time!" : "")}."); }
        }

        public static bool? Decide(Rps bot, Rps user)
        {
            if (user == Rps.Paper && bot == Rps.Rock) return false;
            if (user == Rps.Scissors && bot == Rps.Paper) return false;
            if (user == Rps.Rock && bot == Rps.Scissors) return false;
            if (bot == Rps.Paper && user == Rps.Rock) return true;
            if (bot == Rps.Scissors && user == Rps.Paper) return true;
            if (bot == Rps.Rock && user == Rps.Scissors) return true;
            return null;
        }

        public static RolledDice RequestDice()
        {
            try
            {
                HttpWebRequest req = WebRequest.CreateHttp("http://roll.diceapi.com/json/d6");
                req.Method = "GET";
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                string response = new StreamReader(res.GetResponseStream()).ReadToEnd();
                return JsonConvert.DeserializeObject<RolledDice>(response);
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return null;
            }
        }

        public static RandomDogImage RequestRandomDogImage()
        {
            try
            {
                HttpWebRequest req = WebRequest.CreateHttp("https://dog.ceo/api/breeds/image/random");
                req.Method = "GET";
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                string response = new StreamReader(res.GetResponseStream()).ReadToEnd();
                return JsonConvert.DeserializeObject<RandomDogImage>(response);
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return null;
            }
        }

        public static RandomJoke RequestJoke()
        {
            try
            {
                HttpWebRequest req = WebRequest.CreateHttp("https://official-joke-api.appspot.com/random_joke");
                req.Method = "GET";
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                string response = new StreamReader(res.GetResponseStream()).ReadToEnd();
                return JsonConvert.DeserializeObject<RandomJoke>(response);
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return null;
            }
        }

        public static RandomFact RequestFact()
        {
            try
            {
                HttpWebRequest req = WebRequest.CreateHttp("https://uselessfacts.jsph.pl/random.json?language=en");
                req.Method = "GET";
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                string response = new StreamReader(res.GetResponseStream()).ReadToEnd();
                return JsonConvert.DeserializeObject<RandomFact>(response);
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return null;
            }
        }

        public static MemePost RequestMeme(string subreddit)
        {
            try
            {
                HttpWebRequest req = WebRequest.CreateHttp($"https://meme-api.herokuapp.com/gimme/{subreddit}");
                req.Method = "GET";
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                string response = new StreamReader(res.GetResponseStream()).ReadToEnd();
                return JsonConvert.DeserializeObject<MemePost>(response);
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return null;
            }
        }

        public static Urban RequestUrban(string term)
        {
            try
            {
                Urban urban = new Urban();
                urban.Term = term;
                urban.Definition = Urban.DefineAsync(term).GetAwaiter().GetResult();
                return urban;
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return null;
            }
        }

        public static InsultPost RequestInsult()
        {
            try
            {
                HttpWebRequest req = WebRequest.CreateHttp($"https://evilinsult.com/generate_insult.php?lang=en&type=json");
                req.Method = "GET";
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                string response = new StreamReader(res.GetResponseStream()).ReadToEnd();
                return JsonConvert.DeserializeObject<InsultPost>(response);
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return null;
            }
        }

        public static YoMama RequestYoMama()
        {
            try
            {
                HttpWebRequest req = WebRequest.CreateHttp($"https://api.yomomma.info/");
                req.Method = "GET";
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                string response = new StreamReader(res.GetResponseStream()).ReadToEnd();
                return JsonConvert.DeserializeObject<YoMama>(response);
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return null;
            }
        }

        public static FriendsQuote RequestFriendsQuote()
        {
            try
            {
                HttpWebRequest req = WebRequest.CreateHttp($"https://friends-quotes-api.herokuapp.com/quotes/random");
                req.Method = "GET";
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                string response = new StreamReader(res.GetResponseStream()).ReadToEnd();
                return JsonConvert.DeserializeObject<FriendsQuote>(response);
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return null;
            }
        }
    }

    public class MemePost
    {
        [JsonProperty("postLink")]
        public string Link { get; set; }

        [JsonProperty("subreddit")]
        public string Subreddit { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("nsfw")]
        public bool Nsfw { get; set; }

        [JsonProperty("spoiler")]
        public bool Spoiler { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }
    }

    public class Logger : ILoggerFactory, ILogger
    {
        public static bool IsDebugEnabled { get; set; } = Configs.BotConfig.Debug;
        public static string Time => DateTime.Now.ToString("HH:mm:ss");
        public static string Timestamp => DateTime.Now.ToString("dd.MM.yy HH:mm");

        public static void Info(object msg) => Add("info", msg);
        public static void Warn(object msg) => Add("warn", msg);
        public static void Error(object msg) => Add("error", msg);
        public static void Exception(Exception e)
        {
            Add("exception", e.ToString());
        }

        public static void Debug(object msg)
        {
            if (!IsDebugEnabled)
                return;
            Add("debug", msg);
        }

        public static void Add(string type, object msg)
        {
            if (type == "info")
                Console.ForegroundColor = ConsoleColor.Yellow;
            if (type == "warn")
                Console.ForegroundColor = ConsoleColor.Green;
            if (type == "error")
                Console.ForegroundColor = ConsoleColor.Red;
            if (type == "exception")
                Console.ForegroundColor = ConsoleColor.DarkRed;
            if (type == "debug")
                Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"<{Time}> [{type.ToUpper()}]: {msg}");
            Console.ResetColor();
        }

        public ILogger CreateLogger(string categoryName) => new Logger();
        public void AddProvider(ILoggerProvider provider) { }
        public void Dispose() { }
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }
        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Debug ? IsDebugEnabled : true;
        public IDisposable BeginScope<TState>(TState state) => default;
    }

    public static class ClassBuilder
    {
        public static DiscordEmbed BuildEmbed(string author, string authorUrl, string authorIconUrl, string title, string description, string url, string thumbnailUrl, string imageUrl, List<Field> fields, string footer, string footerIconUrl, DiscordColor color, bool timestamp)
        {
            try
            {
                DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();
                embedBuilder.WithColor(color);
                if (timestamp) embedBuilder.Timestamp = DateTimeOffset.Now;
                embedBuilder.WithFooter(string.IsNullOrEmpty(footer) ? null : footer, string.IsNullOrEmpty(footerIconUrl) ? null : footerIconUrl);
                embedBuilder.WithAuthor(string.IsNullOrEmpty(author) ? null : author, string.IsNullOrEmpty(authorUrl) ? null : authorUrl, string.IsNullOrEmpty(authorIconUrl) ? null : authorIconUrl);
                embedBuilder.WithTitle(string.IsNullOrEmpty(title) ? null : title);
                embedBuilder.WithDescription(string.IsNullOrEmpty(description) ? null : description);
                embedBuilder.WithUrl(string.IsNullOrEmpty(url) ? null : url);
                embedBuilder.WithThumbnail(string.IsNullOrEmpty(thumbnailUrl) ? null : thumbnailUrl);
                embedBuilder.WithImageUrl(string.IsNullOrEmpty(imageUrl) ? null : imageUrl);
                if (fields != null && fields.Count > 0)
                {
                    foreach (Field field in fields)
                    {
                        embedBuilder.AddField(string.IsNullOrEmpty(field.Name) ? null : field.Name, string.IsNullOrEmpty(field.Value) ? null : field.Value, field.Inline);
                    }
                }
                return embedBuilder.Build();
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return null;
            }
        }
    }

    public class Field
    {
        public bool Inline { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }

        public Field(object name, object value)
        {
            Inline = true;
            Name = name.ToString();
            Value = value.ToString();
        }

        public Field(bool inline, object name, object value)
        {
            Inline = inline;
            Name = name.ToString();
            Value = value.ToString();
        }
    }

    public static class Writer
    {
        public static void Write(string line, string path)
        {
            try
            {
                if (!File.Exists(path))
                    File.Create(path).Close();
                Remove(line, path);
                using (StreamWriter writer = new StreamWriter(path, true))
                {
                    writer.WriteLine(line);
                    writer.Flush();
                    writer.Close();
                }
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }

        public static void Remove(string line, string path)
        {
            try
            {
                if (!File.Exists(path))
                    File.Create(path).Close();
                line = FindLine(line, path);
                if (line == "")
                    return;
                List<string> lines = new List<string>();
                foreach (string str in Reader.Read(path))
                {
                    if (str != line)
                    {
                        lines.Add(str);
                    }
                }
                Reader.Delete(path);
                File.Create(path).Close();
                foreach (string l in lines)
                    Write(l, path);
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }
        
        public static string FindLine(string search, string path)
        {
            try
            {
                if (!File.Exists(path))
                    File.Create(path).Close();
                foreach (string line in Reader.Read(path))
                {
                    if (line.StartsWith(search) || line.EndsWith(search) || line.Contains(search) || line == search)
                        return line;
                }
                return null;
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return null;
            }
        }
    }

    public static class RoleManager
    {
        public static string RolesAfterJoin = $"{Paths.ModThingsPath}/RolesAfterJoin.json";
        public static List<ulong> Roles = new List<ulong>();

        public static void Read()
        {
            CreateDefault();
            Roles = JsonConvert.DeserializeObject<List<ulong>>(Reader.ReadAll(RolesAfterJoin));
        }

        public static void CreateDefault()
        {
            if (!File.Exists(RolesAfterJoin))
            {
                File.Create(RolesAfterJoin).Close();
                Save();
            }
        }

        public static void Save()
        {
            if (File.Exists(RolesAfterJoin))
                File.Delete(RolesAfterJoin);
            Writer.Write(JsonConvert.SerializeObject(Roles), RolesAfterJoin);
        }

        public static void CheckRole(DiscordMember user, DiscordGuild guild)
        {
            if (Roles == null)
                return;
            List<DiscordRole> roles = Roles.Select(h => h.GetRole()).ToList();
            if (roles.Count > 0)
            {
                for (int i = 0; i < roles.Count; i++)
                {
                    if (roles[i] != null)
                        user.GrantRoleAsync(roles[i]);
                }
            }
        }

        public static void AddRoleAfterJoin(DiscordRole role)
        {
            if (!Roles.Contains(role.Id))
            {
                Roles.Add(role.Id);
                Save();
            }
        }

        public static void RemoveRoleAfterJoin(DiscordRole role)
        {
            if (Roles.Contains(role.Id))
            {
                Roles.Remove(role.Id);
                Save();
            }
        }
    }

    public static class Reader
    {
        public static void Delete(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        public static string[] Read(string path)
        {
            try
            {
                if (!File.Exists(path))
                    File.Create(path).Close();
                return Paths.ReadAllLines(path);
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return null;
            }
        }

        public static string ReadAll(string path)
        {
            try
            {
                if (!File.Exists(path))
                    File.Create(path).Close();
                return File.ReadAllText(path);
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return null;
            }
        }

        public static bool Contains(string line, string path)
        {
            try
            {
                if (!File.Exists(path))
                    File.Create(path).Close();
                foreach (string str in Read(path))
                {
                    if (str == line || str.StartsWith(line) || str.EndsWith(line) || str.Contains(line))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return false;
            }
        }
    }

    public static class MuteHandler
    {
        public static string File = $"{Paths.ModThingsPath}/Mutes.json";
        public static List<MuteData> Mutes = new List<MuteData>();
        public static bool ForceStop = false;

        public static string Mute(DiscordMessage msg, DiscordGuild guild, DiscordMember user, DiscordMember issuer, ulong duration, string reason, Timer.Time time, bool isPermanent = false, bool pm = true)
        {
            try
            {
                if (isPermanent)
                {
                    user.GrantRoleAsync(Helper.MuteRoles[guild.Id].GetRole());
                    if (pm)
                        user.SendMessageAsync(ClassBuilder.BuildEmbed($"{issuer.Username}#{issuer.Discriminator}", "", issuer.GetAvatar(), "", $":warning: You were permanently muted in **{guild.Name}**!", "", "", "", new List<Field>() { new Field("Reason", reason) }, "", "", Helper.GetRandomColor(), false));
                    if (msg != null)
                        return $":white_check_mark: {user.Mention} was permanently muted.\nReason: **{reason}**";
                }

                AddMute(CalculateSeconds(Timer.ParseTime(duration, time)), guild.Id, user.Id);
                string str = Timer.ParseString(duration, time);
                user.GrantRoleAsync(Helper.MuteRoles[guild.Id].GetRole());
                if (pm)
                    user.SendMessageAsync(ClassBuilder.BuildEmbed($"{issuer.Username}#{issuer.Discriminator}", "", issuer.GetAvatar(), "", $":warning: You were muted in **{guild.Name}**!", "", "", "", new List<Field>() { new Field("Reason", reason), new Field("Duration", str) }, "", "", Helper.GetRandomColor(), false));
                if (msg != null)
                    return $":white_check_mark: **{user.Mention}** was muted for **{duration} {str}**.\nReason: **{reason}**";
                return "";
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return $":x: An error occured! Exception of type {e.GetType().Name} was thrown.";
            }
        }

        public static string Unmute(DiscordMessage msg, DiscordGuild guild, DiscordMember user)
        {
            try
            {
                RemoveMute(user.Id, guild.Id);
                if (msg != null)
                    return $":white_check_mark: {user.Mention} was unmuted.";
                return "";
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return $":x: An error occured! Exception of type {e.GetType().Name} was thrown.";
            }
        }

        public static void AddMute(double dur, ulong server, ulong user)
        {
            MuteData muteData = new MuteData();
            muteData.Duration = dur;
            muteData.ServerId = server;
            muteData.UserId = user;
            Mutes.Add(muteData);
            Save();
        }

        public static void Read()
        {
            try
            {
                if (!System.IO.File.Exists(File))
                {
                    System.IO.File.Create(File).Close();
                    Writer.Write(JsonConvert.SerializeObject(Mutes), File);
                }

                Mutes = JsonConvert.DeserializeObject<List<MuteData>>(Reader.ReadAll(File));
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }

        public static void Update()
        {
            try
            {
                if (ForceStop)
                    return;
                foreach (MuteData data in Mutes)
                {
                    data.Duration -= 1;
                    if (data.Duration == 0)
                    {
                        RemoveMute(data.UserId, data.ServerId);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }

        public static void RemoveMute(ulong userId, ulong guildId)
        {
            try
            {
                DiscordRole role = Helper.MuteRoles[guildId].GetRole();
                DiscordMember user = userId.ToString().GetUser();
                DiscordGuild guild = Bot.Client.Guilds.Where(h => h.Value.Id == guildId).FirstOrDefault().Value;
                if (user != null && role != null && guild != null)
                {
                    user.RevokeRoleAsync(role);
                    user.SendMessageAsync(ClassBuilder.BuildEmbed("", "", "", "", $":warning: You were unmuted in **{guild.Name}**!", "", "", "", null, "", "", Helper.GetRandomColor(), false));
                }

                List<MuteData> mutes = new List<MuteData>();
                foreach (MuteData data in Mutes)
                {
                    if (data.UserId != userId)
                        mutes.Add(data);
                }
                Mutes = mutes;
                Save();
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }

        public static void Save()
        {
            try
            {
                ForceStop = true;
                if (System.IO.File.Exists(File))
                    System.IO.File.Create(File).Close();
                Writer.Write(JsonConvert.SerializeObject(Mutes), File);
                ForceStop = false;
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }

        public static double CalculateSeconds(TimeSpan span)
        {
            return MathF.Ceiling((float)(span - TimeSpan.FromTicks(DateTime.Now.Ticks)).TotalSeconds);
        }
    }

    public class MuteData
    {
        [JsonProperty("duration")]
        public double Duration { get; set; }

        [JsonProperty("user")]
        public ulong UserId { get; set; }

        [JsonProperty("server")]
        public ulong ServerId { get; set; }
    }

    public static class WarnHandler
    {
        public class WarnData
        {
            [JsonProperty("user")]
            public ulong User { get; set; }

            [JsonProperty("log")]
            public List<WarnReason> Log { get; set; }
        }

        public class WarnReason
        {
            [JsonProperty("reason")]
            public string Reason { get; set; }

            [JsonProperty("id")]
            public ulong Id { get; set; }

            [JsonProperty("issuer")]
            public ulong Issuer { get; set; }
        }

        public static List<WarnData> Warns = new List<WarnData>();
        public static string WDict = $"{Paths.ModThingsPath}/Warns/";

        public static string GetPath(ulong usr) => $"{WDict}/{usr}.json";

        public static void Warn(DiscordMember usr, DiscordMember issuer, string reason)
        {
            if (Warns.Select(h => h.User).Contains(usr.Id))
            {
                WarnData data = Warns.Where(h => h.User == usr.Id).FirstOrDefault();
                WarnReason res = new WarnReason();
                res.Id = GetNextId(usr.Id);
                res.Issuer = issuer.Id;
                res.Reason = reason;
                data.Log.Add(res);
                Save();
            }
            else
            {
                WarnData data = new WarnData();
                data.User = usr.Id;
                data.Log = new List<WarnReason>();
                WarnReason res = new WarnReason();
                res.Id = GetNextId(usr.Id);
                res.Issuer = issuer.Id;
                res.Reason = reason;
                data.Log.Add(res);
                Warns.Add(data);
                Save();
            }
        }

        public static ulong GetNextId(ulong user)
        {
            WarnData data = null;
            foreach (WarnData d in Warns)
            {
                if (d.User == user)
                    data = null;
            }
            
            if (data != null)
            {
                return data.Log.Count < 1 || data.Log.LastOrDefault() == null ? 0 : data.Log.LastOrDefault().Id + 1;
            }
            else
            {
                return 0;
            }
        }

        public static List<WarnReason> GetWarns(ulong user)
        {
            WarnData data = null;
            foreach (WarnData d in Warns)
            {
                if (d.User == user)
                    data = d;
            }

            if (data != null)
            {
                return data.Log;
            }
            else
                return new List<WarnReason>();
        }

        public static List<Field> GetWarnsField(ulong user)
        {
            List<WarnReason> data = GetWarns(user);
            if (data.Count < 1)
                return null;
            else
            {
                List<Field> fields = new List<Field>();
                foreach (WarnReason wr in data)
                {
                    DiscordMember issuer = wr.Issuer.ToString().GetUser();
                    Field field = new Field($"Case #{wr.Id}", $"**Issuer**: {(issuer == null ? "None" : issuer.Mention)}\n**Reason**: {wr.Reason}");
                    fields.Add(field);
                }
                return fields;
            }
        }

        public static void Save()
        {
            foreach (WarnData w in Warns)
            {
                string path = GetPath(w.User);
                if (File.Exists(path)) File.Delete(path);
                File.Create(path).Close();
                Writer.Write(JsonConvert.SerializeObject(w), GetPath(w.User));
            }
        }

        public static void Read()
        {
            if (!Directory.Exists(WDict))
                Directory.CreateDirectory(WDict);
            foreach (string file in Directory.GetFiles(WDict))
            {
                Warns.Add(JsonConvert.DeserializeObject<WarnData>(Reader.ReadAll(file)));
            }
        }
    }

    public static class BanHandler
    {
        public static List<BanData> Bans = new List<BanData>();
        public static string File = $"{Paths.ModThingsPath}/Bans.json";
        public static bool ForceStop = false;

        public static void Update()
        {
            try
            {
                if (ForceStop)
                    return;
                foreach (BanData data in Bans)
                {
                    data.Duration -= 1;
                    if (data.Duration == 0)
                    {
                        RemoveBan(data.UserId, data.ServerId);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }

        public static void RemoveBan(ulong userId, ulong serverId)
        {
            try
            {
                DiscordGuild guild = Bot.Client.GetGuildAsync(serverId).GetAwaiter().GetResult();
                DiscordBan ban = GetBan(userId, guild);
                if (ban == null)
                    return;
                guild.UnbanMemberAsync(ban.User);
                List<BanData> bans = new List<BanData>();
                foreach (BanData data in Bans)
                {
                    if (data.UserId != userId)
                        bans.Add(data);
                }
                Bans = bans;
                Save();
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }

        public static DiscordBan GetBan(ulong userId, DiscordGuild guild)
        {
            try
            {
                if (guild == null)
                    return null;
                IEnumerable<DiscordBan> bans = guild.GetBansAsync().GetAwaiter().GetResult();
                foreach (DiscordBan ban in bans)
                {
                    if (ban.User.Id == userId)
                        return ban;
                }
                return null;
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return null;
            }
        }

        public static string Ban(DiscordMessage msg, DiscordGuild guild, DiscordMember user, DiscordMember issuer, ulong duration, string reason, Timer.Time time, bool isPermanent = false, bool pm = true)
        {
            try
            {
                if (!guild.Members.Select(h => h.Value.Id).Contains(user.Id))
                {
                    return $":x: An error occured while processing your request!\nThis guild does not contain this user!";
                }

                if (isPermanent)
                {
                    user.SendMessageAsync(ClassBuilder.BuildEmbed($"{issuer.Username}#{issuer.Discriminator}", "", issuer.GetAvatar(), "", $":warning: You were permanently banned from **{guild.Name}**!", "", "", "", new List<Field>() { new Field("Reason", reason) }, "", "", Helper.GetRandomColor(), false));
                    guild.BanMemberAsync(user, 7, reason);
                    return $":white_check_mark: Permanently banned {user.Mention}.\nReason: **{reason}**";
                }

                AddBan(guild.Id, MuteHandler.CalculateSeconds(Timer.ParseTime(duration, time)), user.Id);
                string str = Timer.ParseString(duration, time);
                if (pm)
                    user.SendMessageAsync(ClassBuilder.BuildEmbed($"{issuer.Username}#{issuer.Discriminator}", "", issuer.GetAvatar(), "", $":warning: You were banned from **{guild.Name}**!", "", "", "", new List<Field>() { new Field("Reason", reason), new Field("Duration", str) }, "", "", Helper.GetRandomColor(), false));
                guild.BanMemberAsync(user, 7, reason);
                return $":white_check_mark: Banned **{user.Username}#{user.Discriminator}** for **{duration} {str}**.\nReason: **{reason}**";
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return $":x: An error occured! Exception of type **{e.GetType().Name}** was thrown.";
            }
        }

        public static string Unban(string args, DiscordGuild guild, DiscordMessage msg)
        {
            try
            {
                ulong id = 0;
                IReadOnlyCollection<DiscordBan> bans = guild.GetBansAsync().GetAwaiter().GetResult();
                if (ulong.TryParse(args.RemoveCharacters(), out id))
                {
                    RemoveBan(id, guild.Id);
                }
                else
                {
                    foreach (DiscordBan ban in bans)
                    {
                        args = args.ToLower();
                        if (ban.User.Username.ToLower() == args.ToLower() || ban.User.Username.ToLower().Contains(args))
                        {
                            RemoveBan(ban.User.Id, guild.Id);
                            return $":white_check_mark: Succesfully unbanned {ban.User.Mention}";
                        }
                    }
                }

                return "";
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return $":x: An error occured! Exception of type {e.GetType().Name} was thrown.";
            }
        }

        public static void AddBan(ulong id, double dur, ulong user)
        {
            BanData banData = new BanData();
            banData.Duration = dur;
            banData.ServerId = id;
            banData.UserId = user;
            Bans.Add(banData);
            Save();
        }

        public static void Read()
        {
            try
            {
                if (!System.IO.File.Exists(File))
                {
                    System.IO.File.Create(File).Close();
                    Writer.Write(JsonConvert.SerializeObject(Bans), File);
                }

                Bans = JsonConvert.DeserializeObject<List<BanData>>(Reader.ReadAll(File));
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }

        public static void Save()
        {
            ForceStop = true;
            if (System.IO.File.Exists(File))
                System.IO.File.Delete(File);
            Writer.Write(JsonConvert.SerializeObject(Bans), File);
            ForceStop = false;
        }
    }

    public class BanData
    {
        [JsonProperty("user")]
        public ulong UserId { get; set; }

        [JsonProperty("duration")]
        public double Duration { get; set; }

        [JsonProperty("server")]
        public ulong ServerId { get; set; }
    }


    public static class VoiceManager
    {
        public static string RentSettingsFile = $"{Paths.ModThingsPath}/RentSettings.json";
        public static RentSettings RentSettings = null;

        public static void SetVoice(ulong voiceId)
        {
            RentSettings.Voice = voiceId;
            Save();
        }

        public static void Save()
        {
            try
            {
                if (File.Exists(RentSettingsFile))
                    File.Delete(RentSettingsFile);
                Writer.Write(JsonConvert.SerializeObject(RentSettings), RentSettingsFile);
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }

        public static void Read()
        {
            try
            {
                CreateDefault();
                RentSettings = JsonConvert.DeserializeObject<RentSettings>(Reader.ReadAll(RentSettingsFile));
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }

        public static void CreateDefault()
        {
            try
            {
                string str = File.Exists(RentSettingsFile) ? Reader.ReadAll(RentSettingsFile) : "null";
                if (string.IsNullOrEmpty(str) || str == "null")
                {
                    RentSettings = new RentSettings();
                    RentSettings.Bitrate = 8000;
                    RentSettings.CategoryId = 0;
                    RentSettings.Voice = 0;
                    Save();
                }
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }

        public static void ModifySettings(RentSettings newSettings)
        {
            RentSettings = newSettings;
            Save();
        }

        public static async Task CreateVoice(DiscordGuild guild, DiscordMember user, string name, int userLimit)
        {
            try
            {
                if (guild == null || user == null)
                    return;
                if (string.IsNullOrEmpty(name))
                    return;
                if (userLimit < 1)
                    return;
                RentSettings settings = RentSettings;
                if (settings == null)
                    return;
                DiscordChannel channel = user.GetCurrentVoice();
                if (channel == null)
                    return;
                ulong rentVoice = settings.Voice;
                if (rentVoice == 0)
                    return;
                if (channel.Id != rentVoice)
                    return;
                DiscordChannel result = await guild.CreateVoiceChannelAsync(name, channel.Parent);
                DiscordChannel voiceCh = result.Id.ToString().GetVoice();
                await voiceCh.ModifyAsync((e) =>
                {
                    if (settings.Bitrate > 7999)
                        e.Bitrate = settings.Bitrate;
                    if (!string.IsNullOrEmpty(name))
                        e.Name = name;
                });

                await voiceCh.AddOverwriteAsync(user, Permissions.All, Permissions.None);
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }
    }

    public class RentSettings
    {
        [JsonProperty("categoryId")]
        public ulong CategoryId { get; set; }

        [JsonProperty("bitrate")]
        public int Bitrate { get; set; }

        [JsonProperty("rentVoice")]
        public ulong Voice { get; set; }
    }

    public static class ServerLog
    {
        public static void Add(DiscordEmbed embed)
        {
            if (Configs.BotConfig.ServerLogChannel == 0)
                return;
            DiscordChannel channel = Configs.BotConfig.ServerLogChannel.ToString().GetTextChannel();
            if (channel == null)
                return;
            channel.SendMessageAsync(embed);
        }
    }

    public static class ModLog
    {
        public static void Add(DiscordEmbed embed)
        {
            if (Configs.BotConfig.ModLogChannel == 0)
                return;
            DiscordChannel channel = Configs.BotConfig.ModLogChannel.ToString().GetTextChannel();
            if (channel == null)
                return;
            channel.SendMessageAsync(embed);
        }
    }

    public static class Timer
    {
        public static string TimeToString(DateTime time)
        {
            return time.ToString("F");
        }

        public enum Time
        {
            Second,
            Minute,
            Hour,
            Day,
            Month,
            Year
        }

        public static string ParseString(ulong dur, Time time)
        {
            string str = "";
            if (time == Time.Day)
            {
                if (dur > 1)
                    str = "days";
                else
                    str = "day";
            }

            if (time == Time.Hour)
            {
                if (dur > 1)
                    str = "hours";
                else
                    str = "hour";
            }

            if (time == Time.Minute)
            {
                if (dur > 1)
                    str = "minutes";
                else
                    str = "minute";
            }

            if (time == Time.Month)
            {
                if (dur > 1)
                    str = "months";
                else
                    str = "month";
            }

            if (time == Time.Second)
            {
                if (dur > 1)
                    str = "seconds";
                else
                    str = "second";
            }

            if (time == Time.Year)
            {
                if (dur > 1)
                    str = "years";
                else
                    str = "year";
            }
            return str;
        }

        public static TimeSpan ParseTime(ulong dur, Time time)
        {
            DateTime now = DateTime.Now;
            DateTime end = default;
            if (time == Time.Day)
                end = now.AddDays(dur);
            if (time == Time.Hour)
                end = now.AddHours(dur);
            if (time == Time.Minute)
                end = now.AddMinutes(dur);
            if (time == Time.Month)
                end = now.AddMonths((int)dur);
            if (time == Time.Second)
                end = now.AddSeconds(dur);
            if (time == Time.Year)
                end = now.AddYears((int)dur);
            return TimeSpan.FromTicks(end.Ticks);
        }

        public static DateTime ParseDate(TimeSpan span) => new DateTime(span.Ticks);

        public static bool TryParse(this string arg, out Time time)
        {
            if (int.TryParse(arg, out int t))
            {
                time = Time.Hour;
                return true;
            }

            foreach (Time e in Enum.GetValues(typeof(Time)).Cast<Time>())
            {
                if (e.ToString().ToLower() == arg.ToLower() || arg.ToLower().Contains(e.ToString().ToLower()))
                {
                    time = e;
                    return true;
                }
            }

            if (arg.Contains("s"))
            {
                time = Time.Second;
                return true;
            }

            if (arg.Contains("m"))
            {
                time = Time.Minute;
                return true;
            }

            if (arg.Contains("h"))
            {
                time = Time.Hour;
                return true;
            }

            if (arg.Contains("d"))
            {
                time = Time.Day;
                return true;
            }

            if (arg.Contains("M"))
            {
                time = Time.Month;
                return true;
            }

            if (arg.Contains("y"))
            {
                time = Time.Year;
                return true;
            }

            time = Time.Hour;
            return false;
        }

        public static ulong ParseDuration(string args)
        {
            bool numberFound = false;
            ulong number = 0;
            string str = "";
            foreach (char c in args)
            {
                if (ulong.TryParse(c.ToString(), out number))
                {
                    numberFound = true;
                    str += number.ToString();
                }
            }

            if (numberFound && ulong.TryParse(str, out ulong res))
            {
                return res;
            }
            return 0;
        }
    }

    public enum PermType
    {
        Everyone = 0,
        Moderator = 1,
        BotMaster = 2,
        Custom = 3
    }
}
