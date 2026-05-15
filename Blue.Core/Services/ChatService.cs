using Clippy.Core.Classes;
using Clippy.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Clippy.Core.Services
{
    public class ChatService : IChatService
    {
        private readonly HttpClient _httpClient;
        private readonly string _ollamaEndpoint = "http://localhost:11434";
        private readonly string _model = "phi3:latest";

        public ChatService()
        {
            _httpClient = new HttpClient();
        }

        public Task<string> SendChatAsync(IEnumerable<IMessage> messages)
        {
            return SendChatAsync(messages, CancellationToken.None);
        }

        public async Task<string> SendChatAsync(IEnumerable<IMessage> messages, CancellationToken cancellationToken)
        {
            var ollamaMessages = messages.Select(m => new
            {
                role = m.Role.ToString().ToLower(),
                content = m.MessageText
            }).ToList();

            var request = new
            {
                model = _model,
                messages = ollamaMessages,
                stream = false
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_ollamaEndpoint}/api/chat", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseJson);
            return ollamaResponse?.message?.content ?? string.Empty;
        }

        public async IAsyncEnumerable<string> StreamChatAsync(IEnumerable<IMessage> messages, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var ollamaMessages = messages.Select(m => new
            {
                role = m.Role.ToString().ToLower(),
                content = m.MessageText
            }).ToList();

            var request = new
            {
                model = _model,
                messages = ollamaMessages,
                stream = true
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync($"{_ollamaEndpoint}/api/chat", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new System.IO.StreamReader(stream);
            while (!reader.EndOfStream)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                OllamaResponse? chunk = null;
                try
                {
                    chunk = JsonSerializer.Deserialize<OllamaResponse>(line);
                }
                catch (JsonException)
                {
                    // ignore malformed lines
                }

                if (chunk?.message?.content != null)
                {
                    yield return chunk.message.content;
                }
            }
        }
    }

    internal class OllamaResponse
    {
        public OllamaMessage? message { get; set; }
        public bool done { get; set; }
    }

    internal class OllamaMessage
    {
        public string? role { get; set; }
        public string? content { get; set; }
    }
}
