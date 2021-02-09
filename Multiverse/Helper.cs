using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Multiverse.API;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using Lavalink4NET;
using Lavalink4NET.DSharpPlus;
using Lavalink4NET.Player;
using System.Management;

namespace Multiverse
{
    public static class Helper
    {
        public class SystemInformation
        {
            public string OperatingSystem;

            public string CpuCoreCount;
            public string CpuClockSpeed;
            public string CpuManufacturer;
            public string CpuSocket;
            public string CpuName;

            public string GpuName;

            public static SystemInformation Get()
            {
                SystemInformation info = new SystemInformation();
                ManagementClass cpuInfo = new ManagementClass("Win32_Processor");
                ManagementClass gpuInfo = new ManagementClass("Win32_VideoController");
                ManagementObjectCollection cpuCollection = cpuInfo.GetInstances();
                ManagementObjectCollection gpuCollection = gpuInfo.GetInstances();

                foreach (ManagementObject obj in cpuCollection)
                {
                    foreach (PropertyData data in obj.Properties)
                    {
                        if (data.Name == "NumberOfCores") info.CpuCoreCount = data.Value.ToString();
                        if (data.Name == "MaxClockSpeed") info.CpuClockSpeed = data.Value.ToString();
                        if (data.Name == "Manufacturer") info.CpuManufacturer = data.Value.ToString();
                        if (data.Name == "Name") info.CpuName = data.Value.ToString();
                        if (data.Name == "SocketDesignation") info.CpuSocket = data.Value.ToString();
                    }
                }

                foreach (ManagementObject obj in gpuCollection)
                {
                    foreach (PropertyData data in obj.Properties)
                    {
                        if (data.Name == "Name") info.GpuName = data.Value.ToString();
                    }
                }

                info.OperatingSystem = Environment.OSVersion.ToString();
                return info;
            }
        }

        public static DiscordChannel CurrentChannel;

        public static VoiceNextExtension VoiceNextClient;
        public static VoiceNextConnection VoiceNextConnection;
        public static VoiceTransmitSink Sink;

        public static LavalinkNode LavalinkClient;
        public static LavalinkPlayer LavalinkPlayer;

        public static Dictionary<ulong, ulong> MuteRoles = new Dictionary<ulong, ulong>();
        public static List<DiscordColor> AllColors = new List<DiscordColor>() { DiscordColor.Aquamarine, DiscordColor.Azure, DiscordColor.Black, DiscordColor.Blue, DiscordColor.Blurple, DiscordColor.Brown, DiscordColor.Chartreuse, DiscordColor.CornflowerBlue, DiscordColor.Cyan, DiscordColor.DarkBlue, DiscordColor.DarkButNotBlack, DiscordColor.DarkGray, DiscordColor.DarkGreen, DiscordColor.DarkRed, DiscordColor.Gold, DiscordColor.Goldenrod, DiscordColor.Gray, DiscordColor.Grayple, DiscordColor.Green, DiscordColor.HotPink, DiscordColor.IndianRed, DiscordColor.LightGray, DiscordColor.Lilac, DiscordColor.Magenta, DiscordColor.MidnightBlue, DiscordColor.NotQuiteBlack, DiscordColor.Orange, DiscordColor.PhthaloBlue, DiscordColor.PhthaloGreen, DiscordColor.Purple, DiscordColor.Red, DiscordColor.Rose, DiscordColor.SapGreen, DiscordColor.Sienna, DiscordColor.SpringGreen, DiscordColor.Teal, DiscordColor.Turquoise, DiscordColor.VeryDarkGray, DiscordColor.Violet, DiscordColor.Wheat, DiscordColor.White, DiscordColor.Yellow };


        public enum ConnectionType
        {
            Lavalink,
            VoiceNext
        }

        public static double ToMb(ulong bytes)
        {
            return (bytes / 1024f) / 1024f;
        }

        public static async Task CommitAddAll(DiscordRole role, DiscordGuild guild, string addedBy)
        {
            foreach (DiscordMember user in guild.Members.Values)
            {
                try
                {
                    await user.GrantRoleAsync(role, $"{addedBy} used changerole command on all users.");
                }
                catch
                {
                    continue;
                }
            }
        }

        public static async Task CommitRemoveAll(DiscordMember member, string removedBy)
        {
            foreach (DiscordRole role in member.Roles)
            {
                try
                {
                    if (role.Id == member.Guild.EveryoneRole.Id) continue;
                    await member.RevokeRoleAsync(role, $"{removedBy} used changerole removeall on {member.Username}#{member.Discriminator}");
                }
                catch
                {
                    continue;
                }
            }
        }

