using System.Threading.Tasks;
using Multiverse.API;
using System.Linq;
using System;
using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace Multiverse
{
    public static class EventHandler
    {
        public static int MemePost = 3600;
        public static int AutoFact = 66400;
        public static int InfoEmbedTicks = 900;
        public static int upTime = 0;

        public static int InfoOnlineUsers = 0;
        public static int InfoDndUsers = 0;
        public static int InfoOfflineUsers = 0;
        public static int InfoTotalUsers = 0;
        public static int InfoTotalBots = 0;

        public static DiscordMessage InfoEmbedMessage = null;

        public static string GetUptime(TimeSpan span)
        {
            int minutes = span.Seconds / 60;
            int hours = minutes / 60;
            int days = hours / 24;
            return $"{(days > 0 ? $"{days} day(s), " : "")}{(hours > 0 ? $"{hours} hour(s), " : "")}{(minutes > 0 ? $"{minutes} minute(s)" : "")}";
        }

        public static string GetUptime()
        {
            int minutes = upTime / 60;
            int hours = minutes / 60;
            int days = hours / 24;
            return $":hourglass: Uptime: **{(days > 0 ? $"{days} day(s), " : "")}{(hours > 0 ? $"{hours} hour(s), " : "")}{(minutes > 0 ? $"{minutes} minute(s)" : "")}**";
        }

        public static async Task OnMessage(DiscordClient client, MessageCreateEventArgs ev)
        {
            DiscordMember usr = ev.Author.Id.ToString().GetUser();
            try
            {
                if (!ev.Author.IsBot)
                {
                    if (!AutoModerator.ShouldIgnore(usr))
                    {
                        if (AutoModerator.CheckForAll(usr.Guild, ev.Message))
                            return;
                    }
                }

                if (!usr.IsBot && !ev.Message.IsCommand())
                {
                    LevelManager.ProcessLevel(usr, ev.Message.Channel, ev.Message);
                }
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }

            if (ev.Message.Content.StartsWith(Bot.Client.CurrentUser.Mention))
            {
                await ev.Message.Channel.SendMessageAsync($"**Current prefix: `{Configs.BotConfig.Prefix}`**");
                return;
            }

            await CommandHandler.ProcessCommand(ev.Message, usr);
        }

        public static Task OnUserJoin(DiscordClient client, GuildMemberAddEventArgs ev)
        {
            if (MuteHandler.Mutes.Select(h => h.UserId).Contains(ev.Member.Id))
            {
                ev.Member.GrantRoleAsync(Helper.MuteRoles[ev.Guild.Id].GetRole(), "Muted");       
            }

            RoleManager.CheckRole(ev.Member, ev.Guild);
            return Task.CompletedTask;
        }

        public static void OnException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Error(e.ExceptionObject);
            if (e.IsTerminating)
                OnExit().GetAwaiter().GetResult();
        }

        public static void OnCancelPress(object sender, ConsoleCancelEventArgs e)
        {
            OnExit().GetAwaiter().GetResult();
        }

        public static void OnProcessExit(object sender, EventArgs e)
        {
            OnExit().GetAwaiter().GetResult();
        }

        public static async Task OnDisconnect(DiscordClient client, SocketCloseEventArgs ev)
        {
            Logger.Error($"[{ev.CloseCode}]: {ev.CloseMessage}");
            await OnExit();
        }

        public static async Task OnDisconnect(DiscordClient client, SocketErrorEventArgs ev)
        {
            Logger.Exception(ev.Exception);
            await OnExit();
        }

        public static void OnUpdate()
        {
            upTime++;
            MuteHandler.Update();
            BanHandler.Update();
            GiveawayManager.Update();
            MemePost--;
            AutoFact--;
            InfoEmbedTicks--;
            if (MemePost <= 0)
                PostMeme();
            if (AutoFact <= 0)
                PostFact();
            if (InfoEmbedTicks <= 0)
            {
                UpdateInfoEmbed();
                InfoEmbedTicks = 900;
            }
        }

        public static void Save()
        {
            MuteHandler.Save();
            BanHandler.Save();
            LevelManager.Save();
            Shop.Save();
            RoleManager.Save();
            VoiceManager.Save();
            Suggestions.Save();
            AutoModerator.Save();
            GiveawayManager.Save();
            WarnHandler.Save();
        }

        public static void UpdateInfoEmbed()
        {
            if (InfoEmbedMessage == null)
            {
                InfoEmbed infoEmbed = Configs.BotConfig.InfoEmbed;
                if (infoEmbed.ServerId == 0 || infoEmbed.MessageId == null || infoEmbed.MessageId == 0 || infoEmbed.ChannelId == 0) return;
                DiscordGuild guild = Bot.Client.Guilds.Where(h => h.Key == infoEmbed.ServerId).FirstOrDefault().Value;
                if (guild != null)
                {
                    DiscordChannel socketTextChannel = guild.GetChannel(infoEmbed.ChannelId);
                    if (socketTextChannel != null)
                    {
                        DiscordMessage socketMessage = socketTextChannel.GetMessageAsync(infoEmbed.MessageId.Value).GetAwaiter().GetResult();
                        if (socketMessage != null)
                        {
                            InfoEmbedMessage = socketMessage;
                        }
                    }
                }
            }

            if (InfoEmbedMessage != null)
            {
                DiscordGuild socketGuild = InfoEmbedMessage.Channel.Id.GetServer();
                if (socketGuild != null)
                {
                    int onlineUsers = socketGuild.GetUsers(UserStatus.Online);
                    int dndUsers = socketGuild.GetUsers(UserStatus.DoNotDisturb);
                    int offUsers = socketGuild.GetUsers(UserStatus.Offline);
                    int totalUsers = socketGuild.Members.Count;
                    int botUsers = socketGuild.Members.Where(h => h.Value.IsBot).Count();

                    if (onlineUsers != InfoOnlineUsers || dndUsers != InfoDndUsers || offUsers != InfoOfflineUsers || totalUsers != InfoTotalUsers || botUsers != InfoTotalBots)
                    {
                        InfoEmbedMessage.ModifyAsync(null, CreateInfoEmbed(socketGuild));
                    }
                }
            }
        }

        public static DiscordEmbed CreateInfoEmbed(DiscordGuild guild)
        {
            DiscordInvite invite = guild.GetInvitesAsync().GetAwaiter().GetResult().Where(h => !h.IsTemporary).FirstOrDefault();
            string inviteUrl = invite == null ? "" : $"https://discord.gg/{invite.Code}";
            int onlineUsers = guild.Members.Where(h => h.Value.Presence.Status == UserStatus.Online).Count();
            int dndUsers = guild.Members.Where(h => h.Value.Presence.Status == UserStatus.DoNotDisturb).Count();
            int offlineUsers = guild.Members.Where(h => h.Value.Presence.Status == UserStatus.Offline).Count();
            int botsCount = guild.Members.Where(h => h.Value.IsBot).Count();
            InfoDndUsers = dndUsers;
            InfoOfflineUsers = offlineUsers;
            InfoOnlineUsers = onlineUsers;
            InfoTotalBots = botsCount;
            InfoTotalUsers = guild.MemberCount;
            Field users = new Field("Users", $"Total: **{guild.MemberCount}**\nBots: **{botsCount}**\n\n:green_circle: **{onlineUsers}\n:red_circle: {dndUsers}\n:black_circle: {offlineUsers}**");
            Field bots = new Field("Bots", botsCount);
            Field roles = new Field("Roles", guild.Roles.Count);
            return ClassBuilder.BuildEmbed(guild.Name, inviteUrl, guild.IconUrl, "", "", "", "", "", new List<Field>() { users, roles }, "", "", DiscordColor.Aquamarine, false);
        }

        public static int GetUsers(this DiscordGuild guild, UserStatus status) => guild.Members.Where(h => h.Value.Presence.Status == status).Count();

        public static void CreateClient()
        {
            Bot.Client = new DiscordClient(CreateCFG());
        }

        public static void PostFact()
        {
            if (Configs.BotConfig.FactChannel == 0)
                return;
            DiscordChannel channel = Configs.BotConfig.FactChannel.ToString().GetTextChannel();
            if (channel == null)
                return;
            RandomFact fact = Client.RequestFact();
            channel.SendMessageAsync(ClassBuilder.BuildEmbed("", "", "", $"Fact of the day [{DateTime.Now.ToString("dd.MM.yyyy")}]", fact.Fact, "", "", "", null, "", "", Helper.GetRandomColor(), false));
            AutoFact = 66400;
        }

        public static void PostMeme()
        {
            if (Configs.BotConfig.AutoMemeChannel == 0)
                return;
            DiscordChannel channel = Configs.BotConfig.AutoMemeChannel.ToString().GetTextChannel();
            if (channel == null)
                return;
            MemePost post = Client.RequestMeme(Configs.BotConfig.Subreddit);
            if (post.Nsfw)
                post = Client.RequestMeme(Configs.BotConfig.Subreddit);
            DiscordEmbed e = ClassBuilder.BuildEmbed($"u/{post.Author}", "", "", post.Title, "", post.Link, "", post.Url, null, $"r/{post.Subreddit}", "", Helper.GetRandomColor(), false);
            channel.SendMessageAsync(e);
            MemePost = 3600;
        }

        public static DiscordConfiguration CreateCFG()
        {
            return new DiscordConfiguration
            {
                AutoReconnect = true,
                ReconnectIndefinitely = true,
                Token = Configs.BotConfig.Token,
                TokenType = TokenType.Bot,
                LoggerFactory = new Logger(),
                Intents = DiscordIntents.All
            };
        }

        public static async Task OnExit()
        {
            Logger.Error($"[MULTIVERSE] Exit was triggered!");
            Save();
            await Helper.Disconnect();
            await Task.Delay(30000);
        }

        public static async Task StartUpdating()
        {
            for (; ; )
            {
                await Task.Delay(1000);
                OnUpdate();
            }
        }
    }
}
