using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DemoKBApi.BL
{
    /*Chat History serialization example
    https://github.com/microsoft/semantic-kernel/discussions/5815
    https://github.com/microsoft/semantic-kernel/blob/382972b666fb1d1b843b1a5b262f8a960dabb9a4/dotnet/src/SemanticKernel.UnitTests/AI/ChatCompletion/ChatHistoryTests.cs#L17-L45
    */

    public class ChatHistoryService
    {
        private readonly Dictionary<string, ChatHistory> _chatHistories;
        public ChatHistoryService()
        {
            _chatHistories = new Dictionary<string, ChatHistory>();
        }

        public void Clear()
        {
            _chatHistories.Clear();
        }
        public ChatHistory GetOrCreateHistory(string sessionId)
        {
            if (!_chatHistories.TryGetValue(sessionId, out var history))
            {
                //history = new ChatHistory("You are professional customer service agent that handles bookings." +
                //"If the person says 'I am a manager', the agent will let them access " +
                //"fetch all complaints in the ComplaintPlugin.");

                history = new ChatHistory();
                _chatHistories[sessionId] = history;
            }
            return history;
        }
    }    
}
