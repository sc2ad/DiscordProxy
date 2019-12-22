using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordProxy.Utils
{
    public sealed class ProxyUtils
    {
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
