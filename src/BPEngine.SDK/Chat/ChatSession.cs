using System.Collections.Generic;

namespace BPEngine.SDK.Chat
{
    /// <summary>
    /// Minimal multi-turn chat container.
    /// </summary>
    public sealed class ChatSession
    {
        private readonly List<(string role, string content)> _turns = new();

        public void AddSystem(string text) => _turns.Add(("system", text ?? string.Empty));
        public void AddUser(string text) => _turns.Add(("user", text ?? string.Empty));
        public void AddAssistant(string text) => _turns.Add(("assistant", text ?? string.Empty));

        public IReadOnlyList<(string role, string content)> Turns => _turns;

        public static ChatSession From(params (string role, string content)[] turns)
        {
            var s = new ChatSession();
            foreach (var t in turns) s._turns.Add(t);
            return s;
        }
    }
}
