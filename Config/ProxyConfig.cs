using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordProxy.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordProxy.Config
{
    [JsonObject]
    public class ProxyConfig
    {
        public List<ProxyChannel> Channels { get; set; }
    }
    public class ProxyEndpoint
    {
        public ProxyIdentifier Identifier { get; set; }
        public bool? AnonymizeUsers { get; set; }
        public bool? AllowExternalMentions { get; set; }
        public bool? ForwardReactions { get; set; }
    }
    public class ProxyIdentifier
    {
        public string Name { get; set; }
        public ulong? Id { get; set; }
        public async Task<IGuild> GetGuild(IDiscordClient client)
        {
            if (Id.HasValue)
                return await client.GetGuildAsync(Id.Value);
            else if (!string.IsNullOrEmpty(Name))
            {
                var guilds = await client.GetGuildsAsync();
                return guilds.FirstOrDefault((g) => g.Name == Name);
            }
            else
                throw new InvalidOperationException("Name or ID must be set on ProxyIdentifier!");
        }
        public SocketGuildChannel GetChannel(DiscordSocketClient client)
        {
            if (Id.HasValue)
                return (SocketGuildChannel)client.GetChannel(Id.Value);
            else
                throw new InvalidOperationException("ID must be set on ProxyIdentifier for Getting Channels!");
        }
    }
    public class ProxyChannel
    {
        public ProxyEndpoint Source { get; set; }
        public List<ProxyEndpoint> Destinations { get; set; }
        public Dictionary<ulong, (ProxyEndpoint, SocketGuildChannel)> AllChannels { get; private set; }
        private Dictionary<ulong, RestUserMessage> Messages { get; } = new Dictionary<ulong, RestUserMessage>();
        public Dictionary<ulong, (ProxyEndpoint, SocketGuildChannel)> GetAllChannels(DiscordSocketClient client) {
            if (AllChannels == null)
                AllChannels = Destinations.FindAll(pe => pe.Identifier.Id.HasValue)
                    .Select(pe => new KeyValuePair<ulong, (ProxyEndpoint, SocketGuildChannel)>(pe.Identifier.Id.Value, (pe, pe.Identifier.GetChannel(client))))
                    .Append(new KeyValuePair<ulong, (ProxyEndpoint, SocketGuildChannel)>(Source.Identifier.Id.Value, (Source, Source.Identifier.GetChannel(client))))
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
            return AllChannels;
        }
        /// <summary>
        /// Forwards a message that has been received from Source to each Destination
        /// </summary>
        /// <param name="message"></param>
        public async Task<bool> OnMessage(DiscordSocketClient client, SocketMessage message)
        {
            if (message.Author.Id == client.CurrentUser.Id)
            {
                throw new InvalidOperationException("Cannot forward a message sent by the client!");            
            }
            (ProxyEndpoint, SocketGuildChannel) match;
            if (GetAllChannels(client).TryGetValue(message.Channel.Id, out match))
                return await SendMessageToOthers(client, message, match);
            return false;
        }
        //public async Task<bool> OnEdit(DiscordSocketClient client, SocketMessage message)
        //{
        //    // If a message is edited, send the edit to all others.
        //    if (message.Author.Id != client.CurrentUser.Id)
        //    {

        //    }
        //}
        public async Task<bool> SendMessageToOthers(DiscordSocketClient client, SocketMessage message, (ProxyEndpoint, SocketGuildChannel) match)
        {
            string newContent = message.Content;
            // Next, we check config to alter the message if needed
            if (match.Item1.AllowExternalMentions.HasValue && match.Item1.AllowExternalMentions.Value)
            {
                // Convert simple mentions (@username) to Discord mentions (<@userID>)
                newContent = ProxyUtils.ConvertSimpleMentions(match.Item2.Guild, newContent);
            }
            // Convert mentions made in the server the message was sent to legible mentions
            newContent = ProxyUtils.ConvertMentionsToLegible(((SocketGuildChannel)message.Channel).Guild, newContent);
            if (match.Item1.AnonymizeUsers.HasValue && !match.Item1.AnonymizeUsers.Value)
            {
                newContent = "@" + message.Author.Username + ":\n" + newContent;
            }
            if (newContent.Length >= 500)
            {
                // TODO: Add handling for messages that are too long after getting converted
                return false;
            }
            var messageSent = await (match.Item2 as SocketTextChannel).SendMessageAsync(newContent);
            // TODO: Add messageSent to messages that are to be edited when the original message (or anything linked to it) is edited
            return true;
        }
    }
}
