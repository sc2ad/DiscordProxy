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
            if (!(Message is SocketUserMessage message)) return false;

            if (Endpoint.PrettyPrint)
            {
                var newEmbed = ProxyUtils.PrettyProxyMessage(newMessage, Endpoint);
                await message.ModifyAsync(msg => msg.Embed = newEmbed);
            }
            else
            {
                string newContent = ProxyUtils.ProxyMessage(newMessage, Endpoint);
                await message.ModifyAsync(msg => msg.Content = newContent);
            }
            return true;
        }
    }
}
