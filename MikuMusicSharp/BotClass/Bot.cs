using DSharpPlus;
using DSharpPlus.Interactivity;
using DSharpPlus.CommandsNext;
using DSharpPlus.Lavalink;
using System;
using System.Linq;
//using NYoutubeDL;
using System.Threading.Tasks;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using System.Threading;
using DSharpPlus.Net.Udp;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;

namespace MikuMusicSharp
{
    public class Bot : IDisposable
    {
        private DiscordShardedClient bot;
        public LavalinkNodeConnection LavalinkNode { get; private set; }
        private CancellationTokenSource _cts;
        public static LavalinkConfiguration lcfg = new LavalinkConfiguration
        {
            SocketEndpoint = new ConnectionEndpoint { Hostname = "localhost", Port = 8090 },
            RestEndpoint = new ConnectionEndpoint { Hostname = "localhost", Port = 2336 }
        };
        public static List<Gsets> guit = new List<Gsets>();
        public static LavalinkNodeConnection llcon = null;

        public Bot()
        {
            bot = new DiscordShardedClient(new DiscordConfiguration()
            {
                LogLevel = LogLevel.Debug,
                TokenType = TokenType.Bot,
                AutomaticGuildSync = true,
                UseInternalLogHandler = true,
                AutoReconnect = true
            });

            _cts = new CancellationTokenSource();
            bot.Ready += OnReadyAsync;
            //bot.GuildDownloadCompleted += FeedClients;
        }

        public async Task RunAsync()
        {
            await bot.StartAsync();

            var commands = await bot.UseCommandsNextAsync(new CommandsNextConfiguration()
            {
                StringPrefixes = (new[] { "m%" }),
                EnableDefaultHelp = false,
                IgnoreExtraArguments = false,
                CaseSensitive = false
            });

            var interactivity = await bot.UseInteractivityAsync(new InteractivityConfiguration { });

            var llink = await bot.UseLavalinkAsync();

            llcon = await llink.First().Value.ConnectAsync(lcfg);
            if (llcon.IsConnected)
            {
                Console.WriteLine("LL connected");
            }

            foreach (var vmd in commands)
            {
                vmd.Value.RegisterCommands<Commands.Voice>();
                vmd.Value.CommandErrored += Bot_CMDErr;
            }

            bot.ClientErrored += this.Bot_ClientErrored;

            guit.Add(new Gsets
            {
                GID = 0,
                prefix = new List<string>(new string[] { "m%" }),
                queue = new List<Gsets2>(),
                playnow = new Gsets3(),
                repeat = false,
                offtime = DateTime.Now,
                timeout = false,
                shuffle = false,
                LLinkCon = llcon,
                LLGuild = null,
                playing = false,
                rAint = 0,
                repeatAll = false,
                alone = false,
                paused = true,
                stoppin = false
            });

            foreach (var shard in bot.ShardClients)
            {
                shard.Value.VoiceStateUpdated += async e =>
                {
                    try
                    {
                        var pos = guit.FindIndex(x => x.GID == e.Guild.Id);
                        if (pos != -1)
                        {
                            await Task.Delay(500);
                            if (guit[pos].LLGuild.Channel.Id == e.Before.Channel.Id)
                            {
                                if (guit[pos].LLGuild.Channel.Users.Where(x => x.IsBot == false).Count() == 0)
                                {
                                    guit[pos].alone = true;
                                }
                                else
                                {
                                    guit[pos].alone = false;
                                }
                                if (guit[pos].LLGuild.Channel.Users.Where(x => x.IsBot == false).Count() == 0 && guit[pos].queue.Count > 0 && guit[pos].LLGuild.Channel.Id == e.Before.Channel.Id && !guit[pos].paused)
                                {
                                    await e.Guild.GetChannel(guit[pos].cmdChannel).SendMessageAsync("Playback was paused since everybody left the voicechannel, use ``m!resume`` to unpause");
                                    guit[pos].LLGuild.Pause();
                                    guit[pos].paused = true;
                                }
                                handleVoidisc(pos);
                            }
                        }
                    }
                    catch
                    {
                        try
                        {
                            var pos = guit.FindIndex(x => x.GID == e.Guild.Id);
                            if (pos != -1)
                            {
                                handleVoidisc(pos);
                            }
                        }
                        catch { }
                    }
                    await Task.CompletedTask;
                };
                shard.Value.GuildCreated += async e =>
                {
                    var pos = guit.FindIndex(x => x.GID == e.Guild.Id);
                    if (pos == -1)
                    {
                        guit.Add(new Gsets
                        {
                            GID = e.Guild.Id,
                            prefix = new List<string>(new string[] { "m%" }),
                            queue = new List<Gsets2>(),
                            playnow = new Gsets3(),
                            repeat = false,
                            offtime = DateTime.Now,
                            timeout = false,
                            shuffle = false,
                            LLGuild = null,
                            playing = false,
                            rAint = 0,
                            repeatAll = false,
                            alone = false,
                            paused = true,
                            audioPlay = new Commands.Audio.Playback(),
                            audioFunc = new Commands.Audio.Functions(),
                            audioQueue = new Commands.Audio.Queue(),
                            audioEvents = new Commands.Audio.Events(),
                            stoppin = false
                        });
                    }
                    await Task.CompletedTask;
                };
            }

            bot.Ready += async e =>
            {
                DiscordActivity test = new DiscordActivity
                {
                    Name = "New Music System! || m%help",
                    ActivityType = ActivityType.Playing
                };
                await bot.UpdateStatusAsync(activity: test, userStatus: UserStatus.Online);
                await Task.Delay(500);
                try
                {
                    foreach (var shard in bot.ShardClients)
                    {
                        foreach (var guilds in shard.Value.Guilds)
                        {
                            guit.Add(new Gsets
                            {
                                GID = guilds.Value.Id,
                                prefix = new List<string>(new string[] { "%" }),
                                queue = new List<Gsets2>(),
                                playnow = new Gsets3(),
                                repeat = false,
                                offtime = DateTime.Now,
                                timeout = false,
                                shuffle = false,
                                LLGuild = null,
                                playing = false,
                                rAint = 0,
                                repeatAll = false,
                                alone = false,
                                paused = true,
                                audioPlay = new Commands.Audio.Playback(),
                                audioFunc = new Commands.Audio.Functions(),
                                audioQueue = new Commands.Audio.Queue(),
                                audioEvents = new Commands.Audio.Events(),
                                stoppin = false
                            });
                            if (guilds.Value.Id == 336039472250748928)
                            {
                                Console.WriteLine("Derpy guild");
                            }
                        }
                    }
                    Console.WriteLine("GuildList Complete");
                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }

            };

            await WaitForCancellationAsync();
        }

