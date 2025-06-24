using McpDotNet.Configuration;
using McpDotNet.Protocol.Transport;
using McpDotNet.Server;
using McpDotNet.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace McpDotNet;

/// <summary>
/// Extension to configure the MCP server with transports
/// </summary>
public static partial class McpServerBuilderExtensions
{
    /// <summary>
    /// Adds a server transport that uses stdin/stdout for communication.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    public static IMcpServerBuilder WithStdioServerTransport(this IMcpServerBuilder builder)
    {
        Throw.IfNull(builder);

        builder.Services.AddSingleton<IServerTransport, StdioServerTransport>();
        return builder;
    }

    /// <summary>
    /// Adds a server transport that uses HTTP listener for communication.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="serverOptions"></param>
    /// <param name="port"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IMcpServerBuilder WithHttpListenerSseServerTransport(this IMcpServerBuilder builder, McpServerOptions serverOptions, int port)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.AddSingleton(serverOptions); // 将配置项注册到 DI 中

        builder.Services.AddSingleton<IServerTransport>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new HttpListenerSseServerTransport(serverOptions, port, loggerFactory);
        });
        return builder;
    }

    /// <summary>
    /// adds a server transport that uses HTTP listener for communication.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="serverName"></param>
    /// <param name="port"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public static IMcpServerBuilder WithHttpListenerSseServerTransport(this IMcpServerBuilder builder,string serverName, int port)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.AddSingleton<IServerTransport>(sp =>
        {
            // 从 DI 中获取需要的依赖项，例如 McpServerOptions 和 ILoggerFactory
            var serverOptions = sp.GetService<McpServerOptions>();
            if (serverOptions is null)
            {
                throw new InvalidOperationException("McpServerOptions 未注册到 DI 容器中。");
            }
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            // 使用带有 McpServerOptions 的构造函数创建 HttpListenerSseServerTransport 实例
            return new HttpListenerSseServerTransport(serverName, port, loggerFactory);
        });
        return builder;
    }


}
