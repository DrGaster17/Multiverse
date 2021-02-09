using System;
using System.Collections.Generic;
using System.Linq;
using DSharpPlus;
using DSharpPlus.Entities;

namespace Multiverse.API
{
    public static class Extensions
    {
        public static int GetDistance(this string firstString, string secondString)
        {
            int n = firstString.Length;
            int m = secondString.Length;
            int[,] d = new int[n + 1, m + 1];
            if (n == 0)
                return m;
            if (m == 0)
                return n;
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }
            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (secondString[j - 1] == firstString[i - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }

        public static string RemoveNumbers(this string arg)
        {
            string s = "";
            foreach (char c in arg)
            {
                if (ulong.TryParse(c.ToString(), out ulong num))
                    continue;
                else
                    s += c.ToString();
            }
            return s;
        }

        public static int GetCaps(this string str)
        {
            int caps = AutoModerator.Regex.Replace(str, "").Length;
            return caps;
        }

        public static bool IsClose(this DateTime end, DateTime now)
        {
            long ticks = now.Ticks;
            for (long i = 0; i < 5; i++)
            {
                if (end.Ticks + i == ticks || end.Ticks - i == ticks || ((end.Day == now.Day && end.Month == now.Month && end.Year == now.Year && end.Month == now.Month) && (end.Second + i == now.Second || end.Second - i == now.Second)))
                    return true;
            }
            return false;
        }

        public static DiscordInvite GetPermanentInvite(this DiscordGuild guild)
        {
            IReadOnlyCollection<DiscordInvite> invites = guild.GetInvitesAsync().GetAwaiter().GetResult();
            foreach (DiscordInvite invite in invites)
            {
                if (!invite.IsTemporary)
                    return invite;
            }
            return null;
        }

        public static int GetMentions(this string str)
        {
            int index = 0;
            str = str.Trim();
            str = str.Replace('>', ' ');
            string[] arr = str.Split(' ');
            foreach (string arg in arr)
            {
                if (arg.IsMention())
                    index++;
            }
            return index;
        }

        public static bool ContainsInvite(this string arg)
        {
            string[] args = arg.Trim().Split(' ');
            foreach (string str in args)
                if (str.StartsWith("discord.gg/") || str.StartsWith("https://discord.gg"))
                {
                    return true;
                }
            return false;
        }

        public static bool ContainsLink(this string arg)
        {
            string[] args = arg.Trim().Split(' ');
            foreach (string str in args)
                if (str.StartsWith("https://www.") || str.StartsWith("http://www.") || str.StartsWith("https://") || str.StartsWith("http://"))
                {
                    return true;
                }
            return false;
        }

        public static bool IsMention(this string arg)
        {
            if (arg.Contains("<") && arg.Contains("!") && arg.Contains("@"))
            {
                if (ulong.TryParse(arg.RemoveCharacters(), out ulong id))
                {
                    Commands.CommandModerators.ObjectType type = Commands.CommandModerators.Get(id.ToString(), out DiscordMember usr, out DiscordRole r, out DiscordChannel channel);
                    if (type == Commands.CommandModerators.ObjectType.Role || type == Commands.CommandModerators.ObjectType.User)
                        return true;
                }
            }
            return false;
        }

        public static string RemoveCharacters(this string arg)
        {
            string s = "";
            foreach (char c in arg)
            {
                if (!ulong.TryParse(c.ToString(), out ulong num))
                    continue;
                else
                    s += num.ToString();
            }
            return s;
        }

        public static string Combine(this string[] array)
        {
            string msg = "";
            foreach (string s in array)
                msg += $"{s} ";
            if (msg.Contains(' '))
                msg = msg.Remove(msg.LastIndexOf(' '));
            return msg;
        }

        public static DiscordMessage GetMessage(this ulong id)
        {
            foreach (DiscordGuild guild in Bot.Client.Guilds.Values)
            {
                foreach (DiscordChannel channel in guild.GetTextChannels())
                {
                    return channel.GetMessageAsync(id).GetAwaiter().GetResult();
                }
            }

            return null;
        }

        public static List<DiscordChannel> GetVoiceChannels(this DiscordGuild guild) => guild.Channels.Where(h => h.Value.Type == ChannelType.Voice).Select(h => h.Value).ToList();
        public static List<DiscordChannel> GetTextChannels(this DiscordGuild guild) => guild.Channels.Where(h => h.Value.Type == ChannelType.Text).Select(h => h.Value).ToList();
        public static List<DiscordChannel> GetCategories(this DiscordGuild guild) => guild.Channels.Where(h => h.Value.Type == ChannelType.Category).Select(h => h.Value).ToList();

        public static List<DiscordChannel> GetChannelsByType(this DiscordGuild guild, ChannelType type) => guild.Channels.Where(h => h.Value.Type == type).Select(h => h.Value).ToList();

        public static bool IsCommand(this DiscordMessage message)
        {
            try
            {
                int index = message.Content.IndexOf(Configs.BotConfig.Prefix, 1);
                if (index < 1)
                    return false;
                string[] array = message.Content.Remove(index).Split(' ');
                if (array.Length < 1)
                    return false;
                string command = array[0].ToUpper();
                string[] args = array.Skip(1).ToArray();
                ICommandHandler handler = CommandManager.GetCommandHandler(command);
                if (handler == null)
                    return false;
                else
                    return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string GetAvatar(this DiscordUser user) => user.AvatarUrl;

        public static string GetDiscriminator(this DiscordUser usr) => usr.Id == 732565345604337684 ? "" : usr.Discriminator;
        public static string GetAuthor(this DiscordUser usr) => $"{(usr.Id == 732565345604337684 ? "" : $"{usr.Username}#{usr.GetDiscriminator()}")}";

        public static DiscordEmbed GetInfo(this DiscordMember usr)
        {
            Field joinDate = new Field(true, "Joined", $"{Timer.TimeToString(usr.JoinedAt.UtcDateTime)} [{usr.JoinedAt.Calculate().Days} days ago!]");
            Field registerDate = new Field(true, "Registered", $"{Timer.TimeToString(usr.CreationTimestamp.UtcDateTime)} [{usr.CreationTimestamp.Calculate().Days} days ago!]");
            Field userId = new Field(true, "UserID", usr.Id.ToString());
            Field roles = new Field(true, $"Roles [{usr.Roles.Where(h => h.Name != "everyone").Count()}]", usr.RolesToString());
            return ClassBuilder.BuildEmbed($"{usr.Username}#{usr.Discriminator}", "", usr.GetAvatar(), "", "", "", "", "", new List<Field>() { joinDate, registerDate, userId, roles }, $"Possible alt: {usr.IsPossibleAlt()}", "", Helper.GetRandomColor(), false);
        }

        public static bool IsPossibleAlt(this DiscordMember usr)
        {
            TimeSpan span = new TimeSpan(usr.CreationTimestamp.Ticks);
            if (string.IsNullOrEmpty(usr.GetAvatarUrl(ImageFormat.Auto, 2048)) || span.TotalDays < 7)
                return true;
            return false;
        }

        public static DiscordEmbed GetInfo(this DiscordGuild g)
        {
            Field owner = new Field(true, "Owner", g.Owner.Mention);
            Field region = new Field(true, "Voice Region", g.VoiceRegion.Name);
            Field categories = new Field(true, "Categories", g.Channels.Where(h => h.Value.IsCategory).Count().ToString());
            Field textChannels = new Field(true, "Text Channels", g.Channels.Where(h => h.Value.Type == ChannelType.Text).Count().ToString());
            Field voiceChannels = new Field(true, "Voice Channels", g.Channels.Where(h => h.Value.Type == ChannelType.Voice).Count().ToString());
            Field members = new Field(true, "Members", g.Members.Where(h => !h.Value.IsBot).Count().ToString());
            Field bots = new Field(true, "Bots", g.Members.Where(h => h.Value.IsBot).Count().ToString());
            Field roles = new Field(true, "Roles", g.Roles.Count.ToString());
            Field id = new Field(true, "ServerID", g.Id.ToString());
            Field creationDate = new Field(true, "Created", $"{Timer.TimeToString(g.CreationTimestamp.UtcDateTime)} [{(DateTime.UtcNow - g.CreationTimestamp).Days} days ago!]");
            Field bans = new Field(true, "Bans", g.GetBansAsync().GetAwaiter().GetResult().Count.ToString());
            return ClassBuilder.BuildEmbed(g.Name, "", g.IconUrl, "", "", "", "", "", new List<Field>() { owner, region, categories, textChannels, voiceChannels, members, bots, roles, bans, id, creationDate }, "", "", Helper.GetRandomColor(), false);
        }

        public static DiscordEmbed GetInfo(this DiscordEmoji emote)
        {
            Field animated = new Field("Animated", emote.IsAnimated);
            Field created = new Field("Created", emote.CreationTimestamp.GetString());
            Field id = new Field("ID", emote.Id);
            Field name = new Field("Name", emote.Name);
            return ClassBuilder.BuildEmbed("", "", "", "", "", "", "", emote.Url, new List<Field>() { name, id, animated, created }, "", "", Helper.GetRandomColor(), false);
        }

        public static DiscordEmbed GetInfo(this DiscordVoiceRegion reg)
        {
            Field name = new Field("Name", reg.Name);
            Field id = new Field("ID", reg.Id);
            Field custom = new Field("Custom", reg.IsCustom);
            Field deprecated = new Field("Deprecated", reg.IsDeprecated);
            Field optimal = new Field("Optimal", reg.IsOptimal);
            Field vip = new Field("VIP", reg.IsVIP);
            return ClassBuilder.BuildEmbed("", "", "", "", "", "", "", "", new List<Field>() { name, id, custom, deprecated, optimal, vip }, "", "", Helper.GetRandomColor(), false);
        }

        public static DiscordEmbed GetBotInfo(DiscordClient client)
        {
            DiscordUser usr = client.CurrentUser;
            DiscordUser owner = client.GetUserAsync(732565345604337684).GetAwaiter().GetResult();
            Field creator = new Field(true, "Creator", $"{(owner == null ? "Dr. Gaster#6385" : owner.Mention)}");
            Field latency = new Field(true, "Latency", $"{client.Ping} ms");
            Field version = new Field(true, "Version", Bot.Version);
            Field shard = new Field(true, "ShardID", client.ShardId.ToString());
            Field guilds = new Field(true, "Guilds", client.Guilds.Count);
            Field groupChannels = new Field("Group Channels", client.PrivateChannels.Count);
            return ClassBuilder.BuildEmbed($"{usr.Username}#{usr.Discriminator}", "", usr.GetAvatar(), "", "", "", "", "", new List<Field>() { creator, latency, version, shard, groupChannels, guilds }, "", "", Helper.GetRandomColor(), false);
        }

        public static DiscordEmbed GetInfo(this DiscordRole role)
        {
            Field mention = new Field(true, "Mention", role.Mention);
            Field id = new Field(true, "RoleID", role.Id.ToString());
            Field color = new Field(true, "Color", $"{role.Color}");
            Field createdAt = new Field(true, "Created", $"{Timer.TimeToString(role.CreationTimestamp.UtcDateTime)} [{role.CreationTimestamp.Calculate().Days} days ago!]");
            Field members = new Field(true, "Members", Bot.Client.Guilds.First().Value.Members.Where(h => h.Value.Roles.Select(he => he.Id).Contains(role.Id)).Count().ToString());
            Field position = new Field(true, "Position", role.Position.ToString());
            return ClassBuilder.BuildEmbed(role.Name, "", "", "", "", "", "", "", new List<Field>() { mention, id, color, createdAt, members, position }, "", "", Helper.GetRandomColor(), false);
        }

        public static DiscordEmbed GetInfo(this DiscordChannel channel)
        {
            Field mention = new Field(true, "Mention", channel.Mention);
            Field category = new Field(true, "Category", channel.Parent.Name);
            Field position = new Field(true, "Position", channel.Position.ToString());
            Field users = new Field(true, "Users", channel.Users.Count().ToString());
            Field channelId = new Field(true, "ChannelID", channel.Id.ToString());
            Field createdAt = new Field(true, "Created", $"{Timer.TimeToString(channel.CreationTimestamp.UtcDateTime)} [{channel.CreationTimestamp.Calculate().Days} days ago!]");
            return ClassBuilder.BuildEmbed("", "", "", channel.Name, "", "", "", "", new List<Field>() { mention, category, position, users, channelId, createdAt }, "", "", Helper.GetRandomColor(), false);
        }

        public static DiscordEmbed GetInfoVoice(this DiscordChannel channel)
        {
            Field bitrate = new Field("Bitrate", $"{channel.Bitrate} kbps");
            Field category = new Field("Category", channel.Parent.Name);
            Field created = new Field("Created", channel.CreationTimestamp.GetString());
            Field id = new Field("ID", channel.Id);
            Field name = new Field("Name", channel.Name);
            Field position = new Field("Position", channel.Position);
            Field limit = new Field("UserLimit", $"{(channel.UserLimit < 1 ? "Unlimited" : $"{channel.UserLimit} user(s)")}");
            Field users = new Field("Users", $"{channel.Users.Count()} user(s)");
            return ClassBuilder.BuildEmbed("", "", "", "", "", "", "", "", new List<Field>() { bitrate, category, created, id, name, position, limit, users }, "", "", Helper.GetRandomColor(), false);
        }

        public static string GetString(this DateTimeOffset time)
        {
            return $"{Timer.TimeToString(time.UtcDateTime)} [{time.Calculate().Days} days ago!]";
        }

        public static DiscordEmbed GetMembersEmbed(this DiscordRole role)
        {
            string s = $"{role.Mention}\n\n";
            IEnumerable<DiscordMember> members = role.GetMembers().OrderByDescending(h => h.Roles.First().Position).Reverse();
            foreach (DiscordMember usr in members)
            {
                if (s.Length >= 2048 || (s + usr.Mention).Length >= 2048)
                    continue;
                s += $"{usr.Mention}\n";
            }
            return ClassBuilder.BuildEmbed("", "", "", $"Members ({role.GetMembers().Count()}):", s, "", "", "", null, "", "", Helper.GetRandomColor(), false);
        }

        public static List<DiscordMember> GetMembers(this DiscordRole role) => Bot.Client.Guilds.First().Value.Members.Where(h => h.Value.Roles.Select(he => he.Id).Contains(role.Id)).Select(e => e.Value).ToList();

        public static TimeSpan Calculate(this DateTimeOffset off) => DateTime.UtcNow - off.UtcDateTime;
        public static TimeSpan Calculate(this DateTimeOffset? off) => DateTime.UtcNow - off.Value.UtcDateTime;

        public static string ToString<T>(this IEnumerable<T> ts)
        {
            string s = "";
            foreach (T t in ts)
            {
                if (string.IsNullOrEmpty(s))
                    s += $"{t}";
                else
                    s += $" {t}";
            }
            return s;
        }

        public static DiscordChannel GetCurrentVoice(this DiscordUser user)
        {
            try
            {
                foreach (DiscordGuild guild in Bot.Client.Guilds.Values)
                {
                    List<DiscordChannel> VoiceChannels = guild.Channels.Where(h => h.Value.Type == ChannelType.Voice).Select(h => h.Value).ToList();
                    foreach (DiscordChannel voice in VoiceChannels)
                    {
                        if (voice.Users.Select(h => h.Id).Contains(user.Id))
                        {
                            return voice;
                        }
                    }
                }
                return null;
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return null;
            }
        }

        public static string RolesToString(this DiscordMember usr)
        {
            string s = "";
            List<DiscordRole> roles = usr.Roles.ToList();
            roles = roles.OrderByDescending(h => h.Position).ToList();
            foreach (DiscordRole role in roles)
            {
                if (role.Id == usr.Guild.EveryoneRole.Id)
                    continue;
                if (string.IsNullOrEmpty(s))
                    s += $"{role.Mention}";
                else
                    s += $" {role.Mention}";
            }
            return s;
        }

        public static DiscordChannel GetVoice(this string arg)
        {
            foreach (DiscordGuild guild in Bot.Client.Guilds.Values)
            {
                foreach (DiscordChannel channel in guild.Channels.Where(h => h.Value.Type == ChannelType.Voice).Select(h => h.Value))
                {
                    arg = arg.Replace("<", "").Replace("#", "").Replace(">", "");
                    if (ulong.TryParse(arg, out ulong id))
                    {
                        if (channel.Id == id)
                            return channel;
                    }
                    else
                    {
                        if (channel.Name.ToLower().Contains(arg.ToLower()) || channel.Name.ToLower() == arg.ToLower())
                            return channel;
                    }
                }
            }
            return null;
        }

        public static DiscordChannel GetTextChannel(this string arg)
        {
            foreach (DiscordGuild guild in Bot.Client.Guilds.Values)
            {
                foreach (DiscordChannel channel in guild.Channels.Where(h => h.Value.Type == ChannelType.Text).Select(h => h.Value))
                {
                    arg = arg.Replace("<", "").Replace("#", "").Replace(">", "");
                    if (ulong.TryParse(arg, out ulong id))
                    {
                        if (channel.Id == id)
                            return channel;
                    }
                    else
                    {
                        if (channel.Name.ToLower().Contains(arg.ToLower()) || channel.Name.ToLower() == arg.ToLower())
                            return channel;
                    }
                }
            }
            return null;
        }

        public static DiscordRole GetRole(this ulong id)
        {
            foreach (DiscordGuild guild in Bot.Client.Guilds.Values)
            {
                foreach (DiscordRole role in guild.Roles.Values)
                {
                    if (role.Id == id)
                        return role;
                }
            }
            return null;
        }

        public static DiscordColor GetColor(this string str)
        {
            str = str.ToLower();
            foreach (DiscordColor color in Helper.AllColors)
            {
                if (color.ToString().ToLower() == str)
                    return color;
            }
            if (int.TryParse(str, out int raw))
                return new DiscordColor(raw);
            string[] args = str.Split(',');
            if (!byte.TryParse(args[0], out byte r))
                r = 0;
            if (!byte.TryParse(args[1], out byte g))
                g = 0;
            if (!byte.TryParse(args[2], out byte b))
                b = 0;
            if (r > 0 && g > 0 && b > 0)
                return new DiscordColor(r, g, b);
            return Helper.GetRandomColor();
        }

        public static DiscordChannel GetCurrentVoice(this DiscordMember user)
        {
            try
            {
                foreach (DiscordGuild guild in Bot.Client.Guilds.Values)
                {
                    List<DiscordChannel> VoiceChannels = guild.Channels.Where(h => h.Value.Type == ChannelType.Voice).Select(h => h.Value).ToList();
                    foreach (DiscordChannel voice in VoiceChannels)
                    {
                        if (voice.Users.Select(h => h.Id).Contains(user.Id))
                        {
                            return voice;
                        }
                    }
                }
                return null;
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return null;
            }
        }

        public static DiscordMember GetUser(this string args)
        {
            args = args.Replace("<", "").Replace("@", "").Replace(">", "").Replace("!", "");
            if (ulong.TryParse(args, out ulong id))
            {
                foreach (DiscordGuild server in Bot.Client.Guilds.Values)
                {
                    foreach (DiscordMember idk in server.GetAllMembersAsync().GetAwaiter().GetResult())
                    {
                        if (idk.Id == id)
                        {
                            return idk;
                        }
                    }
                }
            }

            foreach (DiscordGuild server in Bot.Client.Guilds.Values)
            {
                foreach (DiscordMember idk in server.Members.Values)
                {
                    if (idk.Username == args || idk.Username.Contains(args) || idk.Id.ToString() == args || idk.Discriminator == args)
                        return idk;
                    if (idk.Username.ToLower() == args.ToLower() || idk.Username.ToLower().Contains(args.ToLower()))
                        return idk;
                    if (!string.IsNullOrEmpty(idk.Nickname) && (idk.Nickname.ToLower() == args.ToLower() || idk.Nickname.ToLower().Contains(args.ToLower())))
                        return idk;
                }
            }
            return null;
        }

        public static DiscordGuild GetServer(this ulong id)
        {
            foreach (DiscordGuild guild in Bot.Client.Guilds.Values)
            {
                if (guild.Id == id)
                    return guild;
                if (guild.Channels.Select(h => h.Value.Id).Contains(id))
                    return guild;
            }
            return null;
        }

        public static DiscordRole GetRole(this string args)
        {
            args = args.Replace("<", "").Replace("@", "").Replace(">", "").Replace("&", "");

            if (ulong.TryParse(args, out ulong id))
            {
                foreach (DiscordGuild server in Bot.Client.Guilds.Values)
                {
                    foreach (DiscordRole idk in server.Roles.Values)
                    {
                        if (idk.Id == id)
                            return idk;
                    }
                }
            }

            foreach (DiscordGuild server in Bot.Client.Guilds.Values)
            {
                foreach (DiscordRole idk in server.Roles.Values)
                {
                    if (idk.Name.Contains(args) || args.Contains(idk.Name) || args == idk.Name || idk.Id.ToString() == args)
                        return idk;
                    if (idk.Name.ToLower().Contains(args) || idk.Name.ToLower() == args.ToLower())
                        return idk;
                }
            }
            return null;
        }

        public static bool IsAllowed(this DiscordMember user, PermType permType)
        {
            if (permType == PermType.Custom)
                return true;
            if (permType == PermType.Everyone)
                return true;
            List<ulong> staffIds = Configs.StaffConfig.StaffIds;
            List<ulong> masterIds = Configs.StaffConfig.BotMasters;
            foreach (DiscordGuild guild in Bot.Client.Guilds.Values)
            {
                foreach (DiscordRole role in guild.Roles.Values)
                {
                    if (role.Permissions.HasPermission(Permissions.Administrator) && user.HasRole(role))
                        return true;
                    if (staffIds.Contains(role.Id) && user.HasRole(role) && permType == PermType.Moderator)
                        return true;
                    if (masterIds.Contains(role.Id) && user.HasRole(role) && permType == PermType.Moderator)
                        return true;
                    if (masterIds.Contains(role.Id) && user.HasRole(role) && permType == PermType.BotMaster)
                        return true;
                    if (masterIds.Contains(user.Id) && permType == PermType.BotMaster)
                        return true;
                    if (masterIds.Contains(user.Id) && permType == PermType.Moderator)
                        return true;
                    if (staffIds.Contains(user.Id) && permType == PermType.Moderator)
                        return true;
                }
            }
            return false;
        }

        public static bool HasRole(this DiscordMember user, DiscordRole role) => role.GetMembers().Select(h => h.Id).Contains(user.Id);
    }
}