        private async Task WaitForCancellationAsync()
        {
            while (!_cts.IsCancellationRequested)
                await Task.Delay(500);
        }

        private Task<int> PreGet(DiscordMessage msg)
        {
            int pos = guit.FindIndex(x => x.GID == msg.Channel.GuildId);
            var wtf = msg.Content;
            if (pos != -1)
            {
                var multiprefloc = guit[pos].prefix.FindIndex(x => wtf.StartsWith(x));
                int prefloc;
                if (multiprefloc != -1)
                {
                    prefloc = msg.GetStringPrefixLength(guit[pos].prefix[multiprefloc]);

                    if (prefloc != -1)
                    {
                        return Task.FromResult(prefloc);
                    }
                }
            }
            return Task.FromResult(msg.GetStringPrefixLength(guit[0].prefix[0]));
        }

        private async Task OnReadyAsync(ReadyEventArgs e)
        {
            await Task.Yield();
        }

        public void Dispose()
        {
            //this.interactivity = null;
            //this.commands = null;
        }

        internal void WriteCenter(string value, int skipline = 0)
        {
            for (int i = 0; i < skipline; i++)
                Console.WriteLine();

            Console.SetCursorPosition((Console.WindowWidth - value.Length) / 2, Console.CursorTop);
            Console.WriteLine(value);
        }

