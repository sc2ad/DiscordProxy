using DiscordProxy.Config;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordProxy.Proxy;
using Discord.WebSocket;

namespace DiscordProxy
{
    class Program
    {
        private static Proxy.Proxy proxy;
        static async Task Main(string[] args)
        {
            Console.WriteLine("[INFO] Main: Beginning Discord Bot!");
            var config = JsonConvert.DeserializeObject<ProxyConfig>(File.ReadAllText("config.json"));
            var socketConfig = new DiscordSocketConfig()
            {
#if DEBUG
                LogLevel = Discord.LogSeverity.Debug
#endif
            };
            proxy = new Proxy.Proxy(config, socketConfig);
            proxy.Log += Proxy_Log;
            proxy.Ready += Proxy_Ready;
            try
            {
                await proxy.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            // Halt execution until the application is done running
            await Task.Delay(-1);
        }

        private static async Task Proxy_Ready()
        {
            var guilds = proxy._client.Guilds;
            foreach (var g in guilds)
            {
                Console.WriteLine($"GUILD: {g.Name} ID: {g.Id}");
                foreach (var c in g.Channels)
                {
                    Console.WriteLine($"- {c.Name}: {c.Id}");
                }
            }
        }

        private static async Task Proxy_Log(Discord.LogMessage obj)
        {
            Console.WriteLine($"[{obj.Severity}] {obj.Source}: {obj.Message}");
        }
    }
}
