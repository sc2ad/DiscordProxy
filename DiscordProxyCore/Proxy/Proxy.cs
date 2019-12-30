using Discord.WebSocket;
using DiscordProxy.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordProxy.Proxy
{
    public sealed class Proxy : IDisposable
    {
        public event Func<Discord.LogMessage, Task> Log;
        public event Func<Task> Ready;
        public readonly DiscordSocketClient _client;
        private readonly ProxyConfig _config;

        public Proxy(ProxyConfig config, DiscordSocketConfig socketConfig)
        {
            _config = config;
            _client = new DiscordSocketClient(socketConfig);
        }

        public async Task Start()
        {
            _client.MessageReceived += _client_MessageReceived;
            _client.Log += _client_Log;
            _client.Ready += _client_Ready;
            _client.MessageUpdated += _client_MessageUpdated;
            await _client.StartAsync();
            await _client.LoginAsync(Discord.TokenType.Bot, File.ReadAllText("discord.key").Trim());
        }

        private async Task _client_MessageUpdated(Discord.Cacheable<Discord.IMessage, ulong> arg1, SocketMessage message, ISocketMessageChannel channel)
        {
            foreach (var c in _config.Channels)
            {
                var x = await c.OnEditMessage(message);
                if (!x)
                {
                    Console.WriteLine($"Failed to forward edit with config channel: {c} real channel: {channel.Name} message: {message.Content} with author: {message.Author}");
                }
            }
        }

        private async Task _client_Ready()
        {
            await Ready();
        }

        private async Task _client_Log(Discord.LogMessage arg)
        {
            await Log(arg);
        }

        public async Task Stop()
        {
            _client.MessageReceived -= _client_MessageReceived;
            await _client.LogoutAsync();
            Dispose();
        }

        private async Task _client_MessageReceived(SocketMessage message)
        {
            foreach (var c in _config.Channels)
            {
                if (c.GetAllChannels(_client).ContainsKey(message.Channel.Id))
                {
                    var x = await c.OnMessage(_client, message);
                    if (!x)
                    {
                        Console.WriteLine($"Failed to forward message with channel: {c} and message: {message.Content} with author: {message.Author}");
                    }
                }
            }
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
