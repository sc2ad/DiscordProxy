using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordProxy.Utils;
using DiscordProxyCore.Config;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool AnonymizeUsers { get; set; }

        [DefaultValue(false)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool AllowExternalMentions { get; set; }

        public bool? ForwardReactions { get; set; }

        [DefaultValue(false)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool ReceiveDMs { get; set; }

        [DefaultValue(false)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool ReceiveGroupMessages { get; set; }

        [DefaultValue(false)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool PrettyPrint { get; set; }

        public string ConvertMessageContent(IMessage message)
        {
            string newContent = "";
            // Only forward DMs to recipients that accept DMs
            if (message.Channel is ISocketPrivateChannel)
            {
                if (message.Channel is SocketGroupChannel && ReceiveGroupMessages)
                {
                    // Message is from Group
                    newContent = $"Group DM From: {(message.Channel as SocketGroupChannel).Name}:\n{message.Content}";
                }
                else if (message.Channel is SocketDMChannel && ReceiveDMs)
                {
                    // Message is from DM
                    foreach (var u in (message.Channel as SocketDMChannel).Users)
                    {
                        // Assumes the other user is not a bot
                        if (!u.IsBot)
                        {
                            newContent = $"DM From {u.Username}:\n{message.Content}";
                            break;
                        }
                    }
                }
                return newContent;
            }
            newContent = message.Content;
            if (AllowExternalMentions)
            {
                // TODO: Add something here?
            }
            if (!AnonymizeUsers)
            {
                newContent = "@" + message.Author.Username + ":\n" + newContent;
            }
            return newContent;
        }
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
        [JsonIgnore]
        public Dictionary<ulong, (ProxyEndpoint, SocketTextChannel)> AllChannels { get; private set; }
        [JsonIgnore]
        private List<LinkedMessage> Messages { get; } = new List<LinkedMessage>();
        [JsonIgnore]
        private ulong _lastProxiedID;
        private LinkedMessage _proxiedLinkedMessage;
        public Dictionary<ulong, (ProxyEndpoint, SocketTextChannel)> GetAllChannels(DiscordSocketClient client) {
            if (AllChannels == null)
                AllChannels = Destinations.FindAll(pe => pe.Identifier.Id.HasValue)
                    .Select(pe => new KeyValuePair<ulong, (ProxyEndpoint, SocketTextChannel)>(pe.Identifier.Id.Value, (pe, (SocketTextChannel)pe.Identifier.GetChannel(client))))
                    .Append(new KeyValuePair<ulong, (ProxyEndpoint, SocketTextChannel)>(Source.Identifier.Id.Value, (Source, (SocketTextChannel)Source.Identifier.GetChannel(client))))
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
            return AllChannels;
        }
        public async Task<bool> OnEditMessage(SocketMessage message)
        {
            foreach (var m in Messages)
            {
                if (message.Id == m.SourceID)
                {
                    var success = await m.OnMessageEdit(message);
                    if (!success)
                        return false;
                }
            }
            return true;
        }
        /// <summary>
        /// Forwards a message that has been received from Source to each Destination
        /// </summary>
        /// <param name="message"></param>
        public async Task<bool> OnMessage(DiscordSocketClient client, SocketMessage message)
        {
            if (message.Author.Id == client.CurrentUser.Id) {
                if (_lastProxiedID != 0 && _proxiedLinkedMessage != null)
                {
                    // Assume that a message sent from this bot is only after a message has been sent from a user
                    if (GetAllChannels(client).ContainsKey(message.Channel.Id))
                    {
                        _proxiedLinkedMessage.AddHeader(message, GetAllChannels(client)[message.Channel.Id].Item1);
                    }
                }
                return true;
            }
            foreach (var k in GetAllChannels(client).Keys)
            {
                if (k == message.Channel.Id)
                    continue;
                var success = await SendMessageToOthers(message, GetAllChannels(client)[k]);
                if (!success)
                    return false;
            }
            _lastProxiedID = message.Id;
            _proxiedLinkedMessage = new LinkedMessage(_lastProxiedID);
            Messages.Add(_proxiedLinkedMessage);
            return true;
        }
        public async Task<bool> SendMessageToOthers(SocketMessage message, (ProxyEndpoint, SocketTextChannel) match)
        {
            if (match.Item1.PrettyPrint)
            {
                var newEmbed = ProxyUtils.PrettyProxyMessage(message, match.Item1);
                await match.Item2.SendMessageAsync(embed: newEmbed);
            }
            else
            {
                string newContent = ProxyUtils.ProxyMessage(message, match.Item1);
                // Discord max length is 300 characters
                if (newContent.Length >= 300)
                {
                    // TODO: Add handling for messages that are too long after getting converted
                    return false;
                }

                if (!string.IsNullOrEmpty(newContent))
                    await match.Item2.SendMessageAsync(newContent);
                // TODO: Add messageSent to messages that are to be edited when the original message (or anything linked to it) is edited
            }

            return true;
        }
    }
}
