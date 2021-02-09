using System.Collections.Generic;
using System.Linq;
using Multiverse.API;
using Multiverse.Commands;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace Multiverse
{
    public static class CommandManager
    {
        public static List<ICommandHandler> Commands { get; } = new List<ICommandHandler>();

        public static void Register(ICommandHandler handler)
        {
            Commands.Add(handler);
        }

        public static ICommandHandler GetCommandHandler(string command)
        {
            if (command.ToLower() == "help")
                return Commands.Where(h => h.Command == "help").FirstOrDefault();
            for (int i = 0; i < Commands.Count; i++)
            {
                ICommandHandler h = Commands[i];
                if (h.Command == command || h.Command.ToLower() == command.ToLower()) return h;
                if (!string.IsNullOrEmpty(h.Aliases) && (h.Aliases.Contains(command) || h.Aliases.ToLower().Contains(command.ToLower()))) return h;
            }

            return null;
        }

        public static string ExtractCommand(DiscordMessage message)
        {
            if (!message.Content.StartsWith(Configs.BotConfig.Prefix))
                return "";
            string[] array = message.Content.Remove(message.Content.IndexOf(Configs.BotConfig.Prefix), 1).Split(' ');
            if (array.Length < 1)
                return "";
            return array[0].ToUpper();
        }

        public static string[] ExtractArguments(DiscordMessage message)
        {
            if (!message.Content.StartsWith(Configs.BotConfig.Prefix))
                return new string[] { "" };
            string[] array = message.Content.Remove(message.Content.IndexOf(Configs.BotConfig.Prefix), 1).Split(' ');
            if (array.Length < 1)
                return array;
            return array.Skip(1).ToArray();
        }

        public static string CallCommand(DiscordMessage message, DiscordMember user)
        {
            if (!message.Content.StartsWith(Configs.BotConfig.Prefix))
                return "";
            string[] array = message.Content.Remove(message.Content.IndexOf(Configs.BotConfig.Prefix), 1).Split(' ');
            if (array.Length < 1)
                return "";
            string command = array[0].ToUpper();
            if (string.IsNullOrEmpty(command))
                return "";
            string[] args = array.Skip(1).ToArray();
            ICommandHandler handler = GetCommandHandler(command);
            if (handler != null)
            {
                if (!message.Author.Id.ToString().GetUser().IsAllowed(handler.PermType))
                {
                    return $":octagonal_sign: **Permission denied.**";
                }

                if (args.Count() > 0)
                {
                    Logger.Debug($"[CMDMANAGER] Calling command \"{command}\" with arguments \"{args.Combine()}\"");
                }
                else
                {
                    Logger.Debug($"[CMDMANAGER] Calling command \"{command}\"");
                }

                try
                {
                    return handler.Execute(user, message, args);
                }
                catch (System.Exception e)
                {
                    Logger.Exception(e);
                    return $"```csharp\n{e}\n```";
                }
            }
            return "";
        }

        public static Task RegisterCommands()
        {
            Register(new CommandHelp());
            Register(new CommandAddMod());
            Register(new CommandDelMod());
            Register(new CommandPrefix());
            Register(new CommandAddMaster());
            Register(new CommandDelMaster());
            Register(new CommandKick());
            Register(new CommandStatus());
            Register(new CommandSeek());
            Register(new CommandVolume());
            Register(new CommandPause());
            Register(new CommandResume());
            Register(new CommandNowPlaying());
            Register(new CommandClear());
            Register(new CommandStop());
            Register(new CommandLeave());
            Register(new CommandQueue());
            Register(new CommandSkip());
            Register(new CommandPlay());
            Register(new CommandJoin());
            Register(new CommandRemove());
            Register(new CommandDownload());
            Register(new CommandMute());
            Register(new CommandUnmute());
            Register(new CommandBan());
            Register(new CommandUnban());
            Register(new CommandReload());
            Register(new CommandPurge());
            Register(new CommandLock());
            Register(new CommandUnlock());
            Register(new CommandWarn());
            Register(new CommandSay());
            Register(new CommandEmbed());
            Register(new CommandAvatar());
            Register(new CommandBotInfo());
            Register(new CommandUserInfo());
            Register(new CommandServerInfo());
            Register(new CommandAddRank());
            Register(new CommandRemoveRank());
            Register(new CommandModifyRent());
            Register(new CommandRent());
            Register(new CommandLockdown());
            Register(new CommandRandomJoke());
            Register(new CommandLevels());
            Register(new CommandRank());
            Register(new CommandLevelManager());
            Register(new CommandRoleInfo());
            Register(new CommandChannelInfo());
            Register(new CommandMeme());
            Register(new CommandInsult());
            Register(new CommandYoMama());
            Register(new CommandFriends());
            Register(new CommandFact());
            Register(new CommandMembers());
            Register(new CommandRegionInfo());
            Register(new CommandVoiceInfo());
            Register(new CommandModerators());
            Register(new CommandRandomEmote());
            Register(new CommandEmoteInfo());
            Register(new CommandVerify());
            Register(new CommandSuggestions());
            Register(new CommandSuggest());
            Register(new CommandAutomod());
            Register(new CommandAutoMeme());
            Register(new CommandFactOfTheDay());
            Register(new CommandEditSuggestion());
            Register(new CommandGiveaways());
            Register(new CommandGiveaway());
            Register(new CommandConfig());
            Register(new CommandReroll());
            Register(new CommandUptime());
            Register(new CommandWarns());
            Register(new CommandInfoEmbed());
            Register(new CommandAddRadio());
            Register(new CommandRadio());
            Register(new CommandRandomColor());
            Register(new CommandLavalinkStatistics());
            Register(new CommandHostInfo());
            Register(new CommandReactionRoles());
            Register(new CommandRolePersist());
            Register(new CommandSelfRole());
            Register(new CommandRole());
            Register(new CommandRoles());
            Register(new CommandGuildRoles());
            Register(new CommandIgnore());
            Register(new CommandChangeRole());
            Register(new CommandRps());
            Register(new CommandRandomDog());
            Register(new CommandRoll());
            return Task.CompletedTask;
        }
    }

    public interface ICommandHandler
    {
        string Command { get; }
        string Aliases { get; }
        string Usage { get; }
        string Description { get; }
        PermType PermType { get; }
        string Execute(DiscordMember sender, DiscordMessage cmdMsg, string[] args);
    }
}
