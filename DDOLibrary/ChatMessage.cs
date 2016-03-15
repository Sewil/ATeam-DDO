using System;

namespace DDOLibrary {
    public class ChatMessage {
        public DateTime Sent { get; }
        public string Content { get; }
        public string Name { get; }

        public ChatMessage(DateTime sent, string content, string name) {
            Sent = sent;
            Content = content;
            Name = name;
        }
    }
}
