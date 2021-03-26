using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.WebSocket;


namespace PneumaBot2
{
    // New PneumaBot
    // --------------------------------------------
    // Functions:
    // A) Change server location based on commands
    // B) Use reactions to admin message to assign people to specific roles
    // C) Refresh message each time a new channel is added

    // To read channels:
    // Select CHANNEL SELECT channel
    // Name | Channel | Role | Emoji

    class Program
    {
        public static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        private static DiscordSocketClient _client;

        public async Task MainAsync()
        {
            var clientConfig = new DiscordSocketConfig();
            clientConfig.AlwaysDownloadUsers = true;

            _client = new DiscordSocketClient(clientConfig);
            _client.Log += Log;

            _client.MessageReceived += PneumaBotClient.OnMsgReceived;
            _client.ReactionAdded += PneumaBotClient.OnChannelConfigurationReactionAdded;
            _client.ReactionRemoved += PneumaBotClient.OnChannelConfigurationReactionRemoved;

            await _client.LoginAsync(TokenType.Bot, PneumaBotClient.TOKEN);
            await _client.StartAsync();
            
            await Task.Delay(5000);
            PneumaBotClient.GetChannels(_client);
            await PneumaBotClient.RefreshChannelConfiguration();

            await Task.Delay(-1);
        }

        

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
