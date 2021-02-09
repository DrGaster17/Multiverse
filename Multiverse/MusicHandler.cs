using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using System.Collections.Generic;
using VideoLibrary;
using System.IO;
using System.Diagnostics;
using Lavalink4NET.Player;

namespace Multiverse.API
{
    public class MusicHandler 
    {
        public LavalinkPlayer GetPlayer(DiscordMessage message)
        {
            DiscordMember member = message.Author.Id.ToString().GetUser();
            if (Helper.CurrentChannel == null) Helper.Join(member.VoiceState.Channel, Helper.ConnectionType.Lavalink).GetAwaiter().GetResult();
            return Helper.LavalinkPlayer;
        }

        public async Task Seek(DiscordMessage message, int position)
        {
            var player = GetPlayer(message);
            if (player.CurrentTrack.IsSeekable)
            {
                await player.SeekPositionAsync(new TimeSpan(new DateTime(player.CurrentTrack.Duration.Ticks).AddSeconds(position).Ticks));
                await message.Channel.SendMessageAsync(ClassBuilder.BuildEmbed($"{message.Author.Username}#{message.Author.Discriminator}", "", message.Author.AvatarUrl, "", $":white_check_mark: Current position: **{player.CurrentTrack.Position.ToString("HH:mm:ss")}**", "", "", "", null, "", "", Helper.GetRandomColor(), false));
            }
            else
            {
                await message.Channel.SendMessageAsync(ClassBuilder.BuildEmbed($"{message.Author.Username}#{message.Author.Discriminator}", "", message.Author.AvatarUrl, "", $":x: Can't seek this track.", "", "", "", null, "", "", Helper.GetRandomColor(), false));
            }
        }

        public async Task SetVolume(DiscordMessage message, int value = 98450)
        {
            var player = GetPlayer(message);
            await player.SetVolumeAsync(value);
            await message.Channel.SendMessageAsync($":white_check_mark: **Volume was set to {value}**.");
        }

        public async Task Pause(DiscordMessage message)
        {
            var player = GetPlayer(message);
            await player.PauseAsync();
            await message.Channel.SendMessageAsync(":white_check_mark: **Paused**!");
        }

        public async Task Resume(DiscordMessage message)
        {
            var player = GetPlayer(message);
            await message.Channel.SendMessageAsync($":white_check_mark: **Resumed.**");
            await player.ResumeAsync();
        }

        public async Task NowPlaying(DiscordMessage message)
        {
            var player = GetPlayer(message);
            var playList = message.Channel.Id.GetServer().Id.PlayList();
            var my = $"**👉 {player.CurrentTrack.Title} [{player.CurrentTrack.Source}]**";
            if (playList.Any()) my += $"\n**Next: {playList[0].Title} [{playList[0].Source}]**";
            var build = new DiscordEmbedBuilder
            {
                Title = "Now playing",
                Description = my,
                Color = Helper.GetRandomColor()
            }.Build();
            await message.Channel.SendMessageAsync(build);
        }

        public async Task Clear(DiscordMessage message)
        {
            message.Channel.Id.GetServer().Id.PopAll();
            await message.Channel.SendMessageAsync(":white_check_mark: **Queue cleared!**");
        }

        public async Task Stop(DiscordMessage message, bool reply = true)
        {
            var player = GetPlayer(message);
            if (player.State == PlayerState.Playing)
                await player.StopAsync();
            if (reply)
                await message.Channel.SendMessageAsync($":white_check_mark: **Stopped playing.**");
        }

        public async Task Leave(DiscordMessage message)
        {
            Queue.PopAll(message.Channel.Id.GetServer().Id);
            await Helper.Disconnect();
        }

        public async Task QueueTask(DiscordMessage message)
        {
            var my = string.Empty;
            var p = message.Channel.Id.GetServer().Id.PlayList();
            var player = GetPlayer(message);
            if (!p.Any() && player.State != PlayerState.Playing)
            {
                await message.Channel.SendMessageAsync($":x: **The queue is empty**.");
            }
            else
            {
                if (player.State == PlayerState.Playing)
                    my += $"👉 [{player.CurrentTrack.Title}]({player.CurrentTrack.Source}) **{player.CurrentTrack.Duration.ToString("HH:mm:ss")}**\n";
                for (var i = 0; i < Math.Min(p.Count, 10); i++)
                    my += $"**{i + 1}**. [{p[i].Title}]({p[i].Source}) **{p[i].Duration.ToString("HH:mm:ss")}**\n";
                var build = new DiscordEmbedBuilder
                {
                    Title = "Current queue",
                    Description = my,
                    Color = Helper.GetRandomColor(),
                }.Build();
                await message.Channel.SendMessageAsync(build);
            }
        }

        public async Task SkipTask(DiscordMessage message)
        {
            var player = GetPlayer(message);
            var final = await message.Channel.SendMessageAsync(":arrows_counterclockwise: **Searching ...**");
            try
            {
                var track = message.Channel.Id.GetServer().Id.PopTrack();
                var playing = new DiscordEmbedBuilder
                {
                    Title = "Now playing",
                    Description = $"**Title**: {track.Title}\n**URL**: {track.Source}",
                    Color = Helper.GetRandomColor()
                }.Build();

                await player.StopAsync();
                await player.PlayAsync(track);
                await final.ModifyAsync(null, playing);
            }
            catch (Exception)
            {
                await player.StopAsync();
            }
        }