        public async void handleVoidisc(int pos) //if a message needs to be sent to another channel, in commands this is not needed
        {
            try
            {
                guit[pos].offtime = DateTime.Now;
                while (guit[pos].alone || guit[pos].queue.Count < 1)
                {
                    if (DateTime.Now.Subtract(guit[pos].offtime).TotalMinutes > 5)
                    {
                        guit[pos].LLGuild.PlaybackFinished -= guit[pos].audioEvents.PlayFin;
                        guit[pos].LLGuild.TrackStuck -= guit[pos].audioEvents.PlayStu;
                        guit[pos].LLGuild.TrackException -= guit[pos].audioEvents.PlayErr;
                        guit[pos].LLGuild.Disconnect();
                        guit[pos].LLGuild = null;
                        guit[pos].offtime = DateTime.Now;
                        guit[pos].paused = false;
                        break;
                    }
                    else
                    {
                        await Task.Delay(10000);
                    }

                }
            }
            catch { }
        }

        private async Task Bot_CMDErr(CommandErrorEventArgs e) //if bot error
        {
            //e.Context.RespondAsync($"**Error:**\n```{e.Exception.Message}```");
            Console.WriteLine(e.Exception.Message);
            Console.WriteLine(e.Exception.StackTrace);
            await Task.CompletedTask;
        }

        private Task Bot_ClientErrored(ClientErrorEventArgs e) //if bot error
        {
            Console.WriteLine(e.Exception.Message);
            Console.WriteLine(e.Exception.StackTrace);
            return Task.CompletedTask;
        }

        private Task Bot_Inend(TimeoutException e) //if bot error
        {
            return Task.CompletedTask;
        }

        public Task Bot_MessageEdited(MessageUpdateEventArgs e)
        {
            return Task.CompletedTask;
        }

        public Task itError(CommandErrorEventArgs oof)
        {
            if (oof.Exception.HResult != -2146233088)
            {
                oof.Context.RespondAsync(oof.Command.Description); //as i explained above
            }
            return Task.CompletedTask;
        }
    }

    public class Gsets
    {
        [JsonProperty("GID")]
        public ulong GID { get; set; }
        [JsonProperty("LLinkCon")]
        public LavalinkNodeConnection LLinkCon { get; set; }
        [JsonProperty("LLGuild")]
        public LavalinkGuildConnection LLGuild { get; set; }
        [JsonProperty("prefix")]
        public List<string> prefix { get; set; }
        [JsonProperty("queue")]
        public List<Gsets2> queue { get; set; }
        [JsonProperty("playingnow")]
        public Gsets3 playnow { get; set; }
        [JsonProperty("offtime")]
        public DateTime offtime { get; set; }
        [JsonProperty("repeat")]
        public bool repeat { get; set; }
        [JsonProperty("repeatAll")]
        public bool repeatAll { get; set; }
        [JsonProperty("rtAll")]
        public int rAint { get; set; }
        [JsonProperty("shuffle")]
        public bool shuffle { get; set; }
        [JsonProperty("playing")]
        public bool playing { get; set; }
        [JsonProperty("timeout")]
        public bool timeout { get; set; }
        [JsonProperty("alone")]
        public bool alone { get; set; }
        [JsonProperty("paused")]
        public bool paused { get; set; }
        [JsonProperty("stoppin")]
        public bool stoppin { get; set; }
        [JsonProperty("cmdChannel")]
        public ulong cmdChannel { get; set; }
        [JsonProperty("audioPlay")]
        public Commands.Audio.Playback audioPlay { get; set; }
        [JsonProperty("audioFunc")]
        public Commands.Audio.Functions audioFunc { get; set; }
        [JsonProperty("audioQueue")]
        public Commands.Audio.Queue audioQueue { get; set; }
        [JsonProperty("audioEvents")]
        public Commands.Audio.Events audioEvents { get; set; }
    }

    public class Gsets2
    {
        [JsonProperty("requester")]
        public DiscordMember requester { get; set; }
        [JsonProperty("LavaTrack")]
        public LavalinkTrack LavaTrack { get; set; }
        [JsonProperty("addtime")]
        public DateTime addtime { get; set; }
    }

    public class Gsets3
    {
        [JsonProperty("requester")]
        public DiscordMember requester { get; set; }
        [JsonProperty("LavaTrack")]
        public LavalinkTrack LavaTrack { get; set; }
        [JsonProperty("sstop")]
        public bool sstop { get; set; }
        [JsonProperty("addtime")]
        public DateTime addtime { get; set; }
    }
}
