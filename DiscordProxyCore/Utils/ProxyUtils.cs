using Discord;
using Discord.WebSocket;
using DiscordProxy.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordProxy.Utils
{
    public sealed class ProxyUtils
    {
        public static string ProxyMessage(SocketMessage message, ProxyEndpoint endpoint)
        {
            string newContent = "";
            
            if (endpoint == null)
            {
                if (message.Channel is SocketGroupChannel)
                {
                    // Message is from Group
                    newContent = $"Group DM From: {(message.Channel as SocketGroupChannel).Name}:\n{message.Content}";
                }
                else if (message.Channel is SocketDMChannel)
                {
                    // Message is from DM
                    foreach (var u in (message.Channel as SocketDMChannel).Users)
                    {
                        // Assumes the other user is not a bot
                        if (!u.IsBot)
                            newContent = $"DM From {u.Username}:\n{message.Content}";
                    }

                }
            }
            else
            {
                newContent = endpoint.ConvertMessageContent(message);
            }
            return newContent;
        }
        public static Embed PrettyProxyMessage(SocketMessage message, ProxyEndpoint endpoint)
        {
            var embed = new EmbedBuilder {Description = message.Content};

            if (!endpoint.AnonymizeUsers)
            {
                embed = embed.WithAuthor(message.Author);
            }

            if (message.Channel is ISocketPrivateChannel)
            {
                embed = message.Channel switch
                {
                    SocketGroupChannel channel => embed.WithTitle(channel.Name),
                    SocketDMChannel _ => embed.WithTitle("DM"),
                    _ => embed
                };
            }

            return embed.WithCurrentTimestamp().Build();
        }

        public static string ConvertMentionsToLegible(SocketGuild sourceGuild, string originalMessage)
        {
            int leftMention = originalMessage.IndexOf("<");
            if (leftMention == -1 || originalMessage.Length <= 2)
                return originalMessage;
            int rightMention = originalMessage.Substring(leftMention + 2).IndexOf(">") + 2;
            while (leftMention + 2 <= rightMention)
            {
                string toReplace = originalMessage.Substring(leftMention, rightMention - leftMention + 1);
                ulong id;
                // Get THING from ID here:
                if (MentionUtils.TryParseChannel(toReplace, out id))
                    originalMessage = originalMessage.Replace(toReplace, "#" + sourceGuild.GetChannel(id).Name);
                else if (MentionUtils.TryParseRole(toReplace, out id))
                    originalMessage = originalMessage.Replace(toReplace, "@" + sourceGuild.GetRole(id).Name);
                else if (MentionUtils.TryParseUser(toReplace, out id))
                    originalMessage = originalMessage.Replace(toReplace, "@" + sourceGuild.GetUser(id).Username);
                else
                    // Not parsable into anything known:
                    originalMessage.Replace(toReplace, "[[UNKNOWN MENTION]]");
                leftMention = originalMessage.IndexOf("<");
                if (leftMention == -1)
                    break;
                rightMention = originalMessage.Substring(leftMention).IndexOf(">");
            }
            return originalMessage;
        }
        // TODO: Fix this method
        public static string ConvertSimpleMentions(SocketGuild guild, string originalMessage)
        {
            int leftMention = originalMessage.IndexOf("@");
            if (leftMention == -1 || originalMessage.Length <= 2)
                return originalMessage;
            int rightMention = originalMessage.Substring(leftMention).IndexOf(" ");
            while (leftMention + 1 < rightMention)
            {
                string toReplace = originalMessage.Substring(leftMention, rightMention - leftMention);
                string name = originalMessage.Substring(leftMention + 1, toReplace.Length - 2);

            }
            return "";
        }
    }
}
