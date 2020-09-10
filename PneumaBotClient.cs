using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System.Collections.Generic;


namespace PneumaBot2
{
    public static class PneumaBotClient
    {
        // Pneuma Bot Commands:
        public static string COMMAND_REFRESH_CONFIGURATIONS = "!Refresh";

        // Parameters
        // Token:
        public static string TOKEN = "";

        // Channels:
        static ulong ChannelID_Configurations = 753352703811387573;
        static ulong ChannelID_Select = 586339544920621134;
        static ulong ChannelID_Logging = 586336266702946334; 

        // Channels:
        static SocketTextChannel Channel_Config;
        static SocketTextChannel Channel_Select;
        static SocketTextChannel Channel_Logging;

        // Client
        static DiscordSocketClient Client;

        // Guild
        static SocketGuild Guild;

        // Messages
        static RestUserMessage MainMessage = null;

        #region Initiation Methods
        public static void GetChannels(DiscordSocketClient client)
        {
            Client = client;
            Channel_Config = (SocketTextChannel)client.GetChannel(ChannelID_Configurations);
            Channel_Select = (SocketTextChannel)client.GetChannel(ChannelID_Select);
            Channel_Logging = (SocketTextChannel)client.GetChannel(ChannelID_Logging);

            Guild = Channel_Config.Guild;
        }

        #endregion


        
        public static async Task OnMsgReceived(SocketMessage msg)
        {
            
            if (msg.Author.Id == Client.CurrentUser.Id)
                return;

            var content = msg.Content;
            if (content.Contains("579965103038791689") && 
                (content.ToLower().Contains("change server") ||
                content.ToLower().Contains("change location") ||
                content.ToLower().Contains("change loc")))
                    await ChangeServerLocation(msg);

            else if (msg.Channel.Id == ChannelID_Configurations && msg.Content == COMMAND_REFRESH_CONFIGURATIONS)
                await RefreshChannelConfiguration();
        }



        #region Role Select Configruation Changes
        // General Role Select Refresh
        public static async Task RefreshChannelConfiguration()
        {
            var inputs = new List<string>();

            // Get last 100 messages currently in the channel
            var msgs = await Channel_Config.GetMessagesAsync(100).FlattenAsync();

            // Delete useless messages, add content of remaining ones for processing
            foreach(var msg in msgs)
            {
                var content = msg.Content;
                if(content.Contains(@"Template: Name | #channel | @role | :emoji:. Use !Refresh to update changes."))
                    continue;
                else if (ChannelSelectController.InputMessageFormatValid(content))
                    inputs.Add(content);
                else
                    await msg.DeleteAsync();
            }

            // Refresh the configurations
            var processMsg = ChannelSelectController.RefreshChannelList(inputs);
            await LogMessage(processMsg);

            // Generate Embed Message
            var embeddedMessage = await GenerateEmbeddedMessage();

            // Get all messages in the select channel and delete everything except the last message which should be Pneumas
            var configmsgs = await Channel_Select.GetMessagesAsync(100).FlattenAsync();
            RestUserMessage lastMsg = null;
            foreach(var msg in configmsgs)
            {
                if (msg == configmsgs.Last() && msg.Author.Id == Client.CurrentUser.Id)
                    lastMsg = (RestUserMessage)msg;
                else
                    await msg.DeleteAsync();
            }

            // Edit if the last message exists and is Pneuma's, else delete and create a new one
            if (lastMsg == null)
            {
                lastMsg = await Channel_Select.SendMessageAsync(null, false, embeddedMessage);
            }
            else
            {
                await lastMsg.ModifyAsync(x => x.Embed = embeddedMessage);
            }
            MainMessage = lastMsg;


            // Verify all emojis exist on message
            foreach(var selection in ChannelSelectController.ChannelList)
            {
                var emoji = await Guild.GetEmoteAsync(selection.EmojiId);
                // Verify if Pneuma has done the emote
                var emojiUses = await MainMessage.GetReactionUsersAsync(emoji, 100).FlattenAsync();
                if (!emojiUses.Any(u => u.Id == Client.CurrentUser.Id))
                    await MainMessage.AddReactionAsync(emoji);
            }
        }

