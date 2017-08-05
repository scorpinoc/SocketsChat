using System;

namespace ChatServer.Models
{
    public class Message
    {
        public uint Number { get; }
        public string NickName { get; }
        public DateTime? RecieveTime { get; set; }
        public string MessageText { get; }

        public Message(uint number, string nickName, string messageText)
        {
            Number = number;
            NickName = nickName;
            MessageText = messageText;
        }
    }
}