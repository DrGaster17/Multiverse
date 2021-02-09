using System.Threading.Tasks;
using Multiverse.API;
using System;
using DSharpPlus;
using DSharpPlus.Entities;

namespace Multiverse
{
	public class Bot
	{
		public static DiscordClient Client { get; internal set; }
		public static MusicHandler MusicHandler { get; internal set; }
		public static string Version => "v4.0.2";

		public Bot()
		{
			Configs.Reload();
			EventHandler.CreateClient();
			Start().GetAwaiter().GetResult();
		}

		public async Task Start()
		{
			try
			{
				Logger.Info($"Initializing Multiverse .. ({Version})");

				if (Configs.BotConfig.Token == "empty")
				{
					Logger.Error("You MUST set the token before starting!");
					await Task.Delay(5000);
					return;
				}

				MusicHandler = new MusicHandler();
				Client.MessageCreated += EventHandler.OnMessage;
				Client.GuildMemberAdded += EventHandler.OnUserJoin;
				Client.Ready += Client_Ready;
				Client.SocketClosed += EventHandler.OnDisconnect;
                Client.SocketErrored += EventHandler.OnDisconnect;
                Client.GuildAvailable += Client_GuildAvailable;
                Client.GuildDownloadCompleted += Client_GuildDownloadCompleted;
                Client.Heartbeated += Client_Heartbeated;
                Client.SocketOpened += Client_SocketOpened;
                Client.MessageReactionAdded += Client_MessageReactionAdded;
				Client.MessageReactionRemoved += Client_MessageReactionRemoved;
				AppDomain.CurrentDomain.ProcessExit += EventHandler.OnProcessExit;
				AppDomain.CurrentDomain.UnhandledException += EventHandler.OnException;
				Console.CancelKeyPress += EventHandler.OnCancelPress;
				Logger.Info("Connecting ...");
				await Client.ConnectAsync(new DiscordActivity
				{
					ActivityType = ActivityType.ListeningTo,
					Name = $"{Configs.BotConfig.Prefix}help",
				}, UserStatus.Idle);
				Logger.Info("Connected!");			
				await Client.InitializeAsync();
				await CommandManager.RegisterCommands();
				await EventHandler.StartUpdating();
				await Task.Delay(-1);
			}
			catch (Exception e)
			{
				Logger.Exception(e);
			}
		}

		private Task Client_MessageReactionRemoved(DiscordClient sender, DSharpPlus.EventArgs.MessageReactionRemoveEventArgs e) => ReactionRoles.Check(e.Message, e.User, e.Emoji, false);
		private Task Client_MessageReactionAdded(DiscordClient sender, DSharpPlus.EventArgs.MessageReactionAddEventArgs e) => ReactionRoles.Check(e.Message, e.User, e.Emoji, true);

        private Task Client_SocketOpened(DiscordClient sender, DSharpPlus.EventArgs.SocketEventArgs e)
        {
			Logger.Debug($"[WEBSOCKET] Connection opened!");
			return Task.CompletedTask;
        }

        private Task Client_Heartbeated(DiscordClient sender, DSharpPlus.EventArgs.HeartbeatEventArgs e)
        {
			Logger.Debug($"[CORE] Heartbeat received! {e.Ping}ms");
			return Task.CompletedTask;
        }

        private Task Client_GuildDownloadCompleted(DiscordClient sender, DSharpPlus.EventArgs.GuildDownloadCompletedEventArgs e)
        {
			Logger.Debug($"[CORE] Downloaded {e.Guilds.Count} guild(s)!");
			return Task.CompletedTask;
        }

        private Task Client_GuildAvailable(DiscordClient sender, DSharpPlus.EventArgs.GuildCreateEventArgs e)
        {
			Logger.Debug($"[CORE] {e.Guild.Name} is now available.");
			return Helper.Setup();
        }

        private async Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs e)
		{
			Logger.Info("Ready!");
			await Helper.Initialize();
		}
    }
}
