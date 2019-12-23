using Discord.WebSocket;
using DiscordProxy.Config;
using DiscordProxy.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordProxyCore.Config
{
    public class HeaderSocketMessage
    {
        public ProxyEndpoint Endpoint { get; set; }
        public SocketMessage Message { get; set; }
        public async Task<bool> ModifyMessage(SocketMessage newMessage)
        {
            string newContent = ProxyUtils.ProxyMessage(newMessage, Endpoint);
            if (newMessage is SocketUserMessage && !string.IsNullOrEmpty(newContent))
            {
                await (Message as SocketUserMessage).ModifyAsync(msg => msg.Content = newContent);
                return true;
            }
            return false;
        }
    }
}
