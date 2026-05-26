using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RevitMate.Core.Models;

namespace RevitMate.Core.Api
{
    /// <summary>
    /// HTTP client for the Anthropic Claude Messages API. The client owns a
    /// <see cref="ConversationHistory"/> so callers can drive multi-turn,
    /// tool-augmented conversations across multiple calls without rebuilding
    /// the message array each turn.
    /// </summary>
    public class ClaudeApiClient
    {
        private const string Endpoint = "https://api.anthropic.com/v1/messages";
        private const string AnthropicVersion = "2023-06-01";
        private const int DefaultMaxTokens = 4096;
        private const int ResponseBodyExcerptLimit = 500;

        /// <summary>
        /// Shared <see cref="HttpClient"/> reused across all calls and instances
        /// so the underlying sockets are pooled. Per Microsoft guidance,
        /// <see cref="HttpClient"/> is intended to be instantiated once and reused.
        /// </summary>
        private static readonly HttpClient SharedHttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(2)
        };

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        private readonly string _apiKey;
        private readonly string _model;

        /// <summary>
        /// Running history of user, assistant, and <c>tool_result</c> messages
        /// for this client. Each successful <see cref="SendMessageAsync"/> call
        /// appends the user turn (when provided) and the assistant turn. Callers
        /// may inspect or mutate this list directly when needed.
        /// </summary>
        public List<Message> ConversationHistory { get; } = new List<Message>();

        /// <summary>
        /// Creates a new client bound to a specific API key and model.
        /// </summary>
        /// <param name="apiKey">Anthropic API key, sent in the <c>x-api-key</c> header on every request.</param>
        /// <param name="model">Model identifier; defaults to <c>claude-sonnet-4-5</c>.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="apiKey"/> or <paramref name="model"/> is null, empty, or whitespace.</exception>
        public ClaudeApiClient(string apiKey, string model = "claude-sonnet-4-5")
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("Claude API key must not be empty.", nameof(apiKey));
            if (string.IsNullOrWhiteSpace(model))
                throw new ArgumentException("Claude model name must not be empty.", nameof(model));

