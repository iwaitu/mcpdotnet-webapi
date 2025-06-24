﻿namespace McpDotNet.Protocol.Messages;

/// <summary>
/// Base interface for all JSON-RPC messages in the MCP protocol.
/// </summary>
public interface IJsonRpcMessage
{
    /// <summary>
    /// JSON-RPC protocol version. Must be "2.0".
    /// </summary>
    string JsonRpc { get; }
}
