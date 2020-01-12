using Discord.Rest;
using Discord.WebSocket;
using DiscordProxy.Config;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordProxyCore.Config
{
    public class LinkedMessage
    {
        public ulong SourceID { get; set; }
        public List<HeaderSocketMessage> Links { get; } = new List<HeaderSocketMessage>();
        public LinkedMessage(ulong id)
        {
            SourceID = id;
        }
        public void AddHeader(SocketMessage message, ProxyEndpoint endpoint)
        {
            Links.Add(new HeaderSocketMessage()
            {
                Message = message,
                SourceEndpoint = endpoint
            });
        }
        public async Task<bool> OnMessageEdit(SocketMessage message)
        {
            // Check if the message edited is the SourceID
            // If it is, we need to propogate this edit to all other locations
            if (message.Id == SourceID)
            {
                foreach (var l in Links)
                {
                    var success = await l.ModifyMessage(message);
                    if (!success)
                        return false;
                }
            }
            return true;
        }
    }
}
