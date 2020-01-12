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

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool AnonymizeChannels { get; set; }

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
        private SocketTextChannel _channel;
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
        public SocketTextChannel GetChannel(DiscordSocketClient client)
        {
            if (_channel == null)
            {
                if (Id.HasValue)
                    _channel = (SocketTextChannel)client.GetChannel(Id.Value);
                else
                    throw new InvalidOperationException("ID must be set on ProxyIdentifier for getting channels!");
            }
            return _channel;
        }
    }
    public class ProxyChannel
    {
        public ProxyEndpoint Source { get; set; }
        public ProxyEndpoint Destination { get; set; }
        public int MaxCachedMessagesCount { get; set; } = 1000;
        [JsonIgnore]
        private List<LinkedMessage> Messages { get; } = new List<LinkedMessage>();
        [JsonIgnore]
        private ulong _lastProxiedID;
        [JsonIgnore]
        private LinkedMessage _proxiedLinkedMessage;
        public async Task<bool> OnEditMessage(SocketMessage message)
        {
            foreach (var m in Messages)
            {
                if (m.SourceID == message.Id)
                    if (!await m.OnMessageEdit(message))
                        return false;
            }
            return true;
        }
        /// <summary>
        /// Forwards a message that has been received from Source to Destination
        /// </summary>
        /// <param name="message"></param>
        public async Task<bool> OnMessage(DiscordSocketClient client, SocketMessage message)
        {
            if (message.Author.Id == client.CurrentUser.Id) {
                // If we sent the message
                if (_lastProxiedID != 0 && _proxiedLinkedMessage != null)
                {
                    // Assume that if a message is sent in the destination channel immediately after _lastProxiedID has been set
                    // Is a message to link to the proxiedLinkedMessage
                    if (Source.Identifier.Id == message.Channel.Id)
                        // If this message was just received in the source, then use Destination config
                        _proxiedLinkedMessage.AddHeader(message, Destination);
                    else if (Destination.Identifier.Id == message.Channel.Id)
                        // If this message was just received in the destination, then use Source config
                        _proxiedLinkedMessage.AddHeader(message, Source);
                }
                return true;
            }
            if (message.Channel is SocketDMChannel)
            {
                // If the message was DM'd, send this message to the source/destination
                // ONLY IF THEY HAVE THE FLAGS SET!
                if (Source.ReceiveDMs && !await SendMessageTo(message, Source, Source.Identifier.GetChannel(client)))
                    return false;
                if (Destination.ReceiveDMs && !await SendMessageTo(message, Destination, Destination.Identifier.GetChannel(client)))
                    return false;
            }
            else if (message.Channel is SocketGroupChannel)
            {
                // If the message was sent in a group chat, send this message to the source/destination
                // ONLY IF THEY HAVE THE FLAGS SET!
                if (Source.ReceiveGroupMessages && !await SendMessageTo(message, Source, Source.Identifier.GetChannel(client)))
                    return false;
                if (Destination.ReceiveGroupMessages && !await SendMessageTo(message, Destination, Destination.Identifier.GetChannel(client)))
                    return false;
            }
            else if (message.Channel.Id == Source.Identifier.Id.Value)
            {
                // If the message was sent in the source, send a message to the Destination
                if (!await SendMessageTo(message, Source, Destination.Identifier.GetChannel(client)))
                    return false;
            }
            else if (message.Channel.Id == Destination.Identifier.Id.Value)
            {
                if (!await SendMessageTo(message, Destination, Source.Identifier.GetChannel(client)))
                    return false;
            }
            else
            {
                // Message received from a channel that is not this one
                // Return true because there is nothing to send
                return true;
            }
            // Cache the message for edits iff the message is actually handled/sent
            _lastProxiedID = message.Id;
            _proxiedLinkedMessage = new LinkedMessage(_lastProxiedID);
            if (Messages.Count >= MaxCachedMessagesCount)
                Messages.RemoveAt(0);
            Messages.Add(_proxiedLinkedMessage);
            return true;
        }
        public async Task<bool> SendMessageTo(SocketMessage message, ProxyEndpoint source, SocketTextChannel dest)
        {
            if (source.PrettyPrint)
            {
                var (newEmbed, newContents) = ProxyUtils.PrettyProxyMessage(message, source);
                if (newContents.Count > 0)
                    return (await dest?.SendMessageAsync(string.Join('\n', newContents), embed: newEmbed)) != null;
                else
                    return (await dest?.SendMessageAsync(embed: newEmbed)) != null;
            }
            else
            {
                var newContent = ProxyUtils.ProxyMessage(message, source);
                var links = message.Attachments?.Select(a => a.Url).ToArray();
                if (links.Length > 0)
                    newContent += '\n' + string.Join('\n', links);
                do
                {
                    // TODO: Add handling for messages that are too long after getting converted/edited
                    // Sending multiple messages will cause big errors when editing them
                    // Currently, the edits will simply not propagate
                    var check = (await dest?.SendMessageAsync(newContent.Substring(0, Math.Min(3000, newContent.Length)))) != null;
                    if (!check)
                        return false;
                    newContent = newContent.Substring(Math.Min(3000, newContent.Length - 1));
                } while (newContent.Length >= 3000);
                return true;
            }
        }
    }
}