        public static async Task SetRadioStatus(string name, string url)
        {
            await Bot.Client.UpdateStatusAsync(new DiscordActivity
            {
                ActivityType = ActivityType.Streaming,
                Name = name,
                StreamUrl = url
            }, UserStatus.Online);
        }

        public static async Task SetPlaying(LavalinkTrack track)
        {
            await Bot.Client.UpdateStatusAsync(new DiscordActivity
            {
                ActivityType = ActivityType.Streaming,
                Name = track.Title,
                StreamUrl = track.Source
            }, UserStatus.Online);
        }

        public static async Task SetDefaultStatus()
        {
            await Bot.Client.UpdateStatusAsync(new DiscordActivity
            {
                ActivityType = ActivityType.ListeningTo,
                Name = $"{Configs.BotConfig.Prefix}help",
            }, UserStatus.Idle);
        }

        public static async Task Join(DiscordChannel channel, ConnectionType typeToUse)
        {
            try
            {
                if (typeToUse == ConnectionType.Lavalink)
                {
                    if (!LavalinkClient.IsConnected)
                    {
                        Logger.Error($"[NODE] The client isn't connected to any node.");
                        return;
                    }

                    if (CurrentChannel == null)
                        LavalinkPlayer = await LavalinkClient.JoinAsync(channel.GuildId, channel.Id, true);
                    else
                        LavalinkPlayer = LavalinkClient.GetPlayer(channel.GuildId);
                    CurrentChannel = channel;
                }
                else
                {
                    if (VoiceNextConnection == null || !VoiceNextConnection.IsPlaying)
                    {
                        VoiceNextClient = Bot.Client.GetVoiceNext();
                        VoiceNextConnection = await channel.ConnectAsync();
                        await VoiceNextConnection.TargetChannel.Guild.CurrentMember.SetDeafAsync(true);
                        Sink = VoiceNextConnection.GetTransmitSink();

                        VoiceNextConnection.VoiceReceived += VoiceNextConnection_VoiceReceived;
                        VoiceNextConnection.VoiceSocketErrored += VoiceNextConnection_VoiceSocketErrored;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }

        private static Task VoiceNextConnection_VoiceSocketErrored(VoiceNextConnection sender, DSharpPlus.EventArgs.SocketErrorEventArgs e)
        {
            Logger.Error($"[NEXT] An error occured while communicating with websocket! \n{e.Exception}");
            return Task.CompletedTask;
        }

        private static Task VoiceNextConnection_VoiceReceived(VoiceNextConnection sender, DSharpPlus.VoiceNext.EventArgs.VoiceReceiveEventArgs e)
        {
            Logger.Debug($"[NEXT] VoiceUpdate received!");
            return Task.CompletedTask;
        }

        public static bool CanExecute(DiscordMember user)
        {
            DiscordChannel voice = user.GetCurrentVoice();
            if (voice == null) return false;
            if (CurrentChannel == null) Join(voice, ConnectionType.Lavalink).GetAwaiter().GetResult();
            if (CurrentChannel.Id == voice.Id) return true;
            return false;
        }

        public static async Task Disconnect()
        {
            if (Radio.Streamer != null) Radio.Streamer.Stop();
            Radio.sendingdatas = false;
            Radio.Cancel = true;
            if (LavalinkPlayer != null) await LavalinkPlayer.DisconnectAsync();
            await ClearStream();
            if (VoiceNextConnection != null) VoiceNextConnection.Disconnect();
            CurrentChannel = null;
            await SetDefaultStatus();
        }

        public static async Task ClearStream()
        {
            if (Sink != null)
            {
                await Sink.FlushAsync();
            }
        }

        public static async Task RefreshRadio()
        {
            VoiceNextConnection.Disconnect();
            VoiceNextConnection = await VoiceNextClient.ConnectAsync(CurrentChannel);
            Sink = VoiceNextConnection.GetTransmitSink();
        }
        
        public static async Task Initialize()
        {
            try
            {
                VoiceNextClient = Bot.Client.UseVoiceNext(new VoiceNextConfiguration
                {
                    EnableIncoming = false
                });

                LavalinkClient = new LavalinkNode(new LavalinkNodeOptions
                {
                    AllowResuming = true,
                    DebugPayloads = true,
                    DisconnectOnStop = false,
                    Password = Configs.BotConfig.LavalinkPassword,
                    RestUri = "http://localhost:2333/",
                    WebSocketUri = "ws://localhost:2333/"
                }, new DiscordClientWrapper(Bot.Client));

                LavalinkClient.Connected += LavalinkClient_Connected;
                LavalinkClient.Disconnected += LavalinkClient_Disconnected;
                LavalinkClient.PayloadReceived += LavalinkClient_PayloadReceived;
                LavalinkClient.PlayerConnected += LavalinkClient_PlayerConnected;
                LavalinkClient.PlayerDisconnected += LavalinkClient_PlayerDisconnected;
                LavalinkClient.ReconnectAttempt += LavalinkClient_ReconnectAttempt;
                LavalinkClient.StatisticsUpdated += LavalinkClient_StatisticsUpdated;
                LavalinkClient.TrackException += LavalinkClient_TrackException;
                LavalinkClient.TrackStarted += LavalinkClient_TrackStarted;
                LavalinkClient.TrackStuck += LavalinkClient_TrackStuck;
                LavalinkClient.WebSocketClosed += LavalinkClient_WebSocketClosed;

                await LavalinkClient.InitializeAsync();
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }

        private static Task LavalinkClient_WebSocketClosed(object sender, Lavalink4NET.Events.WebSocketClosedEventArgs eventArgs)
        {
            Logger.Error($"[NODE] WebSocket closed connection! {eventArgs.CloseCode} - {eventArgs.Reason}");
            return SetDefaultStatus();
        }

        private static Task LavalinkClient_TrackStuck(object sender, Lavalink4NET.Events.TrackStuckEventArgs eventArgs)
        {
            Logger.Error($"[NODE] Track got stuck! {eventArgs.Player.CurrentTrack.Title}");
            return Task.CompletedTask;
        }

        private static Task LavalinkClient_TrackStarted(object sender, Lavalink4NET.Events.TrackStartedEventArgs eventArgs)
        { 
            Logger.Debug($"[NODE] Started playing {eventArgs.Player.CurrentTrack.Title}");
            return SetPlaying(eventArgs.Player.CurrentTrack);
        }

        private static Task LavalinkClient_TrackException(object sender, Lavalink4NET.Events.TrackExceptionEventArgs eventArgs)
        {
            Logger.Error($"[NODE] An error occured while playing {eventArgs.Player.CurrentTrack.Title}!\n{eventArgs.Error}");
            return Task.CompletedTask;
        }

        private static Task LavalinkClient_StatisticsUpdated(object sender, Lavalink4NET.Events.NodeStatisticsUpdateEventArgs eventArgs)
        {
            Logger.Debug($"[NODE] Statistics updated.");
            return Task.CompletedTask;
        }

        private static Task LavalinkClient_ReconnectAttempt(object sender, Lavalink4NET.Events.ReconnectAttemptEventArgs eventArgs)
        {
            Logger.Info($"[NODE] Attempting reconnect to {eventArgs.Uri} ... [{eventArgs.Attempt}]");
            return Task.CompletedTask;
        }

        private static Task LavalinkClient_PlayerDisconnected(object sender, Lavalink4NET.Events.PlayerDisconnectedEventArgs eventArgs)
        {
            Logger.Debug($"[NODE] Player disconnected from {eventArgs.VoiceChannelId} - {eventArgs.DisconnectCause}");
            return SetDefaultStatus();
        }

        private static Task LavalinkClient_PlayerConnected(object sender, Lavalink4NET.Events.PlayerConnectedEventArgs eventArgs)
        {
            Logger.Debug($"[NODE] Player connected to {eventArgs.VoiceChannelId}");
            return Task.CompletedTask;
        }

        private static Task LavalinkClient_PayloadReceived(object sender, Lavalink4NET.Events.PayloadReceivedEventArgs eventArgs)
        {
            Logger.Debug($"[NODE] Payload received!");
            if (eventArgs.RawJson.Contains("TrackEndEvent"))
            {
                LavalinkTrack next = LavalinkPlayer.GuildId.PopTrack();
                if (next != null)
                {
                    LavalinkPlayer.PlayAsync(next);
                    return SetPlaying(next);
                }
                else
                    return SetDefaultStatus();
            }
            return Task.CompletedTask;
        }

        private static Task LavalinkClient_Disconnected(object sender, Lavalink4NET.Events.DisconnectedEventArgs eventArgs)
        {
            Logger.Warn($"[NODE] Disconnected from Lavalink node at {eventArgs.Uri}");
            return Task.CompletedTask;
        }

        private static Task LavalinkClient_Connected(object sender, Lavalink4NET.Events.ConnectedEventArgs eventArgs)
        {
            Logger.Info($"[NODE] Connected to Lavalink node at {eventArgs.Uri}");
            return Task.CompletedTask;
        }

        public static async Task Setup()
        {
            try
            {
                await Task.Delay(5000);
                foreach (DiscordGuild guild in Bot.Client.Guilds.Values)
                {
                    foreach (DiscordRole role in guild.Roles.Values)
                    {
                        if (role.Name == "Muted" && !MuteRoles.ContainsKey(guild.Id))
                        {
                            MuteRoles.TryAdd(guild.Id, role.Id);
                        }
                    }
                }

                foreach (DiscordGuild g in Bot.Client.Guilds.Values)
                {
                    if (!MuteRoles.ContainsKey(g.Id))
                    {
                        DiscordRole role = await g.CreateRoleAsync("Muted", Permissions.None, DiscordColor.Red, false, false, "Created a Muted role.");
                        MuteRoles.TryAdd(g.Id, role.Id);
                    }
                }

                foreach (ulong server in MuteRoles.Keys)
                {
                    ulong roleId = MuteRoles[server];
                    DiscordRole role = roleId.GetRole();
                    if (role == null)
                        continue;
                    foreach (DiscordChannel text in server.GetServer().Channels.Values.Where(h => h.Type == ChannelType.Text))
                    {
                        await text.AddOverwriteAsync(role, Permissions.None, Permissions.SendMessages, "Setting permissions for Muted roles");
                    }
                }

                try
                {
                    LevelManager.CheckDir();
                    LevelManager.Read();
                    LevelManager.SetLevels();
                    MuteHandler.Read();
                    BanHandler.Read();
                    VoiceManager.Read();
                    RoleManager.Read();
                    Suggestions.Read();
                    AutoModerator.Read();
                    GiveawayManager.Read();
                    WarnHandler.Read();
                    EventHandler.Save();

                    Logger.Info($"Found {MuteHandler.Mutes.Count} mute(s)!");
                    Logger.Info($"Found {BanHandler.Bans.Count} ban(s)!");
                    Logger.Info($"Found {MuteRoles.Count} mute role(s)!");
                }
                catch (Exception e)
                {
                    Logger.Exception(e);
                }

                await Task.CompletedTask;
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }

        public static async Task DeleteMessages(DiscordChannel channel, DiscordUser usr, int count)
        {
            IEnumerable<DiscordMessage> toDelete = await channel.GetMessagesAsync(count);
            await channel.DeleteMessagesAsync(toDelete, $"{usr.Username}#{usr.Discriminator} purged {count} messages");
        }

        public static async Task ChangeLock(DiscordChannel channel, bool state)
        {
            await channel.AddOverwriteAsync(channel.Guild.EveryoneRole, state ? Permissions.None : Permissions.SendMessages, state ? Permissions.None : Permissions.SendMessages);
        }

        public static DiscordColor GetRandomColor() => AllColors[new Random().Next(AllColors.Count)];

        public static void DeleteAfterUsage(ICommandHandler handler, DiscordMessage cmd)
        {
            if (handler == null || cmd == null)
                return;
            List<string> delete = Configs.BotConfig.CommandsToDelete;
            bool shouldDelete = delete.Contains("all") || (delete.Contains("everyone") && handler.PermType == PermType.Everyone) || (delete.Contains("moderator") && handler.PermType == PermType.Moderator) || (delete.Contains("master") && handler.PermType == PermType.BotMaster) || delete.Contains(cmd.Author.Id.ToString()) || delete.Contains(handler.Command) ? true : false;
            if (shouldDelete)
                cmd.DeleteAsync();
        }

        public static string Verify(DiscordMember usr, DiscordMember auth)
        {
            if (Configs.BotConfig.VerifiedRoleId == 0 || Configs.BotConfig.UnverifiedRoleId == 0)
                return ":x: You must set verification roles in the bot config before using this command.";
            DiscordRole verified = Configs.BotConfig.VerifiedRoleId.GetRole();
            DiscordRole unverified = Configs.BotConfig.UnverifiedRoleId.GetRole();
            if (verified == null || unverified == null)
                return ":x: You must set verification roles in the bot config before using this command.";
            usr.RevokeRoleAsync(unverified, $"Verified by {auth.Username}#{auth.Discriminator}");
            usr.GrantRoleAsync(verified, $"Verified by {auth.Username}#{auth.Discriminator}");
            usr.SendMessageAsync(ClassBuilder.BuildEmbed(usr.GetAuthor(), "", auth.GetAvatar(), "Verification", $":white_check_mark: **You were verified by {auth.Mention}**", "", "", "", null, "", "", GetRandomColor(), false));
            return $":white_check_mark: {usr.Mention} was verified by {auth.Mention}.";
        }
    }
}
