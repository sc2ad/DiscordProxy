using Discord.WebSocket;
using DiscordProxy.Config;
using DiscordProxy.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
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
                var (newEmbed, newContents) = ProxyUtils.PrettyProxyMessage(newMessage, Endpoint);
                await message.ModifyAsync(msg =>
                {
                    msg.Embed = newEmbed;
                    if (newContents.Count > 0)
                    {
                        msg.Content = string.Join('\n', newContents);
                    }
                });
            }
            else
            {
                string newContent = ProxyUtils.ProxyMessage(newMessage, Endpoint);

                var links = newMessage.Attachments.Select(a => a.Url).ToArray();
                if (links.Length > 0)
                {
                    newContent += "\n" + string.Join('\n', links);
                }

                // Discord max length is 3000 characters
                if (newContent.Length >= 3000)
                {
                    // TODO: Add handling for messages that are too long after getting converted
                    return false;
                }

                if (string.IsNullOrEmpty(newContent))
                {
                    await message.ModifyAsync(msg => msg.Content = newContent);
                }
            }
            return true;
        }
    }
}
