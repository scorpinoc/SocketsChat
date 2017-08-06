using System;

namespace ChatServer.Models
{
    internal class Answer
    {
        public uint Number { get; }
        public DateTime AnswerTime { get; }

        public Answer(uint number, DateTime answerTime)
        {
            Number = number;
            AnswerTime = answerTime;
        }
    }
}