using Discord;
using Discord.WebSocket;
using DiscordProxy.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordProxy.Utils
{
    public sealed class ProxyUtils
    {
        public static string ProxyMessage(SocketMessage message, ProxyEndpoint endpoint)
        {
            if (endpoint != null)
                return endpoint.ConvertMessageContent(message);
            return "";
        }
        public static (Embed, List<string>) PrettyProxyMessage(SocketMessage message, ProxyEndpoint endpoint)
        {
            var embed = new EmbedBuilder {Description = message.Content};
            var contents = new List<string>();

            if (!endpoint.AnonymizeUsers)
            {
                embed = embed.WithAuthor(message.Author);
            }

            // Set embed image to first image in attachments and add other links to contents
            var hasFirstImage = false;
            foreach (var attachment in message.Attachments)
            {
                if (!hasFirstImage && attachment.Width.HasValue && attachment.Height.HasValue)
                {
                    embed = embed.WithImageUrl(attachment.Url);
                    hasFirstImage = true;
                }
                else
                {
                    contents.Add(attachment.Url);
                }
            }

            if (!endpoint.AnonymizeChannels)
            {
                embed = message.Channel switch
                {
                    // If the channel is a normal guild channel, display the guild, category and channel names
                    SocketTextChannel channel => embed
                        .WithTitle($"#{channel.Name}")
                        .WithFooter(channel.Category != null
                            ? $"{channel.Guild.Name} ({channel.Category.Name})"
                            : channel.Guild.Name),
                    // If the channel is a DM channel, specify it
                    SocketDMChannel _ => embed.WithFooter("DM"),
                    // If the channel is a group DM channel, specify it and display the group DM name
                    SocketGroupChannel channel => embed.WithFooter($"DM ({channel.Name})"),
                    // If none of the above apply, nothing is displayed
                    _ => embed
                };
            }

            return (embed.WithCurrentTimestamp().Build(), contents);
        }
    }
}