            _apiKey = apiKey;
            _model = model;
        }

        /// <summary>
        /// Appends <paramref name="userText"/> (when non-empty) to the conversation,
        /// POSTs the entire history to Claude, and appends the assistant's reply
        /// to <see cref="ConversationHistory"/> on success. On failure, any
        /// turns appended during this call are rolled back so the history remains
        /// consistent.
        /// </summary>
        /// <param name="userText">
        /// The user's next message. Pass <c>null</c> or empty to continue an
        /// existing conversation without adding a new user turn — typically
        /// after one or more calls to <see cref="AddToolResult"/>.
        /// </param>
        /// <param name="tools">Tool definitions Claude may call; pass <c>null</c> to disable tool use.</param>
        /// <param name="systemPrompt">System prompt prepended on every request; pass <c>null</c> for none.</param>
        /// <returns>The deserialized response from Claude.</returns>
        /// <exception cref="InvalidOperationException">Thrown when both <paramref name="userText"/> and the existing history are empty.</exception>
        /// <exception cref="ClaudeApiException">Thrown on network failure, non-2xx response, or malformed JSON.</exception>
        public async Task<MessageResponse> SendMessageAsync(
            string userText,
            List<Tool> tools = null,
            string systemPrompt = null)
        {
            int historyCountBefore = ConversationHistory.Count;

            if (!string.IsNullOrEmpty(userText))
            {
                ConversationHistory.Add(new Message { Role = "user", Content = userText });
            }

            if (ConversationHistory.Count == 0)
                throw new InvalidOperationException(
                    "Cannot send: conversation history is empty and no user text was provided.");

            try
            {
                MessageResponse response = await SendRequestAsync(tools, systemPrompt).ConfigureAwait(false);

                ConversationHistory.Add(new Message
                {
                    Role = "assistant",
                    Content = response.Content
                });

                return response;
            }
            catch
            {
                while (ConversationHistory.Count > historyCountBefore)
                    ConversationHistory.RemoveAt(ConversationHistory.Count - 1);
                throw;
            }
        }

        /// <summary>
        /// Appends a <c>tool_result</c> content block to the conversation. Call
        /// this after the executor finishes a tool that Claude requested in the
        /// previous assistant turn, before the next <see cref="SendMessageAsync"/>
        /// call. Consecutive calls are merged into a single user turn so that
        /// multiple parallel tool calls from one assistant turn report back
        /// together, as the Anthropic API expects.
        /// </summary>
        /// <param name="toolUseId">The <c>id</c> of the <c>tool_use</c> block this result corresponds to.</param>
        /// <param name="result">The tool's textual output (plain text or serialized JSON).</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="toolUseId"/> is null or empty.</exception>
        public void AddToolResult(string toolUseId, string result)
        {
            if (string.IsNullOrEmpty(toolUseId))
                throw new ArgumentException("Tool-use id must not be empty.", nameof(toolUseId));

            var block = new ContentBlock
            {
                Type = "tool_result",
                ToolUseId = toolUseId,
                Content = result ?? string.Empty
            };

            // Merge with the previous user turn if it is already carrying tool_results.
            if (ConversationHistory.Count > 0)
            {
                Message last = ConversationHistory[ConversationHistory.Count - 1];
                if (last.Role == "user" && last.Content is List<ContentBlock> existing)
                {
                    existing.Add(block);
                    return;
                }
            }

            ConversationHistory.Add(new Message
            {
                Role = "user",
                Content = new List<ContentBlock> { block }
            });
        }

        /// <summary>Discards every turn in <see cref="ConversationHistory"/>.</summary>
        public void ClearHistory()
        {
            ConversationHistory.Clear();
        }

        private async Task<MessageResponse> SendRequestAsync(List<Tool> tools, string systemPrompt)
        {
            var request = new MessageRequest
            {
                Model = _model,
                MaxTokens = DefaultMaxTokens,
                System = systemPrompt,
                Messages = ConversationHistory,
                Tools = tools
            };

            string requestJson = JsonConvert.SerializeObject(request, JsonSettings);

            using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, Endpoint))
            {
                httpRequest.Headers.Add("x-api-key", _apiKey);
                httpRequest.Headers.Add("anthropic-version", AnthropicVersion);
                httpRequest.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse;
                try
                {
                    httpResponse = await SharedHttpClient
                        .SendAsync(httpRequest, HttpCompletionOption.ResponseContentRead, CancellationToken.None)
                        .ConfigureAwait(false);
                }
                catch (HttpRequestException ex)
                {
                    throw new ClaudeApiException("Network failure while calling the Claude API.", ex);
                }
                catch (TaskCanceledException ex)
                {
                    throw new ClaudeApiException("Claude API request timed out.", ex);
                }

                using (httpResponse)
                {
                    string body = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        Debug.WriteLine(
                            $"[ClaudeApiClient] Non-success HTTP {(int)httpResponse.StatusCode} from {Endpoint}: {body}");
                        throw new ClaudeApiException(
                            $"Claude API returned HTTP {(int)httpResponse.StatusCode} {httpResponse.ReasonPhrase}. Body: {Truncate(body, ResponseBodyExcerptLimit)}",
                            httpResponse.StatusCode,
                            body);
                    }

                    MessageResponse parsed;
                    try
                    {
                        parsed = JsonConvert.DeserializeObject<MessageResponse>(body);
                    }
                    catch (JsonException ex)
                    {
                        Debug.WriteLine($"[ClaudeApiClient] Could not parse response body: {body}");
                        throw new ClaudeApiException(
                            $"Failed to parse Claude API response body as JSON. Body: {Truncate(body, ResponseBodyExcerptLimit)}",
                            ex);
                    }

                    if (parsed == null)
                        throw new ClaudeApiException("Claude API returned an empty response body.");

                    return parsed;
                }
            }
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s;
            return s.Substring(0, max) + "...";
        }
    }
}
