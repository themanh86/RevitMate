using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RevitMate.Core.Models
{
    /// <summary>
    /// Top-level request body sent to <c>POST /v1/messages</c>.
    /// </summary>
    public class MessageRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("max_tokens")]
        public int MaxTokens { get; set; } = 4096;

        [JsonProperty("system", NullValueHandling = NullValueHandling.Ignore)]
        public string System { get; set; }

        [JsonProperty("messages")]
        public List<Message> Messages { get; set; } = new List<Message>();

        [JsonProperty("tools", NullValueHandling = NullValueHandling.Ignore)]
        public List<Tool> Tools { get; set; }
    }

    /// <summary>
    /// A single user or assistant turn in the conversation. <see cref="Content"/>
    /// may be either a plain <see cref="string"/> or a <see cref="List{T}"/> of
    /// <see cref="ContentBlock"/>, depending on whether the turn carries plain
    /// text or typed blocks (tool_use, tool_result, multiple text segments, etc.).
    /// </summary>
    public class Message
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        [JsonConverter(typeof(MessageContentConverter))]
        public object Content { get; set; }
    }

    /// <summary>
    /// One block inside a <see cref="Message"/>'s content array. The set of
    /// populated fields depends on <see cref="Type"/>:
    /// <list type="bullet">
    ///   <item><description><c>"text"</c> uses <see cref="Text"/>.</description></item>
    ///   <item><description><c>"tool_use"</c> uses <see cref="Id"/>, <see cref="Name"/>, <see cref="Input"/>.</description></item>
    ///   <item><description><c>"tool_result"</c> uses <see cref="ToolUseId"/> and <see cref="Content"/>.</description></item>
    /// </list>
    /// </summary>
    public class ContentBlock
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        // Populated when Type == "text".
        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }

        // Populated when Type == "tool_use".
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("input", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Input { get; set; }

        // Populated when Type == "tool_result".
        [JsonProperty("tool_use_id", NullValueHandling = NullValueHandling.Ignore)]
        public string ToolUseId { get; set; }

        [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
        public string Content { get; set; }
    }

    /// <summary>
    /// Declarative description of a tool Claude is allowed to invoke.
    /// <see cref="InputSchema"/> is an arbitrary JSON Schema object that
    /// describes the tool's argument shape.
    /// </summary>
    public class Tool
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("input_schema")]
        public JObject InputSchema { get; set; }
    }

    /// <summary>
    /// Response body returned by <c>POST /v1/messages</c>.
    /// </summary>
    public class MessageResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public List<ContentBlock> Content { get; set; } = new List<ContentBlock>();

        [JsonProperty("stop_reason")]
        public string StopReason { get; set; }

        [JsonProperty("usage")]
        public Usage Usage { get; set; }
    }

    /// <summary>
    /// Token-usage statistics returned alongside each response.
    /// </summary>
    public class Usage
    {
        [JsonProperty("input_tokens")]
        public int InputTokens { get; set; }

        [JsonProperty("output_tokens")]
        public int OutputTokens { get; set; }
    }

    /// <summary>
    /// Serializes <see cref="Message.Content"/> as either a JSON string or a
    /// JSON array of <see cref="ContentBlock"/>, depending on the runtime type.
    /// Attached via <c>[JsonConverter]</c> on the <see cref="Message.Content"/>
    /// property only — it is not registered globally.
    /// </summary>
    internal class MessageContentConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.Null:
                    return null;
                case JsonToken.String:
                    return (string)reader.Value;
                case JsonToken.StartArray:
                    return serializer.Deserialize<List<ContentBlock>>(reader);
                default:
                    throw new JsonSerializationException(
                        $"Unexpected token {reader.TokenType} when reading Message.Content; expected string or array.");
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            switch (value)
            {
                case null:
                    writer.WriteNull();
                    break;
                case string s:
                    writer.WriteValue(s);
                    break;
                case IEnumerable<ContentBlock> blocks:
                    serializer.Serialize(writer, blocks);
                    break;
                default:
                    throw new JsonSerializationException(
                        $"Message.Content must be a string or a sequence of ContentBlock; got {value.GetType().FullName}.");
            }
        }
    }
}
