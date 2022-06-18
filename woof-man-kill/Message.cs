namespace WoofManKill
{
    class Message
    {
        public enum Type : byte
        {
            Announcement,
            Chat,
            Disconnect,
            Vote,
            End,
        }

        public Type Ty { get; set; }
        public string Content { get; set; } = "-";
        public string Sender { get; set; } = "-";
        public string Target { get; set; } = "-";

        public Message(Type ty, string content = "-", string sender = "-", string target = "-")
        {
            Ty = ty;
            Content = content;
            Sender = sender;
            Target = target;
        }

        static public Message Announcement(string content) => new(Type.Announcement, content);
        static public Message Chat(string content, string sender = "-") => new(Type.Chat, content, sender);
        static public Message Disconnect() => new(Type.Disconnect);
        static public Message Vote(string sender, string target) => new(Type.Vote, "-", sender, target);
        static public Message End() => new(Type.End);
    }
}