using DSharpPlus.Entities;
using System.Threading.Tasks;
using Multiverse.API;

namespace Multiverse
{
    public static class CommandHandler
    {
        public static async Task ProcessCommand(DiscordMessage message, DiscordMember usr)
        {
            try
            {
                string reply = CommandManager.CallCommand(message, usr);
                if (!string.IsNullOrEmpty(reply))
                {
                    await message.Channel.SendMessageAsync(ClassBuilder.BuildEmbed($"{message.Author.Username}#{message.Author.Discriminator}", "", usr.AvatarUrl, "", reply, "", "", "", null, "", "", Helper.GetRandomColor(), false));
                    Helper.DeleteAfterUsage(CommandManager.GetCommandHandler(CommandManager.ExtractCommand(message)), message);
                    return;
                }
                await Task.CompletedTask;
            }
            catch (System.Exception e)
            {
                Logger.Exception(e);
                string reply = $"```csharp\n{e}\n```";
                await message.Channel.SendMessageAsync(ClassBuilder.BuildEmbed($"{message.Author.Username}#{message.Author.Discriminator}", "", usr.AvatarUrl, "", reply, "", "", "", null, "", "", DiscordColor.Red, false));
            }
        }
    }
}
