using System;

namespace SocketsChat.Model
{
    public interface IMessage
    {
        uint Number { get; }
        string NickName { get; }
        DateTime? RecieveTime { get; set; }
        string MessageText { get; }
    }
}