        public async Task PlayTask(DiscordMessage message, string query)
        {
            var player = GetPlayer(message);
            var tracks = await Helper.LavalinkClient.GetTracksAsync($"ytsearch:{query}");
            if (tracks.Count() < 1)
            {
                await message.Channel.SendMessageAsync($":x: The search found no such tracks.");
                return;
            }

            var track = tracks.FirstOrDefault();

            if (player.State == PlayerState.Playing)
            {
                message.Channel.GuildId.PushTrack(track);
                await message.Channel.SendMessageAsync($":white_check_mark: Added **{track.Title}** to the queue!");
            }
            else
            {
                await player.PlayAsync(track);
                await message.Channel.SendMessageAsync($":white_check_mark: Now playing **{track.Title}**!");
            }
        }
    }

    public static class Queue
    {
        private static readonly Dictionary<ulong, Queue<LavalinkTrack>> _queue = new Dictionary<ulong, Queue<LavalinkTrack>>();

        public static void PushTrack(this ulong guildId, LavalinkTrack track)
        {
            _queue.TryAdd(guildId, new Queue<LavalinkTrack>());
            _queue[guildId].Enqueue(track);
        }

        public static string RemoveTrack(this ulong guildId, int index)
        {
            _queue.TryAdd(guildId, new Queue<LavalinkTrack>());
            List<LavalinkTrack> tracks = _queue[guildId].ToList();
            LavalinkTrack trackToRemove = tracks[index];
            Queue<LavalinkTrack> newQueue = new Queue<LavalinkTrack>();
            tracks.RemoveAt(index);
            foreach (LavalinkTrack track in tracks)
                newQueue.Enqueue(track);
            _queue.Remove(guildId);
            _queue.Add(guildId, newQueue);
            return $":white_check_mark: **Succesfully removed {trackToRemove.Title} [{trackToRemove.Source}] from the queue!**";
        }

        public static LavalinkTrack PopTrack(this ulong guildId)
        {
            _queue.TryAdd(guildId, new Queue<LavalinkTrack>());
            if (!_queue[guildId].Any())
                return null;
            return _queue[guildId].Dequeue();
        }

        public static void PopAll(this ulong guildId)
        {
            _queue.TryAdd(guildId, new Queue<LavalinkTrack>());
            _queue[guildId].Clear();
        }

        public static List<LavalinkTrack> PlayList(this ulong guildId)
        {
            _queue.TryAdd(guildId, new Queue<LavalinkTrack>());
            return _queue[guildId].ToList();
        }
    }

    public static class Downloader
    {
        public static string DownloadPath = $"{Directory.GetCurrentDirectory()}/Downloads";
        public static string FFMPEG = $"{Directory.GetCurrentDirectory()}/ffmpeg";

        public static YouTubeVideo GetVideo(string search)
        {
            try
            {
                YouTube youTube = YouTube.Default;
                var response = Helper.LavalinkClient.GetTracksAsync($"ytsearch:{search}").GetAwaiter().GetResult();
                if (response.Count() < 1) return null;
                var track = response.FirstOrDefault();
                YouTubeVideo video = youTube.GetVideo(track.Source);
                return video;
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return null;
            }
        }

        public static Result Download(YouTubeVideo video)
        {
            try
            {
                if (!Directory.Exists(DownloadPath))
                    Directory.CreateDirectory(DownloadPath);
                string vid = video.FullName;
                string fullName = video.FullName.Replace(".mp4", "");
                string videoName = video.FullName.Replace(" ", "").Replace("\"", "");
                string path = $"{DownloadPath}/{videoName}";
                if (File.Exists(path))
                    File.Delete(path);
                Result result = new Result(path, videoName, vid, fullName, video.Uri);
                File.WriteAllBytes(result.Path, video.GetBytes());
                result.ResultPath = $"{DownloadPath}/{fullName}.mp3";
                var ffmpegProcess = new Process();
                ffmpegProcess.StartInfo.UseShellExecute = false;
                ffmpegProcess.StartInfo.RedirectStandardInput = true;
                ffmpegProcess.StartInfo.RedirectStandardOutput = true;
                ffmpegProcess.StartInfo.RedirectStandardError = true;
                ffmpegProcess.StartInfo.CreateNoWindow = true;
                ffmpegProcess.StartInfo.FileName = FFMPEG;
                ffmpegProcess.StartInfo.Arguments = $"-i \"{result.Path}\" -vn -ar 44100 -ac 2 -b:a 192k \"{result.ResultPath}\"";
                ffmpegProcess.Start();
                ffmpegProcess.StandardOutput.ReadToEnd();
                string output = ffmpegProcess.StandardError.ReadToEnd();
                ffmpegProcess.WaitForExit();
                if (!ffmpegProcess.HasExited)
                {
                    ffmpegProcess.Kill();
                }
                Logger.Debug(output);
                if (!File.Exists(result.ResultPath))
                    return null;
                return result;
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                return null;
            }
        }

        public static void Upload(DiscordChannel channel, Result result)
        {
            try
            {
                FileStream stream = new FileStream(path: result.ResultPath, mode: FileMode.Open);
                channel.SendMessageAsync(new DiscordMessageBuilder().WithFile(result.ResultPath, stream));
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }

        public class Result
        {
            public string Path { get; set; }
            public string VideoName { get; set; }
            public string Video { get; set; }
            public string FullName { get; set; }
            public string Url { get; set; }
            public string ResultPath { get; set; }

            public Result(string path, string videoName, string video, string fullname, string url)
            {
                Path = path;
                Url = url;
                VideoName = videoName;
                Video = video;
                FullName = fullname;
            }
        }
    }
}
