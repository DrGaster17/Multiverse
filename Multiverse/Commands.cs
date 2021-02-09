using Multiverse.API;
using System;
using System.Linq;
using VideoLibrary;
using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Lavalink4NET.DSharpPlus;
using Lavalink4NET.Statistics;
using Lavalink4NET.Lyrics;

namespace Multiverse.Commands
{
    public class CommandHelp : ICommandHandler
    {
        public string Command => "help";
        public string Aliases => "";
        public string Usage => "help (command)/list";
        public string Description => $"Lists all available commands/displays help for a specified command. Use **{Configs.BotConfig.Prefix}help list** to list all available commands.";
        public PermType PermType => PermType.Everyone;

        [Command("help")]
        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length > 0 && args[0].ToLower() == "list")
            {
                string author = $"{sender.Username}#{sender.Discriminator}";
                IEnumerable<ICommandHandler> mods = CommandManager.Commands.Where(h => h.PermType == PermType.Moderator);
                IEnumerable<ICommandHandler> masters = CommandManager.Commands.Where(h => h.PermType == PermType.BotMaster);
                IEnumerable<ICommandHandler> everyones = CommandManager.Commands.Where(h => h.PermType == PermType.Everyone);
                DiscordEmbed modCommands = ClassBuilder.BuildEmbed(author, "", sender.GetAvatar(), $"Moderator Commands ({mods.Count()}):", ToStr(mods), "", "", "", null, "", "", Helper.GetRandomColor(), false);
                DiscordEmbed masterCommands = ClassBuilder.BuildEmbed(author, "", sender.GetAvatar(), $"Bot Master Commands ({masters.Count()}):", ToStr(masters), "", "", "", null, "", "", Helper.GetRandomColor(), false);
                DiscordEmbed everyoneCommands = ClassBuilder.BuildEmbed(author, "", sender.GetAvatar(), $"Commands ({everyones.Count()}):", ToStr(everyones), "", "", "", null, "", "", Helper.GetRandomColor(), false);
                cmdMsg.Channel.SendMessageAsync(everyoneCommands);
                cmdMsg.Channel.SendMessageAsync(modCommands);
                cmdMsg.Channel.SendMessageAsync(masterCommands);
                return "";
            }
            else
            {
                ICommandHandler handler = CommandManager.GetCommandHandler(args.Combine());
                if (handler == null)
                    return $":x: Missing arguments!\nUsage: {Configs.BotConfig.Prefix}{Usage}";
                else
                {
                    Field command = new Field("Command", handler.Command);
                    Field usage = new Field("Usage", $"{Configs.BotConfig.Prefix}{handler.Usage}");
                    Field description = new Field("Description", handler.Description);
                    Field permission = new Field("Permission", handler.PermType.ToString());
                    cmdMsg.Channel.SendMessageAsync(ClassBuilder.BuildEmbed($"{sender.Username}#{sender.Discriminator}", "", sender.GetAvatar(), "", "", "", "", "", new List<Field>() { command, usage, description, permission }, "", "", Helper.GetRandomColor(), false));
                    return "";
                }
            }
        }

        public static string ToStr(IEnumerable<ICommandHandler> cmds)
        {
            string s = "";
            cmds = cmds.OrderByDescending(h => h.Command).Reverse();
            foreach (ICommandHandler cmd in cmds)
            {
                if (s.Length > 2048 || (s + cmd.Command).Length > 2048)
                    continue;
                if (string.IsNullOrEmpty(s))
                    s += cmd.Command;
                else
                    s += $", {cmd.Command}";
            }
            return s;
        }
    }

    public class CommandAddMod : ICommandHandler
    {
        public string Command => "addmod";
        public string Aliases => "";
        public string Usage => "addmod <role/member>";
        public PermType PermType => PermType.BotMaster;
        public string Description => "Adds a moderator";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            try
            {
                if (args.Length < 1)
                    return $":x: Missing arguments!\nUsage: {Usage}";
                CommandModerators.ObjectType type = CommandModerators.Get(args.Combine(), out DiscordMember user, out DiscordRole role, out DiscordChannel channel);
                if (type == CommandModerators.ObjectType.None)
                    return $":x: Missing arguments!\nUsage: {Usage}";
                if (type == CommandModerators.ObjectType.Role && role != null)
                {
                    Configs.AddModerator(role.Id);
                    return $":white_check_mark: Succesfully added {role.Mention} to moderators.";
                }

                if (type == CommandModerators.ObjectType.User && user != null)
                {
                    Configs.AddModerator(user.Id);
                    return $":white_check_mark: Succesfully added {user.Mention} to moderators.";
                }

                return $":x: Missing arguments!\nUsage: {Usage}";
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return $":x: An error occured: Exception of type **{e.GetType().Name}** was thrown.";
            }
        }
    }

    public class CommandDelMod : ICommandHandler
    {
        public string Command => "delmod";
        public string Aliases => "";
        public string Usage => "delmod <role/member>";
        public PermType PermType => PermType.BotMaster;
        public string Description => "Removes a moderator";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            try
            {
                if (args.Length < 1)
                    return $":x: Missing arguments!\nUsage: {Usage}";
                CommandModerators.ObjectType type = CommandModerators.Get(args.Combine(), out DiscordMember user, out DiscordRole role, out DiscordChannel channel);
                if (type == CommandModerators.ObjectType.None)
                    return $":x: Missing arguments!\nUsage: {Usage}";
                if (type == CommandModerators.ObjectType.Role && role != null)
                {
                    Configs.RemoveModerator(role.Id);
                    return $":white_check_mark: Succesfully removed {role.Mention} from moderators.";
                }

                if (type == CommandModerators.ObjectType.User && user != null)
                {
                    Configs.RemoveModerator(user.Id);
                    return $":white_check_mark: Succesfully removed {user.Mention} from moderators.";
                }

                return $":x: Missing arguments!\nUsage: {Usage}";
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return $":x: An error occured: Exception of type **{e.GetType().Name}** was thrown.";
            }
        }
    }

    public class CommandPrefix : ICommandHandler
    {
        public string Command => "prefix";
        public string Aliases => "";
        public string Usage => $"prefix (new prefix)";
        public PermType PermType => PermType.BotMaster;
        public string Description => "Changes current prefix";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1)
                return $"**Current prefix: `{Configs.BotConfig.Prefix}`**";
            else
            {
                Configs.BotConfig.Prefix = args[0];
                Configs.SaveBotConfig();
                Bot.Client.UpdateStatusAsync(new DiscordActivity
                {
                    ActivityType = ActivityType.ListeningTo,
                    Name = $"{Configs.BotConfig.Prefix}help"
                }); 
                return $":white_check_mark: **Current prefix has been succesfully set to `{Configs.BotConfig.Prefix}`**";
            }
        }
    }

    public class CommandAddMaster : ICommandHandler
    {
        public string Command => "addmaster";
        public string Aliases => "";
        public string Usage => "addmaster <role/member>";
        public PermType PermType => PermType.BotMaster;
        public string Description => "Adds a bot master";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            try
            {
                if (args.Length != 1)
                    return $":x: Missing arguments!\nUsage: {Usage}";
                if (args.Length < 1)
                    return $":x: Missing arguments!\nUsage: {Usage}";
                CommandModerators.ObjectType type = CommandModerators.Get(args.Combine(), out DiscordMember user, out DiscordRole role, out DiscordChannel channel);
                if (type == CommandModerators.ObjectType.None)
                    return $":x: Missing arguments!\nUsage: {Usage}";
                if (type == CommandModerators.ObjectType.Role && role != null)
                {
                    Configs.AddMaster(role.Id);
                    return $":white_check_mark: Succesfully added {role.Mention} to bot masters.";
                }

                if (type == CommandModerators.ObjectType.User && user != null)
                {
                    Configs.AddMaster(user.Id);
                    return $":white_check_mark: Succesfully added {user.Mention} to bot masters.";
                }

                return $":x: Missing arguments!\nUsage: {Usage}";
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return $":x: An error occured: Exception of type **{e.GetType().Name}** was thrown.";
            }
        }
    }

    public class CommandDelMaster : ICommandHandler
    {
        public string Command => "delmaster";
        public string Aliases => "";
        public string Usage => "delmaster <role/member>";
        public PermType PermType => PermType.BotMaster;
        public string Description => "Removes a bot master";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            try
            {
                if (args.Length < 1)
                    return $":x: Missing arguments!\nUsage: {Usage}";
                CommandModerators.ObjectType type = CommandModerators.Get(args.Combine(), out DiscordMember user, out DiscordRole role, out DiscordChannel channel);
                if (type == CommandModerators.ObjectType.None)
                    return $":x: Missing arguments!\nUsage: {Usage}";
                if (type == CommandModerators.ObjectType.Role && role != null)
                {
                    Configs.RemoveMaster(role.Id);
                    return $":white_check_mark: Succesfully removed {role.Mention} from bot masters.";
                }

                if (type == CommandModerators.ObjectType.User && user != null)
                {
                    Configs.RemoveMaster(user.Id);
                    return $":white_check_mark: Succesfully removed {user.Mention} from bot masters.";
                }

                return $":x: Missing arguments!\nUsage: {Usage}";
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return $":x: An error occured: Exception of type **{e.GetType().Name}** was thrown.";
            }
        }
    }

    public class CommandKick : ICommandHandler
    {
        public string Command => "kick";
        public string Aliases => "";
        public string Usage => "kick <member> (optional reason)";
        public PermType PermType => PermType.Moderator;
        public string Description => "Kicks a member from this guild";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1)
                return $":x: Missing arguments!\nUsage: {Usage}";
            try
            {
                DiscordUser user = Bot.Client.GetUserAsync(args[0].GetUser().Id).GetAwaiter().GetResult();
                if (user == null)
                    return ":x: **Cannot find that user!**";
                if (user.Id == sender.Id)
                    return ":x: Why are you trying to kick yourself?";
                string reason = "";
                if (args.Length > 1)
                    reason = string.Join(' ', args.Skip(1));
                if (string.IsNullOrEmpty(reason))
                    reason = "No reason provided.";
                DiscordGuild guild = cmdMsg.Channel.Id.GetServer();
                args[0].GetUser().SendMessageAsync(ClassBuilder.BuildEmbed($"{sender.Username}#{sender.Discriminator}", "", sender.GetAvatar(), "", $":warning: You were kicked from **{guild.Name}** by {sender.Mention}!", "", "", "", new List<Field>() { new Field("Reason", reason) }, "", "", Helper.GetRandomColor(), false));
                args[0].GetUser().RemoveAsync($"Kicked by {cmdMsg.Author.Username}#{cmdMsg.Author.Discriminator} for {reason}");
                return $":white_check_mark: Kicked {user.Mention} || **{reason}**";
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return $":x: An error occured: Exception of type **{e.GetType().Name}** was thrown.";
            }
        }
    }

    public class CommandStatus : ICommandHandler
    {
        public string Command => "setstatus";
        public string Aliases => "";
        public string Usage => "setstatus <new status>";
        public PermType PermType => PermType.BotMaster;
        public string Description => "Sets a new bot status";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1)
                return $"{Usage}**";
            string newStatus = args.Combine();
            Bot.Client.UpdateStatusAsync(new DiscordActivity
            {
                ActivityType = ActivityType.ListeningTo,
                Name = newStatus
            }, UserStatus.Idle);
            return $":white_check_mark: **Bot Status has been succesfully set to `{newStatus}`**";
        }
    }

    public class CommandSeek : ICommandHandler
    {
        public string Command => "seek";
        public string Aliases => "";
        public string Usage => "seek <position>";
        public PermType PermType => PermType.Everyone;
        public string Description => "Skips to a specified position in the currently playing song.";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1)
                return $":x: Missing arguments!\nUsage: {Usage}";
            if (!int.TryParse(args[0], out int position))
                return $":x: Missing arguments!\nUsage: {Usage}";
            if (!Helper.CanExecute(sender))
                return ":x: **You have to be in the same voice channel to use this command!**";
            Bot.MusicHandler.Seek(cmdMsg, position);
            return "";
        }
    }

    public class CommandVolume : ICommandHandler
    {
        public string Command => "volume";
        public string Aliases => "vol setvolume";
        public string Usage => "volume <volume>";
        public PermType PermType => PermType.Everyone;
        public string Description => "Sets the volume";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1 || !int.TryParse(args[0], out int volume))
                return $":x: Missing arguments!\nUsage: {Usage}";
            if (!Helper.CanExecute(sender))
                return ":x: **You have to be in the same voice channel to use this command!**";
            Bot.MusicHandler.SetVolume(cmdMsg,volume);
            return "";
        }
    }

    public class CommandPause : ICommandHandler
    {
        public string Command => "pause";
        public string Aliases => "pa";
        public string Usage => Command;
        public PermType PermType => PermType.Everyone;
        public string Description => "Pauses the current song";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (!Helper.CanExecute(sender))
                return ":x: **You have to be in the same voice channel to use this command!**";
            Bot.MusicHandler.Pause(cmdMsg);
            return "";
        }
    }

    public class CommandResume : ICommandHandler
    {
        public string Command => "resume";
        public string Aliases => "res";
        public string Usage => Command;
        public PermType PermType => PermType.Everyone;
        public string Description => "Resumes playing the current song.";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (!Helper.CanExecute(sender))
                return ":x: **You have to be in the same voice channel to use this command!**";
            Bot.MusicHandler.Resume(cmdMsg);
            return "";
        }
    }

    public class CommandNowPlaying : ICommandHandler
    {
        public string Command => "nowplaying";
        public string Aliases => "np";
        public string Usage => Command;
        public PermType PermType => PermType.Everyone;
        public string Description => "Displays the current song.";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            Bot.MusicHandler.NowPlaying(cmdMsg);
            return "";
        }
    }

    public class CommandClear : ICommandHandler
    {
        public string Command => "clear";
        public string Aliases => "c";
        public string Usage => Command;
        public PermType PermType => PermType.Everyone;
        public string Description => "Clears the queue.";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (!Helper.CanExecute(sender))
                return ":x: **You have to be in the same voice channel to use this command!**";
            Bot.MusicHandler.Clear(cmdMsg);
            return "";
        }
    }

    public class CommandStop : ICommandHandler
    {
        public string Command => "stop";
        public string Aliases => "st";
        public string Usage => Command;
        public PermType PermType => PermType.Everyone;
        public string Description => "Stops playing the current song.";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (!Helper.CanExecute(sender))
                return ":x: **You have to be in the same voice channel to use this command!**";
            Bot.MusicHandler.Stop(cmdMsg);
            return "";
        }
    }

    public class CommandLeave : ICommandHandler
    {
        public string Command => "leave";
        public string Aliases => "disconnect dis";
        public string Usage => "disconnect";
        public PermType PermType => PermType.Everyone;
        public string Description => "Leaves the current voice channel.";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (!Helper.CanExecute(sender))
                return ":x: **You have to be in the same voice channel to use this command!**";
            Helper.Disconnect();
            return ":white_check_mark: Goodbye!";
        }
    }

    public class CommandQueue : ICommandHandler
    {
        public string Command => "queue";
        public string Aliases => "q";
        public string Usage => "queue";
        public PermType PermType => PermType.Everyone;
        public string Description => "Displays the current queue";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            Bot.MusicHandler.QueueTask(cmdMsg);
            return "";
        }
    }

    public class CommandSkip : ICommandHandler
    {
        public string Command => "skip";
        public string Aliases => "s";
        public string Usage => Command;
        public PermType PermType => PermType.Everyone;
        public string Description => "Skips to the next song in the queue.";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (!Helper.CanExecute(sender))
                return ":x: **You have to be in the same voice channel to use this command!**";
            Bot.MusicHandler.SkipTask(cmdMsg);
            return "";
        }
    }

    public class CommandPlay : ICommandHandler
    {
        public string Command => "play";
        public string Aliases => "p";
        public string Usage => "play <url/search>";
        public PermType PermType => PermType.Everyone;
        public string Description => "Plays a song from the specified URL/query";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            Bot.MusicHandler.PlayTask(cmdMsg, args.Combine());
            return "";
        }
    }

    public class CommandJoin : ICommandHandler
    {
        public string Command => "join";
        public string Aliases => "j";
        public string Usage => "join";
        public PermType PermType => PermType.Everyone;
        public string Description => "Joins to your current voice channel";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            DiscordChannel voice = sender.GetCurrentVoice();
            if (voice == null)
                return ":x: **You have to be in a voice channel to use this command!**";
            Helper.Join(voice, Helper.ConnectionType.Lavalink).GetAwaiter().GetResult();
            return $":white_check_mark: Joined **{voice.Name}**";
        }
    }

    public class CommandRemove : ICommandHandler
    {
        public string Command => "remove";
        public string Aliases => "r";
        public string Usage => "remove <index>";
        public PermType PermType => PermType.Everyone;
        public string Description => "Removes a song at a specified position";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1)
                return $":x: Missing arguments!\nUsage: {Usage}";
            if (!int.TryParse(args[0], out int index))
                return $":x: Missing arguments!\nUsage: {Usage}";
            if (!Helper.CanExecute(sender))
                return ":x: **You have to be in the same voice channel to use this command!**";
            return Queue.RemoveTrack(cmdMsg.Channel.Guild.Id, index);
        }
    }

    public class CommandDownload : ICommandHandler
    {
        public string Command => "download";
        public string Aliases => "d";
        public string Usage => "download <search>";
        public PermType PermType => PermType.Everyone;
        public string Description => "Posts a downloadable MP3 file of a specified YouTube video URL/query";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1)
                return $":x: Missing arguments!\nUsage: {Usage}";
            string search = string.Join(' ', args);
            YouTubeVideo video = Downloader.GetVideo(search);
            if (video == null)
                return ":x: **The search found no such tracks**";
            Downloader.Result res = Downloader.Download(video);
            if (res == null)
                return ":x: **An error occured while downloading.**";
            try
            {
                Downloader.Upload(cmdMsg.Channel, res);
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return $"```csharp\n{e.ToString()}\n```";
            }
            System.IO.File.Delete(res.Path);
            System.IO.File.Delete(res.ResultPath);
            return "";
        }
    }

    public class CommandMute : ICommandHandler
    {
        public string Command => "mute";
        public string Aliases => "";
        public string Usage => "mute <member> (duration) (reason)";
        public PermType PermType => PermType.Moderator;
        public string Description => "Mutes a member";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1)
                return $":x: Missing arguments!\nUsage: {Usage}";
            ulong duration = 0;
            duration = Timer.ParseDuration(args[1]);
            Timer.Time timeData = Timer.Time.Hour;
            Timer.TryParse(args[1], out timeData);
            bool permanent = duration == 0;
            string reason = "";
            if (args.Length > 1)
                reason = string.Join(' ', args.Skip(2));
            if (string.IsNullOrEmpty(reason))
                reason = "No reason provided.";
            DiscordMember user = args[0].GetUser();
            if (user == null)
                return $":x: Missing arguments!\nUsage: {Usage}";
            if (user.Id == sender.Id)
                return ":x: Why are you even trying to mute yourself?";
            DiscordGuild guild = cmdMsg.Channel.Guild;
            MuteHandler.Mute(cmdMsg, guild, user, sender, duration, reason, timeData, permanent);
            return "";
        }
    }

    public class CommandUnmute : ICommandHandler
    {
        public string Command => "unmute";
        public string Aliases => "";
        public string Usage => "unmute <member>";
        public PermType PermType => PermType.Moderator;
        public string Description => "Unmutes a member";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1)
                return $":x: Missing arguments!\nUsage: {Usage}";
            DiscordMember user = args[0].GetUser();
            if (user == null)
                return ":x: **Cannot find that user!**";
            if (user.Id == sender.Id)
                return ":x: What are you trying?";
            MuteHandler.Unmute(cmdMsg, cmdMsg.Channel.Id.GetServer(), user);
            return "";
        }
    }

    public class CommandBan : ICommandHandler
    {
        public string Command => "ban";
        public string Aliases => "";
        public string Usage => "ban <member> (duration) (reason)";
        public PermType PermType => PermType.Moderator;
        public string Description => "Bans a member from this guild.";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1)
                return $":x: Missing arguments!\nUsage: {Usage}";
            ulong duration = 0;
            try
            {
                duration = Timer.ParseDuration(args[1]);
            }
            catch (Exception)
            {
                duration = 0;
            }
            Timer.Time timeData = Timer.Time.Hour;
            Timer.TryParse(args[1], out timeData);
            bool permanent = duration == 0 || args.Length == 1;
            string reason = "";
            if (args.Length > 1)
                reason = string.Join(' ', args.Skip(2));
            if (string.IsNullOrEmpty(reason))
                reason = "No reason provided.";
            DiscordMember user = args[0].GetUser();
            if (user == null)
                return ":x: **Cannot find that user!**";
            if (user.Id == sender.Id)
                return ":x: Unfortunately I cannot allow self-harm.";
            DiscordGuild guild = cmdMsg.Channel.Id.GetServer();
            return BanHandler.Ban(cmdMsg, guild, user, sender, duration, reason, timeData, permanent);
        }
    }

    public class CommandUnban : ICommandHandler
    {
        public string Command => "unban";
        public string Aliases => "";
        public string Usage => "unban <member>**";
        public PermType PermType => PermType.Moderator;
        public string Description => "Unbans a member in this guild.";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1)
                return $":x: Missing arguments!\nUsage: {Usage}";
            return BanHandler.Unban(args.Combine(), cmdMsg.Channel.Id.GetServer(), cmdMsg);
        }
    }

    public class CommandReload : ICommandHandler
    {
        public string Command => "reload";
        public string Aliases => "";
        public string Usage => Command;
        public PermType PermType => PermType.BotMaster;
        public string Description => "Reloads the bot's configuration files";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            Configs.Reload();
            CommandLockdown.IgnoredChannels.Clear();
            return $":white_check_mark: **Succesfully reloaded.**";
        }
    }

    public class CommandPurge : ICommandHandler
    {
        public string Command => "purge";
        public string Aliases => "";
        public string Usage => "purge <count>";
        public PermType PermType => PermType.Moderator;
        public string Description => "Deletes a specified amount of messages in this channel";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1)
                return $":x: Missing arguments!\nUsage: {Usage}";
            if (!int.TryParse(args[0], out int count))
                return $":x: Missing arguments!\nUsage: {Usage}";
            cmdMsg.DeleteAsync();
            Helper.DeleteMessages(cmdMsg.Channel, sender, count);
            return "";
        }
    }

    public class CommandLock : ICommandHandler
    {
        public string Command => "lock";
        public string Aliases => "";
        public string Usage => "lock (channel)";
        public PermType PermType => PermType.Moderator;
        public string Description => "Locks the specified channel";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            DiscordChannel channel = args.Length < 1 ? cmdMsg.Channel : args.Combine().GetTextChannel();
            Helper.ChangeLock(channel, true);
            return $":lock: Locked {channel.Mention}";
        }
    }

    public class CommandUnlock : ICommandHandler
    {
        public string Command => "unlock";
        public string Aliases => "";
        public string Usage => $"unlock (channel)";
        public PermType PermType => PermType.Moderator;
        public string Description => $"Unlocks a channel.";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            DiscordChannel channel = args.Length < 1 ? cmdMsg.Channel : args.Combine().GetTextChannel();
            Helper.ChangeLock(channel, false);
            return $":key: Unlocked {channel.Mention}";
        }
    }

    public class CommandWarn : ICommandHandler
    {
        public string Command => "warn";
        public string Aliases => "";
        public string Usage => "warn <member> (reason)";
        public PermType PermType => PermType.Moderator;
        public string Description => "Warns a member for breaking the rules.";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 2)
                return $":x: Missing arguments!\nUsage: {Usage}";
            DiscordMember user = args[0].GetUser();
            if (user == null)
                return ":x: **User not found**";
            if (user.Id == sender.Id)
                return ":x: lol nope";
            string reason = args.Skip(1).ToArray().Combine();
            if (string.IsNullOrEmpty(reason))
                reason = "No reason provided.";
            user.SendMessageAsync(ClassBuilder.BuildEmbed($"{sender.Username}#{sender.Discriminator}", "", sender.GetAvatar(), "", $":warning: You were warned in **{sender.Guild.Name}**!", "", "", "", new List<Field>() { new Field("Reason", reason) }, "", "", Helper.GetRandomColor(), false));
            WarnHandler.Warn(user, sender, reason);
            return $":white_check_mark: {user.Mention} was warned. || **{reason}**";
        }
    }

    public class CommandSay : ICommandHandler
    {
        public string Command => "say";
        public string Aliases => "";
        public string Usage => "say (channel) <what do you want the bot to say>";
        public PermType PermType => PermType.Moderator;
        public string Description => "Posts a message in the specified channel";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1)
                return $":x: Missing arguments!\nUsage: {Usage}";
            DiscordChannel channel = args[0].GetTextChannel();
            string str = "";
            bool found = channel != null;
            if (channel == null)
                channel = cmdMsg.Channel.Id.ToString().GetTextChannel();
            if (found)
            {
                str = args.Skip(1).ToArray().Combine();
                channel.SendMessageAsync(str);
            }
            else
            {
                str = args.Combine();
                cmdMsg.Channel.SendMessageAsync(str);
            }
            cmdMsg.DeleteAsync();
            return "";
        }
    }

    public class CommandEmbed : ICommandHandler
    {
        public string Command => "embed";
        public string Aliases => "";
        public string Usage => "embed help";
        public PermType PermType => PermType.Moderator;
        public string Description => "Posts a embed message.";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1)
                return $":x: Missing arguments!\nUsage: {Usage}";
            if (args[0] == "help")
            {
                return $"**__Embed - Help__**\n**Syntax**: `{Configs.BotConfig.Prefix}embed <variable>:<value>\\n<anotherVariable>:<value>`\n\n**__Example - setting the title and the description__**:\n`{Configs.BotConfig.Prefix}embed title:This is a test title\\ndescription:This is a test description`\n**Use `{Configs.BotConfig.Prefix}embed variables` to get a list of all variables**.";
            }

            if (args[0] == "variables")
            {
                return $"**__Embed - Variables__**\nauthor [string]\nauthorUrl [string]\nauthorIconUrl [string]\ntitle [string]\ndescription [string]\ntitleUrl [string]\nthumbnailUrl [string]\nimageUrl [string]\nfooter [string]\nfooterIconUrl [string]\ncolor [string]\ntimestamp [boolean]\nchannel [channel ID/mention] - Sets the channel to post the embed in\nping [role ID/mention] - Sets the role to ping";
            }

            string author = "none";
            string authorUrl = "none";
            string authorIconUrl = "none";
            string title = "none";
            string desc = "none";
            string titleUrl = "none";
            string thumbnailUrl = "none";
            string imageUrl = "none";
            string footer = "none";
            string footerIconUrl = "none";
            DiscordColor color = Helper.GetRandomColor();
            bool time = false;
            List<Field> fields = new List<Field>();
            string channel = "none";
            string ping = "none";
            DiscordGuild guild = cmdMsg.Channel.Id.GetServer();
            try
            {
                foreach (string arg in args.Combine().Split("\\n"))
                {
                    if (arg.StartsWith("author:"))
                        author = arg.Replace("author:", "");
                    if (arg.StartsWith("authorUrl:"))
                        authorUrl = arg.Replace("authorUrl:", "");
                    if (arg.StartsWith("authorIconUrl:"))
                        authorIconUrl = arg.Replace("authorIconUrl:", "");
                    if (arg.StartsWith("title:"))
                        title = arg.Replace("title:", "");
                    if (arg.StartsWith("description:"))
                        desc = arg.Replace("description:", "");
                    if (arg.StartsWith("titleUrl:"))
                        titleUrl = arg.Replace("titleUrl:", "");
                    if (arg.StartsWith("thumbnailUrl:"))
                        thumbnailUrl = arg.Replace("thumbnailUrl:", "");
                    if (arg.StartsWith("imageUrl:"))
                        imageUrl = arg.Replace("imageUrl:", "");
                    if (arg.StartsWith("footer:"))
                        footer = arg.Replace("footer:", "");
                    if (arg.StartsWith("footerIconUrl:"))
                        footerIconUrl = arg.Replace("footerIconUrl:", "");
                    if (arg.StartsWith("color:"))
                        color = arg.Replace("color:", "").GetColor();
                    if (arg.StartsWith("timestamp:"))
                    {
                        string e = arg.Replace("timestamp:", "");
                        if (!bool.TryParse(e, out time))
                            time = false;
                    }
                    if (arg.StartsWith("channel:"))
                        channel = arg.Replace("channel:", "");
                    if (arg.StartsWith("ping:"))
                        ping = arg.Replace("ping:", "");
                }

                DiscordEmbed embed = ClassBuilder.BuildEmbed(author == "sender" ? $"{sender.Username}#{sender.Discriminator}" : author, authorUrl, authorIconUrl == "sender" ? sender.GetAvatar() : authorIconUrl, title, desc, titleUrl, thumbnailUrl, imageUrl, fields, footer, footerIconUrl, color, time);
                DiscordChannel channelE = channel.GetTextChannel();

                DiscordRole role = ping.GetRole();
                if (channel != "none" && channelE != null)
                {
                    if (ping != "none")
                    {
                        string mention = "";
                        if (ping == "everyone")
                            mention = guild.EveryoneRole.Mention;
                        if (ping == "here")
                            mention = "@here";
                        if (role != null)
                            mention = role.Mention;
                        if (!string.IsNullOrEmpty(mention))
                            channelE.SendMessageAsync(mention);
                    }
                    channelE.SendMessageAsync(embed);
                }
                else
                {
                    if (ping != "none")
                    {
                        string mention = "";
                        if (ping == "everyone")
                            mention = guild.EveryoneRole.Mention;
                        if (ping == "here")
                            mention = "@here";
                        if (role != null)
                            mention = role.Mention;
                        if (!string.IsNullOrEmpty(mention))
                            cmdMsg.Channel.SendMessageAsync(mention);
                    }
                    cmdMsg.Channel.SendMessageAsync(embed);
                }

                cmdMsg.DeleteAsync();
                return "";
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return $"```yaml\n{e}\n```";
            }
        }
    }

    public class CommandAvatar : ICommandHandler
    {
        public string Command => "avatar";
        public string Aliases => "av";
        public string Usage => $"avatar (user)";
        public PermType PermType => PermType.Everyone;
        public string Description => "Returns user's avatar.";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1)
            {
                cmdMsg.Channel.SendMessageAsync(sender.AvatarUrl);
                return "";
            }
            else
            {
                DiscordMember user = args[0].GetUser();
                if (user != null)
                {
                    cmdMsg.Channel.SendMessageAsync(user.AvatarUrl);
                    return "";
                }
            }
            return "";
        }
    }

    public class CommandBotInfo : ICommandHandler
    {
        public string Command => "botinfo";
        public string Aliases => "";
        public string Usage => Command;
        public PermType PermType => PermType.Everyone;
        public string Description => "Returns information about this bot.";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            cmdMsg.Channel.SendMessageAsync(Extensions.GetBotInfo(Bot.Client));
            return "";
        }
    }

    public class CommandUserInfo : ICommandHandler
    {
        public string Command => "userinfo";
        public string Aliases => "";
        public string Usage => $"userinfo (user)";
        public PermType PermType => PermType.Everyone;
        public string Description => "Returns information about a user";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            try
            {
                DiscordMember user = null;
                if (args.Length < 1)
                    user = sender;
                else
                    user = args[0].GetUser();
                if (user != null)
                {
                    cmdMsg.Channel.SendMessageAsync(user.GetInfo());
                }
                return "";
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return $":x: An error occured: Exception of type **{e.GetType().Name}** was thrown.";
            }
        }
    }

    public class CommandServerInfo : ICommandHandler
    {
        public string Command => "serverinfo";
        public string Aliases => "";
        public string Usage => Command;
        public PermType PermType => PermType.Everyone;
        public string Description => $"Returns information about this guild";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            try
            {
                DiscordGuild guild = cmdMsg.Channel.Id.GetServer();
                if (guild == null)
                    return "";
                cmdMsg.Channel.SendMessageAsync(guild.GetInfo());
                return "";
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return $":x: An error occured: Exception of type **{e.GetType().Name}** was thrown.";
            }
        }
    }

    public class CommandAddRank : ICommandHandler
    {
        public string Command => "addrank";
        public string Aliases => "";
        public string Usage => "addrank <role>";
        public PermType PermType => PermType.Moderator;
        public string Description => "Adds a role to roles that will be assigned to a new user.";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1)
                return $":x: Missing arguments!\nUsage: {Usage}";
            DiscordRole role = args.Combine().GetRole();
            if (role != null)
            {
                RoleManager.AddRoleAfterJoin(role);
                return $":white_check_mark: Added **{role.Name}** to roles assigned after joining.";
            }
            return $":x: Missing arguments!\nUsage: {Usage}";
        }
    }

    public class CommandRemoveRank : ICommandHandler
    {
        public string Command => "removerank";
        public string Aliases => "";
        public string Usage => "removerank <role>";
        public PermType PermType => PermType.Moderator;
        public string Description => "Removes a role from roles that will be assigned to new users.";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1)
                return $":x: Missing arguments!\nUsage: {Usage}";
            DiscordRole role = args.Combine().GetRole();
            if (role != null)
            {
                RoleManager.RemoveRoleAfterJoin(role);
                return $":white_check_mark: Removed **{role.Name}** from roles assigned after joining.";
            }
            return $":x: Missing arguments!\nUsage: {Usage}";
        }
    }

    public class CommandModifyRent : ICommandHandler
    {
        public string Command => "modifyrent";
        public string Aliases => "";
        public string Usage => "modifyrent <key> <value>";
        public PermType PermType => PermType.BotMaster;
        public string Description => "Sets up the rent system for this guid.";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 2)
                return $":x: Missing arguments!\nUsage: {Usage}";
            DiscordGuild guild = cmdMsg.Channel.Id.GetServer();
            if (guild == null)
                return $":x: Missing arguments!\nUsage: {Usage}";
            RentSettings curr = VoiceManager.RentSettings;
            if (curr == null)
            {
                VoiceManager.RentSettings = new RentSettings();
                VoiceManager.RentSettings.Bitrate = 8000;
                VoiceManager.RentSettings.CategoryId = 0;
                VoiceManager.RentSettings.Voice = 0;
                VoiceManager.Save();
                curr = VoiceManager.RentSettings;
            }

            if (args[0] == "categoryId")
            {
                if (!ulong.TryParse(args[1], out ulong id))
                    return $":x: Missing arguments!\nUsage: {Usage}";
                curr.CategoryId = id;
                VoiceManager.ModifySettings(curr);
                return $":white_check_mark: Succesfully modified `categoryId`. New value: **{id}**";
            }

            if (args[0] == "bitrate")
            {
                if (!int.TryParse(args[1], out int bitrate))
                    return $":x: Missing arguments!\nUsage: {Usage}";
                curr.Bitrate = bitrate;
                VoiceManager.ModifySettings(curr);
                return $":white_check_mark: Succesfully modified `bitrate`. New value: **{bitrate}**";
            }

            if (args[0] == "rentVoice")
            {
                if (!ulong.TryParse(args[1], out ulong id))
                    return $":x: Missing arguments!\nUsage: {Usage}";
                DiscordChannel channel = id.ToString().GetVoice();
                if (channel == null)
                    return $":x: Missing arguments!\nUsage: {Usage}";
                VoiceManager.SetVoice(channel.Id);
                return $":white_check_mark: RentSystem's voice channel on this server has been set to **{channel.Name}**";
            }
            return $":x: Missing arguments!\nUsage: {Usage}";
        }
    }

    public class CommandRent : ICommandHandler
    {
        public string Command => "rent";
        public string Aliases => "";
        public string Usage => "rent <user limit> <voice channel name>";
        public PermType PermType => PermType.Everyone;
        public string Description => "Rents a voice channel.";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1)
                return $":x: Missing arguments!\nUsage: {Usage}";
            if (!int.TryParse(args[0], out int limit))
                return $":x: Missing arguments!\nUsage: {Usage}";
            string name = args.Skip(1).ToArray().Combine();
            if (string.IsNullOrEmpty(name))
                return $":x: Missing arguments!\nUsage: {Usage}";
            DiscordChannel current = sender.GetCurrentVoice();
            if (current == null)
                return $":x: You have to be in a voice channel to use this command!";
            DiscordGuild guild = cmdMsg.Channel.Id.GetServer();
            ulong rentId = VoiceManager.RentSettings.Voice;
            if (rentId == 0)
                return $":x: **RentSystem is not setup in this server.**";
            DiscordChannel rent = rentId.ToString().GetVoice();
            if (rent == null)
                return $":x: **RentSystem is not setup in this server.**";
            if (current.Id != rent.Id)
                return $":x: You have to be in **{rent.Name}** to use this command!";
            VoiceManager.CreateVoice(guild, sender, name, limit);
            return $":white_check_mark: Succesfully created **{name}**";
        }
    }

    public class CommandLockdown : ICommandHandler
    {
        public string Command => "lockdown";
        public string Aliases => "";
        public string Usage => Command;
        public PermType PermType => PermType.Moderator;
        public string Description => "Locks all channels.";

        public static List<ulong> IgnoredChannels = new List<ulong>();

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            DiscordGuild guild = cmdMsg.Channel.Id.GetServer();

            if (args.Length > 0 && args[0].ToLower() == "end")
            {
                foreach (DiscordChannel ch in guild.Channels.Where(h => h.Value.Type == ChannelType.Text).Select(h => h.Value))
                {
                    Helper.ChangeLock(ch, false);
                }
                return $":white_check_mark: Server lockdown has been disabled.";
            }
            else
            {
                foreach (DiscordChannel ch in guild.Channels.Where(h => h.Value.Type == ChannelType.Text).Select(h => h.Value))
                {
                    Helper.ChangeLock(ch, true);
                }

                return $":white_check_mark: Server lockdown has been enabled.";
            }
        }
    }

    public class CommandRandomJoke : ICommandHandler
    {
        public string Command => "joke";
        public string Aliases => "";
        public string Usage => Command;
        public PermType PermType => PermType.Everyone;
        public string Description => "Returns a random joke";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            try
            {
                RandomJoke joke = API.Client.RequestJoke();
                return $"{joke.Setup}\n{joke.Punchline}";
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return $":x: An error occured: Exception of type **{e.GetType().Name}** was thrown.";
            }
        }
    }

    public class CommandLevels : ICommandHandler
    {
        public string Command => "levels";
        public string Aliases => "";
        public string Usage => Command;
        public PermType PermType => PermType.Everyone;
        public string Description => "Returns a Top10 leaderboard for this server";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            DiscordEmbed e = LevelManager.GetLeaderboard(sender);
            if (e == null)
                return $":x: The LevelSystem module is disabled on this server!";
            cmdMsg.Channel.SendMessageAsync(e);
            return "";
        }
    }

    public class CommandRank : ICommandHandler
    {
        public string Command => "rank";
        public string Aliases => "";
        public string Usage => "rank (user)";
        public PermType PermType => PermType.Everyone;
        public string Description => "Returns user's rank and experience.";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length > 0)
            {
                DiscordMember usr = args[0].GetUser();
                if (usr == null)
                {
                    DiscordEmbed e = LevelManager.GetRankCommand(sender);
                    if (e == null)
                        return ":x: The LevelSystem module is disabled on this server!";
                    else
                        cmdMsg.Channel.SendMessageAsync(e);
                    return "";
                }
                else
                {
                    DiscordEmbed e = LevelManager.GetRankCommand(usr);
                    if (e == null)
                        return ":x: The LevelSystem module is disabled on this server!";
                    else
                        cmdMsg.Channel.SendMessageAsync(e);
                    return "";
                }
            }
            else
            {
                DiscordEmbed e = LevelManager.GetRankCommand(sender);
                if (e == null)
                    return ":x: The LevelSystem module is disabled on this server!";
                else
                    cmdMsg.Channel.SendMessageAsync(e);
                return "";
            }
        }
    }

    public class CommandLevelManager : ICommandHandler
    {
        public string Command => "levelmanager";
        public string Aliases => "";
        public string Usage => "levelmanager help";
        public PermType PermType => PermType.BotMaster;
        public string Description => "Sets up the level module in this guild.";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1)
                return $":x: Missing arguments!\nUsage: {Usage}";
            if (args[0] == "help")
            {
                return $"**__LevelManager__**\n\nCommands are prefixed with `{Configs.BotConfig.Prefix}levelmanager`\n**Valid subcommands**: enable, disable, setlevel, setexp, setannouncements, modifyexp, ignore, multiplier, addrole, reset, save";
            }
            else
            {
                if (args[0] == "enable")
                {
                    LevelManager.Settings.Enabled = true;
                    LevelManager.Save();
                    return $":white_check_mark: LevelManager has been enabled on this server.";
                }

                if (args[0] == "disable")
                {
                    LevelManager.Settings.Enabled = false;
                    LevelManager.Save();
                    return $":white_check_mark: LevelManager has been disabled on this server.";
                }

                if (args[0] == "setlevel")
                {
                    DiscordMember usr = args[1].GetUser();
                    int level = 0;
                    if (!int.TryParse(args[2], out level))
                        return $":x: Invalid usage!\nUsage: {Configs.BotConfig.Prefix}levelmanager setlevel <user> <level>";
                    LevelManager.SetLvl(usr, level);
                    LevelManager.Save();
                    return $":white_check_mark: Level of {usr.Mention} has been set to **{level}**.";
                }

                if (args[0] == "setexp")
                {
                    DiscordMember usr = args[1].GetUser();
                    ulong exp = 0;
                    if (!ulong.TryParse(args[2], out exp))
                        return $":x: Invalid usage!\nUsage: {Configs.BotConfig.Prefix}levelmanager setxp <user> <exp>";
                    LevelManager.Experience.TryAdd(usr.Id, 0);
                    LevelManager.Experience[usr.Id] = exp;
                    LevelManager.Save();
                    return $":white_check_mark: Experience of {usr.Mention} has been set to **{exp}**.";
                }

                if (args[0] == "setannouncements")
                {
                    DiscordChannel channel = args[1].GetTextChannel();
                    if (channel == null)
                        return $":x: Invalid usage!\nUsage: {Configs.BotConfig.Prefix}levelmanager setannouncements <channel>";
                    LevelManager.Settings.Channel = channel.Id;
                    LevelManager.Save();
                    return $":white_check_mark: Announcement channel has been set to {channel.Mention}";
                }

                if (args[0] == "modifyexp")
                {
                    if (args.Length < 3)
                        return $"This command modifies the amount of exp needed to pass a specified level.\nUsage: {Configs.BotConfig.Prefix}levelmanager modifyexp <level (max: 1000)> <newExp>";
                    if (!int.TryParse(args[1], out int lvl))
                        return $"This command modifies the amount of exp needed to pass a specified level.\nUsage: {Configs.BotConfig.Prefix}levelmanager modifyexp <level (max: 1000)> <newExp>";
                    if (!ulong.TryParse(args[2], out ulong exp))
                        return $"This command modifies the amount of exp needed to pass a specified level.\nUsage: {Configs.BotConfig.Prefix}levelmanager modifyexp <level (max: 1000)> <newExp>";
                    if (lvl > 1000)
                        return $"This command modifies the amount of exp needed to pass a specified level.\nUsage: {Configs.BotConfig.Prefix}levelmanager modifyexp <level (max: 1000)> <newExp>";
                    LevelManager.LevelExps[lvl] = exp;
                    return $":white_check_mark: The amount of EXP needed to pass level **{lvl}** has been set to **{exp}**";
                }

                if (args[0] == "ignore")
                {
                    if (args.Length < 2)
                        return $"This command adds a ignored ID of a user/channel.\nUsage: {Configs.BotConfig.Prefix}levelmanager ignore <ID>";
                    if (!ulong.TryParse(args[1], out ulong id))
                        return $"This command adds a ignored ID of a user/channel.\nUsage: {Configs.BotConfig.Prefix}levelmanager ignore <ID>";
                    LevelManager.IgnoredIDs.Add(id);
                    LevelManager.Save();
                    return $":white_check_mark: Added {id} to list of ignored IDs.";
                }

                if (args[0] == "multiplier")
                {
                    if (args.Length < 2)
                        return $"This command modifies the EXP multiplier.\nCurrent: **{LevelManager.Settings.Multiplier}**";
                    if (!double.TryParse(args[1], out double multiplier))
                        return $"This command modifies the EXP multiplier.\nCurrent: **{LevelManager.Settings.Multiplier}**";
                    LevelManager.Settings.Multiplier = multiplier;
                    LevelManager.Save();
                    return $":white_check_mark: EXP multiplier has been set to **{multiplier}**";
                }

                if (args[0] == "addrole")
                {
                    if (args.Length < 3)
                        return $"This command adds a role that will be given when a user reaches a specified level.\nUsage: {Configs.BotConfig.Prefix}levelmanager addrole <level (max: 1000)> <role>";
                    if (!int.TryParse(args[1], out int lvl))
                        return $"This command adds a role that will be given when a user reaches a specified level.\nUsage: {Configs.BotConfig.Prefix}levelmanager addrole <level (max: 1000)> <role>";
                    DiscordRole role = args[2].GetRole();
                    if (role == null)
                        return $"This command adds a role that will be given when a user reaches a specified level.\nUsage: {Configs.BotConfig.Prefix}levelmanager addrole <level (max: 1000)> <role>";
                    if (lvl > 1000)
                        return $"This command adds a role that will be given when a user reaches a specified level.\nUsage: {Configs.BotConfig.Prefix}levelmanager addrole <level (max: 1000)> <role>";
                    LevelManager.LevelRoles.TryAdd(lvl, new List<ulong>());
                    LevelManager.LevelRoles[lvl].Add(role.Id);
                    LevelManager.Save();
                    return $":white_check_mark: {role.Mention} will be given to users that reach level **{lvl}**";
                }

                if (args[0] == "reset")
                {
                    LevelManager.Levels.Clear();
                    LevelManager.Experience.Clear();
                    LevelManager.Save();
                    return $":white_check_mark: Levels have been succesfully reset.";
                }

                if (args[0] == "save")
                {
                    LevelManager.Save();
                    return $":white_check_mark: Succesfully saved current settings.";
                }

                return $":x: Missing arguments!\nUsage: {Usage}";
            }
        }
    }

    public class CommandRoleInfo : ICommandHandler
    {
        public string Command => "roleinfo";
        public string Aliases => "";
        public string Usage => "roleinfo <role>";
        public PermType PermType => PermType.Everyone;
        public string Description => "Returns infomation about a role";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1)
                return $":x: Missing arguments!\nUsage: {Usage}";
            DiscordRole role = args.Combine().GetRole();
            if (role == null)
                return $":x: Missing arguments!\nUsage: {Usage}";
            cmdMsg.Channel.SendMessageAsync(role.GetInfo());
            return "";
        }
    }

    public class CommandChannelInfo : ICommandHandler
    {
        public string Command => "channelinfo";
        public string Aliases => "";
        public string Usage => "channelinfo (channel)";
        public PermType PermType => PermType.Everyone;
        public string Description => "Returns information about a channel";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            DiscordChannel socketTextChannel = args.Length < 1 ? cmdMsg.Channel.Id.ToString().GetTextChannel() : args.Combine().GetTextChannel();
            cmdMsg.Channel.SendMessageAsync(socketTextChannel.GetInfo());
            return "";
        }
    }

    public class CommandMeme : ICommandHandler
    {
        public string Command => "meme (subreddit)";
        public string Aliases => "";
        public string Usage => Command;
        public PermType PermType => PermType.Everyone;
        public string Description => "Returns a random ~~shitpost~~ meme";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            MemePost post = API.Client.RequestMeme(args.Length < 1 ? Configs.BotConfig.Subreddit : args[0]);
            DiscordEmbed e = ClassBuilder.BuildEmbed($"u/{post.Author}", "", "", post.Title, "", post.Link, "", post.Url, null, "", "", Helper.GetRandomColor(), false);
            cmdMsg.Channel.SendMessageAsync(e);
            return "";
        }
    }

    public class CommandInsult : ICommandHandler
    {
        public string Command => "insult";
        public string Aliases => "";
        public string Usage => Command;
        public PermType PermType => PermType.Everyone;
        public string Description => "Returns a random insult";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            InsultPost post = API.Client.RequestInsult();
            return post.Insult;
        }
    }

    public class CommandYoMama : ICommandHandler
    {
        public string Command => "yomama";
        public string Aliases => "";
        public string Usage => Command;
        public PermType PermType => PermType.Everyone;
        public string Description => "Returns a random yo mama joke";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            YoMama yoMama = API.Client.RequestYoMama();
            return yoMama.Joke;
        }
    }

    public class CommandFriends : ICommandHandler
    {
        public string Command => "friends";
        public string Aliases => "";
        public string Usage => Command;
        public PermType PermType => PermType.Everyone;
        public string Description => "Returns a random Friends quote.";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            FriendsQuote friendsQuote = API.Client.RequestFriendsQuote();
            cmdMsg.Channel.SendMessageAsync(ClassBuilder.BuildEmbed("", "", "", "", "", "", "", "", new List<Field>() { new Field(true, friendsQuote.Character, friendsQuote.Quote) }, "", "", Helper.GetRandomColor(), false));
            return "";
        }
    }

    public class CommandMembers : ICommandHandler
    {
        public string Command => "members";
        public string Aliases => "";
        public string Usage => "members <role>";
        public PermType PermType => PermType.Everyone;
        public string Description => "Returns a list of all members in a specified role.";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1)
                return $":x: Missing arguments!\nUsage: {Usage}";
            DiscordRole role = args.Combine().GetRole();
            if (role == null)
                return $":x: Missing arguments!\nUsage: {Usage}";
            cmdMsg.Channel.SendMessageAsync(role.GetMembersEmbed());
            return "";
        }
    }

    public class CommandFact : ICommandHandler
    {
        public string Command => "fact";
        public string Aliases => "";
        public string Usage => Command;
        public PermType PermType => PermType.Everyone;
        public string Description => "Returns a random fact";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            RandomFact randomFact = API.Client.RequestFact();
            return randomFact.Fact;
        }
    }

    public class CommandRegionInfo : ICommandHandler
    {
        public string Command => "regioninfo";
        public string Aliases => "";
        public string Usage => "regioninfo";
        public PermType PermType => PermType.Everyone;
        public string Description => "Returns information about a voice region";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            cmdMsg.Channel.SendMessageAsync(cmdMsg.Channel.Guild.VoiceRegion.GetInfo());
            return "";
        }
    }

    public class CommandVoiceInfo : ICommandHandler
    {
        public string Command => "voiceinfo";
        public string Aliases => "";
        public string Usage => "voiceinfo (voiceChannel)";
        public PermType PermType => PermType.Everyone;
        public string Description => "Returns information about a voice channel";

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            DiscordChannel channel = args.Length < 1 ? sender.GetCurrentVoice() : args.Combine().GetVoice();
            if (channel == null)
                return $":x: Missing arguments!\nUsage: {Usage}";
            cmdMsg.Channel.SendMessageAsync(channel.GetInfo());
            return "";
        }
    }

    public class CommandModerators : ICommandHandler
    {
        public string Command => "moderators";
        public string Aliases => "mods";
        public string Usage => "moderators";
        public string Description => "Lists all moderators.";
        public PermType PermType => PermType.Moderator;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            cmdMsg.Channel.SendMessageAsync(GetMods(Configs.StaffConfig.StaffIds));
            return "";
        }

        public enum ObjectType
        {
            User,
            Role,
            Channel,
            None
        }

        public static ObjectType Get(string arg, out DiscordMember user, out DiscordRole role, out DiscordChannel channel)
        {
            DiscordRole r = arg.GetRole();
            DiscordMember usr = arg.GetUser();
            DiscordChannel ch = arg.GetTextChannel();
            if (r != null)
            {
                role = r;
                user = null;
                channel = null;
                return ObjectType.Role;
            }

            if (usr != null)
            {
                user = usr;
                role = null;
                channel = null;
                return ObjectType.User;
            }

            if (ch != null)
            {
                user = null;
                role = null;
                channel = ch;
                return ObjectType.Channel;
            }

            user = null;
            role = null;
            channel = null;
            return ObjectType.None;
        }

        public DiscordEmbed GetMods(IEnumerable<ulong> ids)
        {
            string roles = "**Roles:**\n";
            string users = "**Users:**\n";

            foreach (ulong id in ids)
            {
                ObjectType type = Get(id.ToString(), out DiscordMember user, out DiscordRole role, out DiscordChannel channel);
                if (type == ObjectType.None)
                    continue;
                if (type == ObjectType.Role && role != null && !string.IsNullOrEmpty(role.Mention) && roles.Length < 2048 && (roles + role.Mention).Length < 2048)
                    roles += $"{role.Mention}\n";
                if (type == ObjectType.User && user != null && !string.IsNullOrEmpty(user.Mention) && users.Length < 2048 && (users + user.Mention).Length < 2048)
                    users += $"{user.Mention}\n";
            }

            return ClassBuilder.BuildEmbed("", "", "", "Moderators", $"{roles}\n{users}", "", "", "", null, "", "", Helper.GetRandomColor(), false);
        }
    }

    public class CommandRandomEmote : ICommandHandler
    {
        public string Command => "randomemote";
        public string Aliases => "";
        public string Usage => "";
        public string Description => "Returns a random emote.";
        public PermType PermType => PermType.Everyone;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            DiscordEmoji emote = GetRandom(sender.Guild);
            cmdMsg.Channel.SendMessageAsync($"{(emote.IsAnimated ? $"<a:{emote.Name}:{emote.Id}>" : $"<:{emote.Name}:{emote.Id}>")}");
            return "";
        }

        public static DiscordEmoji GetRandom(DiscordGuild g) => g.Emojis.Values.ToList()[new Random().Next(g.Emojis.Count)];

        public static DiscordEmoji GetEmote(string arg, DiscordGuild g)
        {
            arg = arg.RemoveCharacters();

            if (ulong.TryParse(arg, out ulong id))
            {
                foreach (DiscordEmoji e in g.Emojis.Values)
                {
                    if (e.Id == id || e.Name == id.ToString())
                        return e;
                }
            }

            foreach (DiscordEmoji e in g.Emojis.Values)
            {
                if (e.Name.ToLower() == arg.ToLower() || e.Name.ToLower().Contains(arg.ToLower()) || e.Id.ToString() == arg)
                    return e;
            }

            return null;
        }
    }

    public class CommandEmoteInfo : ICommandHandler
    {
        public string Command => "emoteinfo";
        public string Aliases => "";
        public string Usage => "emoteinfo (emote)";
        public string Description => "Returns information about an emote";
        public PermType PermType => PermType.Everyone;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            DiscordEmoji emote = args.Length < 1 ? CommandRandomEmote.GetRandom(sender.Guild) : CommandRandomEmote.GetEmote(args[0].Replace(":", ""), sender.Guild);
            if (emote == null)
                emote = CommandRandomEmote.GetRandom(sender.Guild);
            cmdMsg.Channel.SendMessageAsync(emote.GetInfo());
            return "";
        }
    }

    public class CommandVerify : ICommandHandler
    {
        public string Command => "verify";
        public string Aliases => "";
        public string Usage => "verify <user>";
        public string Description => "Adds a verified role to mentioned user.";
        public PermType PermType => PermType.Moderator;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1)
                return Usage;
            DiscordMember usr = args.Combine().GetUser();
            if (usr == null)
                return $":x: Missing arguments!\nUsage: verify <user>";
            return Helper.Verify(usr, sender);
        }
    }

    public class CommandSuggestions : ICommandHandler
    {
        public string Command => "suggestions";
        public string Aliases => "";
        public string Usage => "suggestions <addchannel/removechannel> <sourceChannel> <suggestionsChannel>";
        public string Description => "Sets up the suggestions module.";
        public PermType PermType => PermType.BotMaster;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1)
                return $":x: Missing arguments!\nUsage: {Usage}";
            if (args[0] == "addchannel")
            {
                if (args.Length < 3)
                    return $":x: Missing arguments!\nUsage: {Usage}";
                DiscordChannel source = args[1].GetTextChannel();
                DiscordChannel suggests = args[2].GetTextChannel();
                if (source == null || suggests == null)
                    return $":x: Missing arguments!\nUsage: {Usage}";
                Suggestions.Add(source.Id, suggests.Id);
                return $":white_check_mark: Suggestions written in {source.Mention} will be sent to {suggests.Mention}";
            }

            if (args[0] == "removechannel")
            {
                if (args.Length < 3)
                    return $":x: Missing arguments!\nUsage: {Usage}";
                DiscordChannel source = args[1].GetTextChannel();
                DiscordChannel suggests = args[2].GetTextChannel();
                if (source == null || suggests == null)
                    return $":x: Missing arguments!\nUsage: {Usage}";
                Suggestions.Remove(source.Id, suggests.Id);
                return $":white_check_mark: Suggestions written in {source.Mention} will no longer be sent to {suggests.Mention}";
            }

            return $":x: Missing arguments!\nUsage: {Usage}";
        }
    }

    public class CommandSuggest : ICommandHandler
    {
        public string Command => "suggest";
        public string Aliases => "";
        public string Usage => "suggest <suggestion>";
        public string Description => "Sends a suggestion to the connected channel.";
        public PermType PermType => PermType.Everyone;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1)
                return Usage;
            string suggestion = args.Combine();
            if (!Suggestions.Suggest(cmdMsg.Channel.Id, sender, suggestion, out string error, out string channel))
                return error;
            else
                return $":white_check_mark: Your suggestion was sent to {channel}";
        }
    }

    public class CommandAutomod : ICommandHandler
    {
        public string Command => "automod";
        public string Aliases => "";
        public string Usage => "automod <setfilter/setaction/setreason/maxcaps/maxmentions/setduration/ignore/switch/filters/reasons>";
        public string Description => "Sets up the moderation module.";
        public PermType PermType => PermType.BotMaster;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1)
                return $":x: Missing arguments!\nUsage: {Usage}";
            if (args[0] == "setfilter")
            {
                if (args.Length < 2)
                    return $":x: Missing arguments!\nUsage: automod setfilter <filter>";
                AutoModerator.Filter filter = AutoModerator.GetFilter(args[1]);
                if (filter == AutoModerator.Filter.Undefined)
                    return $":x: Missing arguments!\nUsage: {Usage}";
                if (!bool.TryParse(args[2], out bool state))
                    return $":x: Missing arguments!\nUsage: {Usage}";
                AutoModerator.SetFilter(filter, state);
                return $":white_check_mark: Succesfully {(state ? "enabled" : "disabled")} {filter} filtering.";
            }

            if (args[0] == "setaction")
            {
                if (args.Length < 3)
                    return $":x: Missing arguments!\nUsage: automod setaction <filter> <action>";
                AutoModerator.Action act = AutoModerator.GetAction(args[2]);
                AutoModerator.Filter filter = AutoModerator.GetFilter(args[1]);
                if (act == AutoModerator.Action.None || filter == AutoModerator.Filter.Undefined)
                    return $":x: Missing arguments!\nUsage: {Usage}";
                AutoModerator.SetAction(filter, act);
                return $":white_check_mark: Succesfully set action of {filter} filter to {act}.";
            }

            if (args[0] == "setreason")
            {
                if (args.Length < 3)
                    return $":x: Missing arguments!\nUsage: automod setreason <filter> <reason>";
                AutoModerator.Filter filter = AutoModerator.GetFilter(args[1]);
                if (filter == AutoModerator.Filter.Undefined)
                    return $":x: Missing arguments!\nUsage: {Usage}";
                string str = args.Skip(2).ToArray().Combine();
                AutoModerator.SetSentence(filter, str);
                return $":white_check_mark: Succesfully set reason of {filter} filter to **{str}**";
            }

            if (args[0] == "maxcaps")
            {
                if (args.Length < 2)
                    return $":x: Missing arguments!\nUsage: automod maxcaps <max caps>";
                if (!int.TryParse(args[1], out int caps))
                    return $":x: Missing arguments!\nUsage: {Usage}";
                AutoModerator.Config.MaxCaps = caps;
                AutoModerator.Save();
                return $":white_check_mark: MaxCaps filter has been set to **{caps}**";
            }

            if (args[0] == "maxmentions")
            {
                if (args.Length < 2)
                    return $":x: Missing arguments!\nUsage: automod maxmentions <max mentions>";
                if (!int.TryParse(args[1], out int mentions))
                    return $":x: Missing arguments!\nUsage: {Usage}";
                AutoModerator.Config.Mentions = mentions;
                AutoModerator.Save();
                return $":white_check_mark: MaxMentions filter has been set to **{mentions}**";
            }

            if (args[0] == "setduration")
            {
                if (args.Length < 3)
                    return $":x: Missing arguments!\nUsage: automod setduration <filter> <duration>";
                AutoModerator.Filter filter = AutoModerator.GetFilter(args[1]);
                if (filter == AutoModerator.Filter.Undefined)
                    return $":x: Missing arguments!\nUsage: automod setduration <filter> <duration>";
                if (!Timer.TryParse(args[2], out Timer.Time time))
                    return $":x: Missing arguments!\nUsage: automod setduration <filter> <duration>";
                ulong dur = Timer.ParseDuration(args[2]);
                string str = Timer.ParseString(dur, time);
                AutoModerator.Config.Length[filter] = args[2];
                AutoModerator.Save();
                return $":white_check_mark: Duration of {filter} filter was set to **{dur} {str}**";
            }

            if (args[0] == "ignore")
            {
                if (args.Length < 2)
                    return $":x: Missing arguments!\nUsage: automod ignore <ID>";
                CommandModerators.ObjectType type = CommandModerators.Get(args.Skip(2).ToArray().Combine(), out DiscordMember usr, out DiscordRole role, out DiscordChannel channel);
                if (type == CommandModerators.ObjectType.None)
                    return $":x: Missing arguments!\nUsage: automod ignore <ID>";
                AutoModerator.Config.IgnoredIDs.Add(type == CommandModerators.ObjectType.Role ? role.Id : usr.Id);
                AutoModerator.Save();
                return $":white_check_mark: Added {(type == CommandModerators.ObjectType.Role ? role.Mention : usr.Mention)} to ignored {(type == CommandModerators.ObjectType.Role ? "roles" : "users")}";
            }

            if (args[0] == "switch")
            {
                AutoModerator.Config.Enabled = !AutoModerator.Config.Enabled;
                AutoModerator.Save();
                return $":white_check_mark: Succesfully {(AutoModerator.Config.Enabled ? "enabled" : "disabled")} automod.";
            }

            if (args[0] == "filters")
            {
                List<Field> fields = new List<Field>();
                foreach (KeyValuePair<AutoModerator.Filter, AutoModerator.Action> pair in AutoModerator.Config.Actions)
                {
                    fields.Add(new Field(pair.Key, pair.Value));
                }
                cmdMsg.Channel.SendMessageAsync(ClassBuilder.BuildEmbed($"{sender.Username}#{sender.Discriminator}", "", sender.GetAvatar(), "", "", "", "", "", fields, "", "", Helper.GetRandomColor(), false));
                return "";
            }

            if (args[0] == "reasons")
            {
                List<Field> fields = new List<Field>();
                foreach (KeyValuePair<AutoModerator.Filter, string> pair in AutoModerator.Config.Sentences)
                {
                    fields.Add(new Field(pair.Key, pair.Value));
                }
                cmdMsg.Channel.SendMessageAsync(ClassBuilder.BuildEmbed($"{sender.Username}#{sender.Discriminator}", "", sender.GetAvatar(), "", "", "", "", "", fields, "", "", Helper.GetRandomColor(), false));
                return "";
            }

            return $":x: Missing arguments!\nUsage: {Usage}";
        }
    }

    public class CommandAutoMeme : ICommandHandler
    {
        public string Command => "automeme";
        public string Aliases => "";
        public string Usage => "automeme (channel)";
        public string Description => "Sets a channel to post a meme in every 30 minutes.";
        public PermType PermType => PermType.BotMaster;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            DiscordChannel channel = args.Length < 1 ? cmdMsg.Channel.Id.ToString().GetTextChannel() : args.Combine().GetTextChannel();
            Configs.BotConfig.AutoMemeChannel = channel.Id;
            Configs.SaveBotConfig();
            return $":white_check_mark: A ~~shitpost~~ meme will be posted in {channel.Mention} every 30 minutes.";
        }
    }

    public class CommandFactOfTheDay : ICommandHandler
    {
        public string Command => "factoftheday";
        public string Aliases => "fotd";
        public string Usage => "factoftheday (channel)";
        public string Description => "Sets a channel to post a fact in every day.";
        public PermType PermType => PermType.BotMaster;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            DiscordChannel channel = args.Length < 1 ? cmdMsg.Channel.Id.ToString().GetTextChannel() : args.Combine().GetTextChannel();
            Configs.BotConfig.FactChannel = channel.Id;
            Configs.SaveBotConfig();
            return $":white_check_mark: A random fact will be posted in {channel.Mention} every day.";
        }
    }

    public class CommandEditSuggestion : ICommandHandler
    {
        public string Command => "editsuggestion";
        public string Aliases => "";
        public string Usage => "editsuggestion <id> <new suggestion>";
        public string Description => "Edits a suggestion.";
        public PermType PermType => PermType.Everyone;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            try
            {
                if (args.Length < 1)
                    return $":x: Missing arguments!\nUsage: {Usage}";
                if (!ulong.TryParse(args[0], out ulong id))
                    return $":x: Missing arguments!\nUsage: {Usage}";
                if (!Suggestions.TryGet(id, out Suggestions.SuggestionProperties props))
                    return $":x: Cannot find any suggestions with that ID.";
                if (props.Message == null)
                    return $":x: This suggestion was probably deleted.";
                if (props.AuthorId != sender.Id)
                    return $":x: That is not your suggestion.";
                string suggest = args.Skip(1).ToArray().Combine();
                props.Modify(suggest).GetAwaiter().GetResult();
                return $":white_check_mark: Succesfully modified your suggestion.\nNew suggestion: **{suggest}**";
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return $":x: An error occured! Exception of type {e.GetType().Name} was thrown.";
            }
        }
    }

    public class CommandGiveaways : ICommandHandler
    {
        public string Command => "giveaways";
        public string Aliases => "";
        public string Usage => "giveaways <channel>";
        public string Description => "Sets a giveaway channel.";
        public PermType PermType => PermType.BotMaster;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            DiscordChannel channel = args.Length < 1 ? cmdMsg.Channel.Id.ToString().GetTextChannel() : args.Combine().GetTextChannel();
            Configs.BotConfig.GiveawayChannel = channel.Id;
            Configs.SaveBotConfig();
            return $":white_check_mark: Giveaways will be posted to {channel.Mention}";
        }
    }

    public class CommandGiveaway : ICommandHandler
    {
        public string Command => "giveaway";
        public string Aliases => "";
        public string Usage => "giveaway <duration> <mention> <item>";
        public string Description => "Creates a giveaway";
        public PermType PermType => PermType.Moderator;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 3)
                return $":x: Missing arguments!\nUsage: {Usage}";
            string dur = args[0];
            string mention = args[1];
            string item = args.Skip(2).ToArray().Combine();
            return GiveawayManager.CreateGiveaway(mention, dur, item, sender);
        }
    }

    public class CommandConfig : ICommandHandler
    {
        public string Command => "config";
        public string Aliases => "cfg";
        public string Usage => "cfg <key> (value)";
        public string Description => $"Sets or gets a current config value. Use **{Configs.BotConfig.Prefix}cfg keys** to get a list of all config keys.";
        public PermType PermType => PermType.BotMaster;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1)
                return $":x: Missing arguments!\nUsage: {Usage}";

            if (args[0] == "keys")
            {
                return "allowLavaLink, debug, modLogChannel, lavalinkPassword, verifiedRole, unverifiedRole, serverLogChannel, autoMemeChannel, factChannel, giveaways, autoMemeSubreddit";
            }

            if (args[0] == "allowLavaLink")
            {
                if (args.Length < 2)
                    return $"Current value: **{Configs.BotConfig.AllowLavaLink}**";
                else
                {
                    if (!bool.TryParse(args[1], out bool n))
                        return ":x: I suggest adding a valid boolean while you're at it.";
                    Configs.BotConfig.AllowLavaLink = n;
                    Configs.SaveBotConfig();
                    return $":white_check_mark: {(n ? "Enabled" : "Disabled")} LavaLink.";
                }
            }

            if (args[0] == "debug")
            {
                if (args.Length < 2)
                    return $"Current value: **{Configs.BotConfig.Debug}**";
                else
                {
                    if (!bool.TryParse(args[1], out bool n))
                        return ":x: I suggest adding a valid boolean while you're at it.";
                    Configs.BotConfig.Debug = n;
                    Configs.SaveBotConfig();
                    return $":white_check_mark: {(n ? "Enabled" : "Disabled")} debug.";
                }
            }

            if (args[0] == "modLogChannel")
            {
                if (args.Length < 2)
                    return $"Current value: **{Configs.BotConfig.ModLogChannel}**";
                else
                {
                    if (!ulong.TryParse(args[1], out ulong n))
                        return ":x: I suggest adding a valid number while you're at it.";
                    Configs.BotConfig.ModLogChannel = n;
                    Configs.SaveBotConfig();
                    return $":white_check_mark: ModLog channel was set to {n}";
                }
            }

            if (args[0] == "lavalinkPassword")
            {
                if (args.Length < 2)
                    return $"Current value: **{Configs.BotConfig.LavalinkPassword}**";
                else
                {
                    Configs.BotConfig.LavalinkPassword = args.Skip(1).ToArray().Combine();
                    Configs.SaveBotConfig();
                    return $":white_check_mark: Password was set to {args.Skip(1).ToArray().Combine()}";
                }
            }

            if (args[0] == "verifiedRole")
            {
                if (args.Length < 2)
                    return $"Current value: **{Configs.BotConfig.VerifiedRoleId}**";
                else
                {
                    if (!ulong.TryParse(args[1], out ulong n))
                        return ":x: I suggest adding a valid number while you're at it.";
                    Configs.BotConfig.VerifiedRoleId = n;
                    Configs.SaveBotConfig();
                    return $":white_check_mark: VerifiedRoleId was set to {n}";
                }
            }

            if (args[0] == "unverifiedRole")
            {
                if (args.Length < 2)
                    return $"Current value: **{Configs.BotConfig.UnverifiedRoleId}**";
                else
                {
                    if (!ulong.TryParse(args[1], out ulong n))
                        return ":x: I suggest adding a valid number while you're at it.";
                    Configs.BotConfig.UnverifiedRoleId = n;
                    Configs.SaveBotConfig();
                    return $":white_check_mark: UnverifiedRoleId was set to {n}";
                }
            }

            if (args[0] == "serverLogChannel")
            {
                if (args.Length < 2)
                    return $"Current value: **{Configs.BotConfig.ServerLogChannel}**";
                else
                {
                    if (!ulong.TryParse(args[1], out ulong n))
                        return ":x: I suggest adding a valid number while you're at it.";
                    Configs.BotConfig.ServerLogChannel = n;
                    Configs.SaveBotConfig();
                    return $":white_check_mark: ServerLog channel was set to {n}";
                }
            }

            if (args[0] == "autoMemeChannel")
            {
                if (args.Length < 2)
                    return $"Current value: **{Configs.BotConfig.AutoMemeChannel}**";
                else
                {
                    if (!ulong.TryParse(args[1], out ulong n))
                        return ":x: I suggest adding a valid number while you're at it.";
                    Configs.BotConfig.AutoMemeChannel = n;
                    Configs.SaveBotConfig();
                    return $":white_check_mark: AutoMeme channel was set to {n}";
                }
            }

            if (args[0] == "factChannel")
            {
                if (args.Length < 2)
                    return $"Current value: **{Configs.BotConfig.FactChannel}**";
                else
                {
                    if (!ulong.TryParse(args[1], out ulong n))
                        return ":x: I suggest adding a valid number while you're at it.";
                    Configs.BotConfig.FactChannel = n;
                    Configs.SaveBotConfig();
                    return $":white_check_mark: Fact channel was set to {n}";
                }
            }

            if (args[0] == "giveawaysChannel")
            {
                if (args.Length < 2)
                    return $"Current value: **{Configs.BotConfig.GiveawayChannel}**";
                else
                {
                    if (!ulong.TryParse(args[1], out ulong n))
                        return ":x: I suggest adding a valid number while you're at it.";
                    Configs.BotConfig.GiveawayChannel = n;
                    Configs.SaveBotConfig();
                    return $":white_check_mark: Giveaways channel was set to {n}";
                }
            }

            if (args[0] == "deleteAfterUsage")
            {
                if (args.Length < 2)
                    return $"Current value: **{Configs.BotConfig.CommandsToDelete.ToString<string>()}**";
                else
                {
                    if (args[1] == "add")
                    {
                        string toAdd = args.Skip(2).ToArray().Combine();
                        Configs.BotConfig.CommandsToDelete.Add(toAdd);
                        Configs.SaveBotConfig();
                        return $":x: Added **{toAdd}** to commands deleted after usage.";
                    }

                    if (args[1] == "remove")
                    {
                        string toRemove = args.Skip(2).ToArray().Combine();
                        if (Configs.BotConfig.CommandsToDelete.Contains(toRemove))
                            Configs.BotConfig.CommandsToDelete.Remove(toRemove);
                        Configs.SaveBotConfig();
                        return $":x: Removed **{toRemove}** from commands deleted after usage.";
                    }
                }

                if (args[0] == "autoMemeSubreddit")
                {
                    if (args.Length < 1)
                        return $"Current value: **r/{Configs.BotConfig.Subreddit}**";
                    string subreddit = args.Skip(1).ToArray().Combine();
                    Configs.BotConfig.Subreddit = subreddit;
                    Configs.SaveBotConfig();
                    return $":white_check_mark: AutoMeme Subreddit was set to **r/{subreddit}**";
                }
            }

            return $":x: Missing arguments!\nUsage: {Usage}";
        }
    }

    public class CommandReroll : ICommandHandler
    {
        public string Command => "reroll";
        public string Aliases => "";
        public string Usage => "reroll <id>";
        public string Description => "Chooses a new winner of a specific giveaway.";
        public PermType PermType => PermType.Moderator;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1 || !ulong.TryParse(args[0], out ulong id))
                return $":x: Missing arguments!\nUsage: {Usage}";
            GiveawayManager.Giveaway gw = GiveawayManager.GetGiveaway(id);
            if (gw == null)
                return $":x: Cannot find any giveaways with that ID ({id}).";
            gw.ReRoll();
            return "";
        }
    }

    public class CommandUptime : ICommandHandler
    {
        public string Command => "uptime";
        public string Aliases => "";
        public string Usage => "uptime";
        public string Description => "";
        public PermType PermType => PermType.Everyone;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args) => EventHandler.GetUptime();
    }

    public class CommandWarns : ICommandHandler
    {
        public string Command => "warns";
        public string Aliases => "";
        public string Usage => "warns (user)";
        public string Description => "Lists all warnings given to a user.";
        public PermType PermType => PermType.Moderator;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            DiscordMember user = args.Length < 1 ? sender : args.Combine().GetUser();
            List<Field> fields = WarnHandler.GetWarnsField(user.Id);
            if (fields == null) return ":x: No warnings found.";
            cmdMsg.Channel.SendMessageAsync(ClassBuilder.BuildEmbed(sender.GetAuthor(), "", sender.GetAvatar(), "", $"**All warnings for {user.Mention}**:", "", "", "", fields, "", "", Helper.GetRandomColor(), false));
            return "";
        }
    }

    public class CommandInfoEmbed : ICommandHandler
    {
        public string Command => "infoembed";
        public string Aliases => "";
        public string Usage => "infoembed <channel>";
        public string Description => "Posts a informatic embed message that will be updated every 15 seconds.";
        public PermType PermType => PermType.BotMaster;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            DiscordChannel channel = args.Length < 1 ? cmdMsg.Channel.Id.ToString().GetTextChannel() : args[0].GetTextChannel();
            EventHandler.InfoEmbedMessage = channel.SendMessageAsync(EventHandler.CreateInfoEmbed(sender.Guild)).GetAwaiter().GetResult();
            Configs.BotConfig.InfoEmbed = new InfoEmbed
            {
                ChannelId = channel.Id,
                MessageId = EventHandler.InfoEmbedMessage?.Id,
                ServerId = sender.Guild.Id
            };
            Configs.SaveBotConfig();
            return $":white_check_mark: Posted an embed to {channel.Mention}.";
        }
    }

    public class CommandRadio : ICommandHandler
    {
        public string Command => "radio";
        public string Aliases => "";
        public string Usage => "radio <name>/list/pause/resume/leave/ping/volume <volume>";
        public string Description => "Starts playing a radio or lists all available radios.";
        public PermType PermType => PermType.Everyone;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1)
                return $":x: Missing arguments!\nUsage: {Usage}";
            if (args[0] == "list")
            {
                if (Configs.BotConfig.RadioStations.Count < 1)
                    return $":x: There aren't any radio stations added. Use **{Configs.BotConfig.Prefix}addradio <name> <url>** to add one.";
                else
                {
                    string s = "";
                    foreach (KeyValuePair<string, string> pair in Configs.BotConfig.RadioStations)
                    {
                        s += $"**{Configs.BotConfig.RadioStations.Keys.ToList().IndexOf(pair.Key) + 1})** {pair.Key}\n";
                    }
                    return s;
                }
            }

            if (args[0] == "pause")
            {
                if (!Helper.VoiceNextConnection.IsPlaying)
                    return $":x: There aren't any radio stations playing.";
                Helper.VoiceNextConnection.Pause();
                return $":white_check_mark: Paused.";
            }

            if (args[0] == "resume")
            {
                Helper.VoiceNextConnection.ResumeAsync().GetAwaiter().GetResult();
                return $":white_check_mark: Resumed.";
            }

            if (args[0] == "leave")
            {
                Radio.Cancel = true;
                return $":white_check_mark: Goodbye.";
            }

            if (args[0] == "ping")
            {
                return $"{Helper.VoiceNextConnection.WebSocketPing}ms/{Helper.VoiceNextConnection.UdpPing}ms";
            }

            if (args[0] == "volume")
            {
                if (args.Length < 2) return $":x: Missing arguments!\nUsage: **{Configs.BotConfig.Prefix}radio volume <volume>**";
                if (!float.TryParse(args[1], out float volume)) return $":x: Specify a valid integer.";
                Helper.Sink.VolumeModifier = volume / 100f;
                return $":white_check_mark: Volume set to {volume}";
            }

            if (!Configs.BotConfig.RadioStations.TryGetValue(args.Combine(), out string url))
                return $":x: Can't find a station with that name. Use **{Configs.BotConfig.Prefix}radio list** to list all available stations.";
            else
            {
                if (sender.GetCurrentVoice() == null)
                    return ":x: You need to be in a voice channel to use this command!";
                else
                {
                    Radio.Play(url, sender.GetCurrentVoice());
                    return $":white_check_mark: Started playing **{args.Combine()}**";
                }
            }
        }
    }

    public class CommandAddRadio : ICommandHandler
    {
        public string Command => "addradio";
        public string Aliases => "";
        public string Usage => "addradio <name> <url>";
        public string Description => "Adds a radio station.";
        public PermType PermType => PermType.Moderator;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 2)
                return $":x: Missing arguments!\nUsage: {Usage}";
            else
            {
                string key = args[0];
                string url = args.Skip(1).ToArray().Combine();
                if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri uri))
                    return $":x: **{url}** is not a valid URL.";
                if (Configs.BotConfig.RadioStations.ContainsKey(key))
                    return ":x: A station with that name already exists.";
                Configs.BotConfig.RadioStations?.Add(key, url);
                Configs.SaveBotConfig();
                return $":white_check_mark: Added **{key}** to available radio stations.";
            }
        }
    }

    public class CommandRandomColor : ICommandHandler
    {
        public string Command => "randomcolor";
        public string Aliases => "";
        public string Usage => "";
        public string Description => "Returns a random discord color";
        public PermType PermType => PermType.Everyone;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            DiscordColor color = Helper.GetRandomColor();
            cmdMsg.Channel.SendMessageAsync(ClassBuilder.BuildEmbed($"{sender.Username}#{sender.Discriminator}", "", sender.AvatarUrl, "", "", "", "", "", new List<Field>() { new Field("Hexadecimal", color.ToString()) }, "", "", color, false));
            return "";
        }
    }

    public class CommandLavalinkStatistics : ICommandHandler
    {
        public string Command => "lavastats";
        public string Aliases => "";
        public string Usage => "";
        public string Description => "Displays Lavalink server statistics";
        public PermType PermType => PermType.Everyone;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            try
            {
                NodeStatistics s = Helper.LavalinkClient.Statistics;
                if (s == null) return ":x: Statistics are not available now.";
                Field deficitFrames = new Field("Deficit Frames", s.FrameStatistics.AverageDeficitFrames);
                Field nullFrames = new Field("Null Frames", s.FrameStatistics.AverageNulledFrames);
                Field frames = new Field("Frames Sent", s.FrameStatistics.AverageFramesSent);

                Field allocatedMemory = new Field("Allocated Memory", $"{Helper.ToMb(s.Memory.AllocatedMemory)} MB");
                Field freeMemory = new Field("Free Memory", $"{Helper.ToMb(s.Memory.FreeMemory)} MB");
                Field reservableMemory = new Field("Reservable Memory", $"{Helper.ToMb(s.Memory.ReservableMemory)} MB");
                Field usedMemory = new Field("Used Memory", $"{Helper.ToMb(s.Memory.UsedMemory)} MB");

                Field players = new Field("Players", s.Players);
                Field playingPlayers = new Field("Playing Players", s.PlayingPlayers);

                Field cpuCores = new Field("CPU Cores", s.Processor.Cores);
                Field nodeLoad = new Field("Server Load", s.Processor.NodeLoad * 1000f);
                Field serverLoad = new Field("System Load", s.Processor.SystemLoad * 1000f);

                Field uptime = new Field("Uptime", EventHandler.GetUptime(s.Uptime));
                List<Field> fields = new List<Field>() { frames, deficitFrames, nullFrames, allocatedMemory, reservableMemory, freeMemory, usedMemory, players, playingPlayers, cpuCores, nodeLoad, serverLoad, uptime };
                cmdMsg.Channel.SendMessageAsync(ClassBuilder.BuildEmbed($"{sender.Username}#{sender.Discriminator}", "", sender.AvatarUrl, "Lavalink Server Statistics", "", "", "", "", fields, "", "", Helper.GetRandomColor(), false));
                return "";
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return e.Message;
            }
        }
    }

    public class CommandHostInfo : ICommandHandler
    {
        public string Command => "hostinfo";
        public string Aliases => "";
        public string Usage => "";
        public string Description => "Displays information about the machine this bot is hosted on";
        public PermType PermType => PermType.Everyone;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            try
            {
                Helper.SystemInformation info = Helper.SystemInformation.Get();

                Field os = new Field("OS Version", info.OperatingSystem);
                Field gpuName = new Field("GPU Device", info.GpuName);

                Field cpuManufacturer = new Field("CPU Manufacturer", info.CpuManufacturer);
                Field cpuName = new Field("CPU Name", info.CpuName);
                Field coreClock = new Field("CPU Core Clock Speed", $"{info.CpuClockSpeed} MHz");
                Field coreCount = new Field("CPU Core Count", info.CpuCoreCount);
                Field cpuSocket = new Field("CPU Socket", info.CpuSocket);

                List<Field> fields = new List<Field>() { os, gpuName, cpuManufacturer, cpuName, cpuSocket, coreCount, coreClock };
                cmdMsg.Channel.SendMessageAsync(ClassBuilder.BuildEmbed($"{sender.Username}#{sender.Discriminator}", "", sender.AvatarUrl, "", "", "", "", "", fields, "", "", Helper.GetRandomColor(), false));
                return "";
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return e.Message;
            }
        }
    }

    public class CommandReactionRoles : ICommandHandler
    {
        public string Command => "reactionrole";
        public string Aliases => "rr";
        public string Usage => "reactionrole <messageId> <reactionName> <roleId>";
        public string Description => "Used to setup reaction roles.";
        public PermType PermType => PermType.BotMaster;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 3) return $":x: Missing arguments!\n Usage: {Usage}";
            if (!ulong.TryParse(args[0], out ulong messageId)) return $":x: {args[0]} is not a valid DiscordID!";
            string reaction = args[1];
            DiscordRole role = args.Skip(2).ToArray().Combine().GetRole();
            if (role == null) return $":x: Failed while trying to find a role by **{args.Skip(2).ToArray().Combine()}**";
            DiscordMessage message = messageId.GetMessage();
            if (message == null) return $":x: Failed while trying to find a message by **{messageId}**";
            ReactionRoles.Add(message, reaction, role);
            return $":white_check_mark: Added a new reaction role! ([{message.Id}]: {reaction} -> {role.Mention})";
        }
    }

    public class CommandRolePersist : ICommandHandler
    {
        public string Command => "rolepersist";
        public string Aliases => "";
        public string Usage => "rolepersist <user> <role>";
        public string Description => "Adds a role that will be assigned to this specific user everytime he joins";
        public PermType PermType => PermType.Moderator;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 2) return $":x: Missing arguments!\nUsage: {Usage}";
            DiscordMember user = args[0].GetUser();
            DiscordRole role = args.Skip(1).ToArray().Combine().GetRole();
            if (user == null || role == null) return $":x: Unable to find a {(role == null ? "role" : "user")} by {(role == null ? args.Skip(1).ToArray().Combine() : args[0])}";
            if (!Configs.BotConfig.RolePersists.ContainsKey(user.Id))
            {
                Configs.BotConfig.RolePersists.Add(user.Id, new List<ulong>() { role.Id });
            }
            else
            {
                if (Configs.BotConfig.RolePersists[user.Id].Contains(role.Id))
                {
                    Configs.BotConfig.RolePersists[user.Id].Remove(role.Id);
                    Configs.SaveBotConfig();
                    return $":white_check_mark: Removed {role.Mention} from {user.Mention}'s role persists";
                }

                Configs.BotConfig.RolePersists[user.Id].Add(role.Id);
            }

            Configs.SaveBotConfig();
            return $":white_check_mark: Added {role.Mention} to {user.Mention}'s role persists.";
        }
    }

    public class CommandSelfRole : ICommandHandler
    {
        public string Command => "selfrole";
        public string Aliases => "";
        public string Usage => "selfrole <role>";
        public string Description => "Adds a role that can be assigned to all members by using a command.";
        public PermType PermType => PermType.BotMaster;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1) return $":x: Missing arguments!\nUsage: {Usage}";
            DiscordRole role = args.Combine().GetRole();
            if (role == null) return $":x: Failed while trying to find a role by {args.Combine()}";
            if (Configs.BotConfig.SelfRoles.Contains(role.Id))
            {
                Configs.BotConfig.SelfRoles.Remove(role.Id);
                Configs.SaveBotConfig();
                return $":white_check_mark: Removed {role.Mention} from self-assignable roles.";
            }
            else
            {
                Configs.BotConfig.SelfRoles.Add(role.Id);
                Configs.SaveBotConfig();
                return $":white_check_mark: Added {role.Mention} to self roles.";
            }
        }
    }

    public class CommandRole : ICommandHandler
    {
        public string Command => "role";
        public string Aliases => "";
        public string Usage => "role <role>";
        public string Description => "Adds or removes a self assignable role";
        public PermType PermType => PermType.Everyone;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1) return $":x: Missing arguments!\nUsage: {Usage}";
            DiscordRole role = args.Combine().GetRole();
            if (role == null) return $":x: Failed while trying to find a role by {args.Combine()}";
            if (!Configs.BotConfig.SelfRoles.Contains(role.Id)) return ":x: That role is not self-assignable.";
            if (sender.HasRole(role))
            {
                sender.RevokeRoleAsync(role, "Self Role");
                return $":white_check_mark: Removed {role.Mention}";
            }
            else
            {
                sender.GrantRoleAsync(role, "Self Role");
                return $":white_check_mark: Added {role.Mention}";
            }
        }
    }

    public class CommandRoles : ICommandHandler
    {
        public string Command => "selfroles";
        public string Aliases => "";
        public string Usage => "";
        public string Description => "Get a list of joinable roles.";
        public PermType PermType => PermType.Everyone;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (Configs.BotConfig.SelfRoles.Count < 1) return ":x: There are not any self-assignable roles configured.";
            string s = $"Self Assignable Roles ({Configs.BotConfig.SelfRoles.Count}):\n";
            foreach (ulong id in Configs.BotConfig.SelfRoles)
            {
                DiscordRole role = id.GetRole();
                if (role == null) continue;
                s += $"{role.Mention}\n";
            }
            return s;
        }
    }

    public class CommandGuildRoles : ICommandHandler
    {
        public string Command => "roles";
        public string Aliases => "";
        public string Usage => "roles";
        public string Description => "A list of all roles in this server";
        public PermType PermType => PermType.Everyone;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            DiscordGuild guild = cmdMsg.Channel.Guild;
            string s = $"Roles ({guild.Roles.Count}):\n";
            foreach (DiscordRole role in guild.Roles.Values)
            {
                if (role == null || role.Id == guild.EveryoneRole.Id) continue;
                s += $"{role.Mention}\n";
            }
            return s;
        }
    }

    public class CommandIgnore : ICommandHandler
    {
        public string Command => "ignore";
        public string Aliases => "";
        public string Usage => "ignore <user/role/channel>";
        public string Description => "Adds or removes a role/user/channel that will be ignored when using commands.";
        public PermType PermType => PermType.BotMaster;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1) return $":x: Missing arguments!\nUsage: {Usage}";
            CommandModerators.ObjectType type = CommandModerators.Get(args.Combine(), out DiscordMember member, out DiscordRole role, out DiscordChannel channel);
            if (type == CommandModerators.ObjectType.None) return $":x: Failed while trying to find a member/role/channel from {args.Combine()}";
            if (type == CommandModerators.ObjectType.Channel)
            {
                if (Configs.BotConfig.IgnoredIds.Contains(channel.Id))
                {
                    Configs.BotConfig.IgnoredIds.Remove(channel.Id);
                    Configs.SaveBotConfig();
                    return $":white_check_mark: Removed {channel.Mention} from ignored channels.";
                }
                else
                {
                    Configs.BotConfig.IgnoredIds.Add(channel.Id);
                    Configs.SaveBotConfig();
                    return $":white_check_mark: Added {channel.Mention} to ignored channels.";
                }
            }

            if (type == CommandModerators.ObjectType.Role)
            {
                if (Configs.BotConfig.IgnoredIds.Contains(role.Id))
                {
                    Configs.BotConfig.IgnoredIds.Remove(role.Id);
                    Configs.SaveBotConfig();
                    return $":white_check_mark: Removed {role.Mention} from ignored roles.";
                }
                else
                {
                    Configs.BotConfig.IgnoredIds.Add(role.Id);
                    Configs.SaveBotConfig();
                    return $":white_check_mark: Added {role.Mention} to ignored roles.";
                }
            }

            if (type == CommandModerators.ObjectType.User)
            {
                if (Configs.BotConfig.IgnoredIds.Contains(member.Id))
                {
                    Configs.BotConfig.IgnoredIds.Remove(member.Id);
                    Configs.SaveBotConfig();
                    return $":white_check_mark: Removed {member.Mention} from ignored users.";
                }
                else
                {
                    Configs.BotConfig.IgnoredIds.Add(member.Id);
                    Configs.SaveBotConfig();
                    return $":white_check_mark: Added {member.Mention} to ignored members.";
                }
            }

            return $":x: Missing arguments!\nUsage: {Usage}";
        }
    }

    public class CommandChangeRole : ICommandHandler
    {
        public string Command => "changerole";
        public string Aliases => "";
        public string Usage => "changerole <user (all)> <role (all)>";
        public string Description => "Adds or removes a (all) role(s) from a specific user/all users";
        public PermType PermType => PermType.Moderator;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 2) return $":x: Missing arguments!\nUsage: {Usage}";
            DiscordMember user = args[0].GetUser();
            DiscordRole role = args.Skip(1).ToArray().Combine().GetRole();

            if (args[0] == "all")
            {
                Helper.CommitAddAll(role, cmdMsg.Channel.Guild, $"{sender.Username}#{sender.Discriminator}").GetAwaiter().GetResult();
                return $":white_check_mark: Adding {role.Mention} to {cmdMsg.Channel.Guild.MemberCount} users ..";
            }

            if (args[1] == "all")
            {
                int roles = user.Roles.Count();
                Helper.CommitRemoveAll(user, $"{sender.Username}#{sender.Discriminator}");
                return $"white_check_mark: Removing {roles} role(s) from {user.Mention}";
            }

            if (user == null || role == null) return $":x: Unable to find a {(role == null ? "role" : "user")} by {(role == null ? args.Skip(1).ToArray().Combine() : args[0])}";
            if (role.Position > sender.Roles.First().Position) return $":x: This role is higher than your highest role.";
            if (user.HasRole(role))
            {
                user.RevokeRoleAsync(role, $"Removed by {sender.Nickname}#{sender.Discriminator}");
                return $":white_check_mark: Removed {role.Mention} from {user.Mention}";
            }
            else
            {
                user.GrantRoleAsync(role, $"Added by {sender.Nickname}#{sender.Discriminator}");
                return $":white_check_mark: Added {role.Mention} to {user.Mention}";
            }
        }
    }

    public class CommandRps : ICommandHandler
    {
        public string Command => "rps";
        public string Aliases => "";
        public string Usage => "rps <choice>";
        public string Description => "Rock Paper Scissors with the bot.";
        public PermType PermType => PermType.Everyone;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            if (args.Length < 1)
            {
                List<API.Client.Rps> choices = Enum.GetValues(typeof(API.Client.Rps)).Cast<API.Client.Rps>().ToList();
                string s = $"Available choices ({choices.Count}):\n";
                foreach (API.Client.Rps rps in choices)
                {
                    s += $"{(int)rps}) {rps}";
                }

                return $":x: Missing arguments!\nUsage: {Usage}\n\n{s}";
            }
            else
            {
                API.Client.Rps? choice = API.Client.Get(args.Combine());
                if (choice == null)
                {
                    List<API.Client.Rps> choices = Enum.GetValues(typeof(API.Client.Rps)).Cast<API.Client.Rps>().ToList();
                    string s = $"Available choices ({choices.Count}):\n";
                    foreach (API.Client.Rps rps in choices)
                    {
                        s += $"{(int)rps}) {rps}";
                    }

                    return $":x: Invalid choice!\n{s}";
                }
                else
                {
                    API.Client.DoRps(cmdMsg.Channel, choice.Value).GetAwaiter().GetResult();
                    return "";
                }
            }
        }
    }

    public class CommandRandomDog : ICommandHandler
    {
        public string Command => "dog";
        public string Aliases => "";
        public string Usage => "dog";
        public string Description => "Returns a random image of a dog.";
        public PermType PermType => PermType.Everyone;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            RandomDogImage image = API.Client.RequestRandomDogImage();
            cmdMsg.Channel.SendMessageAsync(image.Url);
            return "";
        }
    }

    public class CommandRoll : ICommandHandler
    {
        public string Command => "roll";
        public string Aliases => "";
        public string Usage => "roll";
        public string Description => "Roll a dice.";
        public PermType PermType => PermType.Everyone;

        public string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args)
        {
            RolledDice dice = API.Client.RequestDice();
            return $"Type: **{dice.Value.Value<int>("type")}**\nValue: **{dice.Value.Value<string>("value")}**";
        }
    }
}