        static async Task<Embed> GenerateEmbeddedMessage()
        {
            var msg = new EmbedBuilder();
            msg.Title = "Hello!";
            msg.Description = "If you wish to be added to a text channel, please react to this message with the correct emoji listed below:";
            msg.Color = new Color(23, 240, 214);

            var channelList = ChannelSelectController.ChannelList;
            foreach(var channel in channelList)
            {
                var name = channel.Name;
                var emoji = (await Guild.GetEmoteAsync(channel.EmojiId)).ToString();
                msg.AddField(name, emoji, true);
            }

            msg.AddField("Server Location Change", "To change voice channel server locations, tag me with a message saying \"@PneumaBot change server\" (lowercase) in any text channel. \nYou can also specify a region with \"east\", \"west\", \"south\", or \"central\"", false);
            
            var footerbuilder = new EmbedFooterBuilder();
            footerbuilder.Text = "Removing a reaction will remove you from that channel. Bot created by Xaad#1337";
            msg.Footer = footerbuilder;

            return msg.Build();
        }



        
        public static async Task OnChannelConfigurationReactionAdded(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel socket, SocketReaction reaction)
        {
            if(MainMessage == null)
                return;

            if(reaction.MessageId != MainMessage.Id)
                return;

            if (reaction.UserId == Client.CurrentUser.Id)
                return;

            await AddRoleToUser(reaction);
        }


        public static async Task AddRoleToUser(SocketReaction reaction)
        {
            var emoji = (Emote)reaction.Emote;
            if (!ChannelSelectController.ChannelList.Any(c => c.EmojiId == emoji.Id))
                return;

            var selector = ChannelSelectController.ChannelList.First(c => c.EmojiId == emoji.Id);

            var user = (SocketGuildUser)reaction.User;
            var role = Guild.GetRole(selector.RoleId);

            // Add role to user
            await user.AddRoleAsync(role);

            // Let user know
            var channel = Guild.GetTextChannel(selector.ChannelId);
            var msg = $"{user.Mention} has joined {channel.Name}";
            await channel.SendMessageAsync(msg);

            // Admin message
            await Channel_Logging.SendMessageAsync(msg);
        }


        public static async Task OnChannelConfigurationReactionRemoved(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel socket, SocketReaction reaction)
        {
            if(MainMessage == null)
                return;

            if(reaction.MessageId != MainMessage.Id)
                return;

                
            if (reaction.UserId == Client.CurrentUser.Id)
                return;

            await RemoveRoleFromUser(reaction);
        }


        public static async Task RemoveRoleFromUser(SocketReaction reaction)
        {
            var emoji = (Emote)reaction.Emote;

            if (!ChannelSelectController.ChannelList.Any(c => c.EmojiId == emoji.Id))
                return;

            var selector = ChannelSelectController.ChannelList.First(c => c.EmojiId == emoji.Id);

            var user = (SocketGuildUser)reaction.User;
            var role = Guild.GetRole(selector.RoleId);

            // Add role to user
            await user.RemoveRoleAsync(role);

            // Let user know
            var channel = Guild.GetTextChannel(selector.ChannelId);
            var msg = $"{user.Username} has left {channel.Name}";
            await channel.SendMessageAsync(msg);

            // Admin message
            await Channel_Logging.SendMessageAsync(msg);
        }
        #endregion


        #region Server Location Changes
        static List<string> serverLocs = new List<string>() {"us-east", "us-west", "us-south", "us-central"};



        public static async Task OnServerChannelChangeRequest(SocketMessage msg)
        {
            var content = msg.Content;
            if (content.Contains("579965103038791689") && 
                (content.ToLower().Contains("change server") ||
                content.ToLower().Contains("change location") ||
                content.ToLower().Contains("change loc")))
                    await ChangeServerLocation(msg);
        }

        private static async Task ChangeServerLocation(SocketMessage msg)
        {
            var content = msg.Content;
            string newId = null;
            if (content.Contains("east"))
                newId = "us-east";
            if (content.Contains("west"))
                newId = "us-west";
            if (content.Contains("south"))
                newId = "us-south";
            if (content.Contains("central"))
                newId = "us-central";

            if (newId == null)
            {
                var currentId = Guild.VoiceRegionId;
                var index = serverLocs.IndexOf(currentId);
                index = (index + 1) % serverLocs.Count;
                newId = serverLocs[index];
            }

            int timeout = 5000;
            var task = Guild.ModifyAsync(a => a.RegionId = newId);
            if (await Task.WhenAny(task, Task.Delay(timeout)) == task) {
                // task completed within timeout
                await msg.Channel.SendMessageAsync($"Voice channel region changed to {newId}!");
            } else { 
                // timeout logic
                Console.WriteLine("Somethings not right pls help");
                await msg.Channel.SendMessageAsync($"We're getting rate limted, try again later...");
            }
        }
        #endregion


        #region Validation
        public static bool ValidateRoldId(ulong id)
        {
            return Guild.Roles.Any(r => r.Id == id);
        }

        
        public static bool ValidateChannelId(ulong id)
        {
            return Guild.Channels.Any(c => c.Id == id);
        }

        
        public static bool ValidateEmojiId(ulong id)
        {
            return Guild.Emotes.Any(e => e.Id == id);
        }

        #endregion


        public static async Task LogMessage(string message)
        {
            await Channel_Logging.SendMessageAsync(message);
        }
    }
}
