using System;
using System.Net;

namespace RevitMate.Core.Api
{
    /// <summary>
    /// Thrown when the Anthropic Claude API returns a non-success HTTP status,
    /// the response payload cannot be parsed as JSON, or the underlying
    /// network call fails (timeout, DNS error, TLS error, etc.).
    /// </summary>
    public class ClaudeApiException : Exception
    {
        /// <summary>
        /// The HTTP status code returned by the API. <c>default(HttpStatusCode)</c>
        /// (i.e. <c>0</c>) when the failure occurred before a response was received.
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// The raw response body returned by the API. <c>null</c> when no
        /// response was received (e.g. network or timeout failure).
        /// </summary>
        public string ResponseBody { get; }

        /// <summary>Creates an exception describing a client-side or pre-flight failure.</summary>
        public ClaudeApiException(string message) : base(message) { }

        /// <summary>Creates an exception wrapping a lower-level failure.</summary>
        public ClaudeApiException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>Creates an exception describing a failed HTTP response.</summary>
        public ClaudeApiException(string message, HttpStatusCode statusCode, string responseBody) : base(message)
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }
}
