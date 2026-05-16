using Blue.Core.Classes;
using Blue.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Blue.Core.Services
{
    public class ChatService : IChatService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly HttpClient _healthClient;
        private readonly string _ollamaEndpoint = "http://localhost:11434";
        private readonly string _model = "phi3:latest";

        public ChatService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            _healthClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(3)
            };
        }

        public async Task<bool> HealthCheckAsync()
        {
            try
            {
                // Use a dedicated client with a short timeout so health checks
                // never interfere with or are blocked by in-flight chat requests.
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var response = await _healthClient.GetAsync($"{_ollamaEndpoint}/api/tags", cts.Token);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
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

        public void Dispose()
        {
            _httpClient.Dispose();
            _healthClient.Dispose();
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
