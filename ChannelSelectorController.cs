using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PneumaBot2
{
    public static class ChannelSelectController
    {
        public static List<ChannelSelector> ChannelList {get; set;}

        static Regex rxRoleId = new Regex(@"<@&[0-9]*>");
        static Regex rxEmojiId = new Regex(@"<:.*:[0-9]*>");
        static Regex rxChannelId = new Regex(@"<#[0-9]*>");
        static Regex rxNumber = new Regex(@"[0-9]+");



        static ChannelSelectController()
        {
            ChannelList = new List<ChannelSelector>();
        }

        // Receive strings from main client with relevant metadata
        // Name | Channel | Role | Emoji
        // "Test Channel 1 | <#537460056979931151> | <@&662770161425842246> | <:fifty:587768727723048981>"
        public static string RefreshChannelList(List<string> configInputs)
        {
            string msg = "";
            ChannelList = new List<ChannelSelector>();

            foreach(var input in configInputs)
            {
                // Verify input
                if (!InputMessageFormatValid(input))
                    continue;

                var inputs = input.Split(" | ");
                // Extract input information
                var newEntry = new ChannelSelector();
                try
                {   
                    var name = inputs[0];
                    var channelId = Convert.ToUInt64(GetNumbersFromString(inputs[1]));
                    var roleId = Convert.ToUInt64(GetNumbersFromString(inputs[2]));
                    var emojiId = Convert.ToUInt64(GetNumbersFromString(inputs[3]));

                    // Validate
                    if (!PneumaBotClient.ValidateChannelId(channelId))
                        throw new PneumaBotExceptions("Channel not in this server.");
                    if (!PneumaBotClient.ValidateRoldId(roleId))
                        throw new PneumaBotExceptions("Role not in this server.");
                    if (!PneumaBotClient.ValidateEmojiId(emojiId))
                        throw new PneumaBotExceptions("Emoji not in this server.");

                    newEntry.Name = name;
                    newEntry.ChannelId = channelId;
                    newEntry.RoleId = roleId;
                    newEntry.EmojiId = emojiId;
                }
                catch (Exception e)
                {
                    msg += $"ERROR when processing: {input}\n{e.Message}\n";
                    continue;
                }

                // Add to list
                ChannelList.Add(newEntry);
            }

            return msg + "Channel List Refreshed";
        }

        private static string GetNumbersFromString(string input)
        {
            var matches = rxNumber.Matches(input);
            return matches.Last().ToString();
        }


        public static bool InputMessageFormatValid(string input)
        {
            // Check if can be split
            // Check if 
            var inputs = input.Split(" | ");
            if (inputs.Length != 4)
                return false;

            if (!rxChannelId.IsMatch(inputs[1]))
                return false;

            if (!rxRoleId.IsMatch(inputs[2]))
                return false;

            if (!rxEmojiId.IsMatch(inputs[3]))
                return false;

            return true;
        }

    }
}