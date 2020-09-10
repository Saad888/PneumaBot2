using System;

namespace PneumaBot2
{
    public struct ChannelSelector
    {
        public string Name {get; set;} // Name displayed on message
        public ulong RoleId {get; set;} // Role discord ID used
        public ulong ChannelId {get; set;} // Channel discord ID used
        public ulong EmojiId {get; set;} // Emoji discord ID used

        public static bool operator ==(ChannelSelector A, ChannelSelector B)
        {
            return (A.Name == B.Name && 
                    A.RoleId == B.RoleId && 
                    A.ChannelId == B.ChannelId && 
                    A.EmojiId == B.EmojiId);
        }

        public static bool operator !=(ChannelSelector A, ChannelSelector B)
        {
            return (A.Name != B.Name || 
                    A.RoleId != B.RoleId ||
                    A.ChannelId != B.ChannelId ||
                    A.EmojiId != B.EmojiId);
        }


        public override bool Equals(object obj)
        {
            
            return base.Equals(obj);
        }
        
        // override object.GetHashCode
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}