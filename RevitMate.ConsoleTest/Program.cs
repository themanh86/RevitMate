using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RevitMate.Core.Api;
using RevitMate.Core.Models;

namespace RevitMate.ConsoleTest
{
    /// <summary>
    /// Interactive smoke test for <see cref="ClaudeApiClient"/>. Reads the API
    /// key from <c>appsettings.json</c>, runs a REPL loop, and exercises the
    /// full tool-use round-trip with a dummy <c>get_current_time</c> tool.
    /// </summary>
    internal static class Program
    {
        private const int MaxToolRoundsPerTurn = 10;

        private static async Task<int> Main(string[] args)
        {
            IConfiguration configuration;
            try
            {
                configuration = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                    .Build();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to load configuration: {ex.Message}");
                return 1;
            }

            string apiKey = configuration["ClaudeApiKey"];
            string model = configuration["Model"] ?? "claude-sonnet-4-5";

            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("REPLACE_", StringComparison.Ordinal))
            {
                Console.Error.WriteLine(
                    "ClaudeApiKey is missing. Copy appsettings.template.json to appsettings.json and set your key.");
                return 1;
            }

            ClaudeApiClient client;
            try
            {
                client = new ClaudeApiClient(apiKey, model);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to construct ClaudeApiClient: {ex.Message}");
                return 1;
            }

            List<Tool> tools = BuildTools();
            const string systemPrompt =
                "You are RevitMate's smoke-test assistant. Be concise. "
                + "When a tool can answer a question, prefer calling it over guessing.";

            PrintBanner(model);

            while (true)
            {
                Console.Write("You: ");
                string input = Console.ReadLine();

                if (input == null)
                {
                    // EOF (Ctrl+Z on Windows, Ctrl+D on Unix) ends the session.
                    Console.WriteLine();
                    break;
                }

                input = input.Trim();
                if (input.Length == 0) continue;

                if (string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase))
                    break;

                if (string.Equals(input, "clear", StringComparison.OrdinalIgnoreCase))
                {
                    client.ClearHistory();
                    Console.WriteLine("(Conversation history cleared.)");
                    continue;
                }

                try
                {
                    await RunOneTurnAsync(client, input, tools, systemPrompt).ConfigureAwait(false);
                }
                catch (ClaudeApiException ex)
                {
                    Console.Error.WriteLine($"Claude API error: {ex.Message}");
                    if (!string.IsNullOrEmpty(ex.ResponseBody))
                        Console.Error.WriteLine($"  Response body: {ex.ResponseBody}");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Unexpected error: {ex.GetType().Name}: {ex.Message}");
                }
            }

            Console.WriteLine("Bye.");
            return 0;
        }

        /// <summary>
        /// Drives a single user turn end-to-end: sends the prompt, then loops on
        /// tool_use rounds (printing each tool call, executing it, and feeding
        /// the result back) until Claude produces a final text answer.
        /// </summary>
        private static async Task RunOneTurnAsync(
            ClaudeApiClient client,
            string userText,
            List<Tool> tools,
            string systemPrompt)
        {
            MessageResponse response = await client
                .SendMessageAsync(userText, tools, systemPrompt)
                .ConfigureAwait(false);
            PrintUsage(response);

            int round = 0;
            while (response.StopReason == "tool_use" && round < MaxToolRoundsPerTurn)
            {
                round++;

                // Any interleaved text from the assistant before the tool calls.
                foreach (ContentBlock block in response.Content)
                {
                    if (block.Type == "text" && !string.IsNullOrWhiteSpace(block.Text))
                        Console.WriteLine($"Claude: {block.Text}");
                }

                // Execute every tool_use block from this turn; the client batches
                // the resulting tool_result blocks into a single user message.
                foreach (ContentBlock block in response.Content)
                {
                    if (block.Type != "tool_use") continue;

                    string inputJson = block.Input?.ToString(Formatting.None) ?? "{}";
                    Console.WriteLine($"AI wants to call tool: {block.Name}  input={inputJson}");

                    string toolResult = ExecuteTool(block.Name, block.Input);
                    Console.WriteLine($"  Tool result: {toolResult}");

                    client.AddToolResult(block.Id, toolResult);
                }

                // Continue without a new user message so Claude consumes the tool_results.
                response = await client.SendMessageAsync(null, tools, systemPrompt).ConfigureAwait(false);
                PrintUsage(response);
            }

            if (round >= MaxToolRoundsPerTurn && response.StopReason == "tool_use")
            {
                Console.Error.WriteLine(
                    $"Stopped after {MaxToolRoundsPerTurn} tool-use rounds without a final answer.");
                return;
            }

            bool printedAny = false;
            foreach (ContentBlock block in response.Content)
            {
                if (block.Type == "text" && !string.IsNullOrWhiteSpace(block.Text))
                {
                    Console.WriteLine($"Claude: {block.Text}");
                    printedAny = true;
                }
            }

            if (!printedAny)
            {
                Console.WriteLine($"Claude: (no text content; stop_reason={response.StopReason ?? "<null>"})");
            }
        }

        private static List<Tool> BuildTools()
        {
            return new List<Tool>
            {
                new Tool
                {
                    Name = "get_current_time",
                    Description = "Returns the current server date and time.",
                    InputSchema = new JObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JObject(),
                        ["required"] = new JArray()
                    }
                }
            };
        }

        /// <summary>
        /// Routes a tool name to its local handler. Unknown tools produce a
        /// JSON-encoded error so Claude can recover gracefully on the next turn.
        /// </summary>
        private static string ExecuteTool(string name, JObject input)
        {
            switch (name)
            {
                case "get_current_time":
                    return DateTime.UtcNow.ToString("o");
                default:
                    return $"{{\"error\":\"Unknown tool '{name}' in smoke test.\"}}";
            }
        }

        private static void PrintUsage(MessageResponse response)
        {
            if (response?.Usage == null) return;
            Console.WriteLine(
                $"  [tokens] input={response.Usage.InputTokens} output={response.Usage.OutputTokens}  stop={response.StopReason}");
        }

        private static void PrintBanner(string model)
        {
            Console.WriteLine("RevitMate ConsoleTest - REPL");
            Console.WriteLine($"Model: {model}");
            Console.WriteLine("Type 'exit' to quit, 'clear' to reset history.");
            Console.WriteLine("Tools available: get_current_time");
            Console.WriteLine("Try: \"What time is it?\"");
            Console.WriteLine(new string('-', 60));
        }
    }
}
