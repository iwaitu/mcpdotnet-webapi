using McpDotNet.Hosting;
using McpDotNet.Protocol.Types;
using McpDotNet.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text.Json;

namespace McpDotNet.Webapi;

/// <summary>
/// 扩展方法，用于将 API 控制器中的所有 public 方法添加为工具。
/// </summary>
public static class McpApiFunctionExtensions
{
    /// <summary>
    /// 扫描 WebApplication 所在程序集中的 API 控制器（类名以 "Controller" 结尾或标记 [ApiController]）并将其所有返回 IActionResult 的 public 方法添加为工具。
    /// </summary>
    /// <param name="app">WebApplication 实例</param>
    /// <param name="assembly">要扫描的程序集，默认为调用程序集</param>
    /// <returns>返回更新后的 WebApplication 实例</returns>
    public static async Task<WebApplication> UseMcpApiFunctions(this WebApplication app, Assembly? assembly = null)
    {
        if (app is null)
            throw new ArgumentNullException(nameof(app));

        // 使用 app.Services 获取完整的 IServiceProvider
        IServiceProvider serviceProvider = app.Services;
        assembly ??= Assembly.GetCallingAssembly();
        List<AIFunction> functions = new List<AIFunction>();

        foreach (var type in assembly.GetTypes())
        {
            if (!type.IsClass || (!type.Name.EndsWith("Controller", StringComparison.Ordinal) && type.GetCustomAttribute<ApiControllerAttribute>() == null))
                continue;

            object? controllerInstance = null;
            try
            {
                // 使用 DI 容器创建控制器实例
                controllerInstance = ActivatorUtilities.CreateInstance(serviceProvider, type);
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (method.DeclaringType != type)
                    continue;

                if (typeof(IActionResult).IsAssignableFrom(method.ReturnType))
                    continue;

                var function = AIFunctionFactory.Create(method, controllerInstance, new()
                {
                    Name = $"{type.Name}.{method.Name}"
                });

                functions.Add(function);
            }
        }

        List<Tool> tools = [];
        Dictionary<string, Func<RequestContext<CallToolRequestParams>, CancellationToken, Task<CallToolResponse>>> callbacks = [];
        foreach (AIFunction function in functions)
        {
            
            tools.Add(new()
            {
                Name = function.Name,
                Description = function.Description,
                InputSchema = JsonSerializer.Deserialize<JsonSchema>(function.JsonSchema),
            });

            callbacks.Add(function.Name, async (request, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                object? result;
                try
                {
                    result = await function.InvokeAsync(
                        new AIFunctionArguments(
                            request.Params?.Arguments == null
                                ? null
                                : new Dictionary<string, object?>(request.Params.Arguments.ToDictionary(
                                    kv => kv.Key,
                                    kv => (object?)kv.Value
                                ))
                        ),
                        cancellationToken
                    ).ConfigureAwait(false);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    return new CallToolResponse()
                    {
                        IsError = true,
                        Content = [new() { Text = e.Message, Type = "text" }],
                    };
                }

                switch (result)
                {
                    case JsonElement je when je.ValueKind == JsonValueKind.Null:
                        return new() { Content = [] };

                    case JsonElement je when je.ValueKind == JsonValueKind.Array:
                        return new() { Content = je.EnumerateArray().Select(x => new Content() { Text = x.ToString(), Type = "text" }).ToList() };

                    default:
                        return new() { Content = [new() { Text = result?.ToString(), Type = "text" }] };
                }
            });
        }

        var mcpHostedService = app.Services.GetRequiredService<McpServerHostedService>();

        // 停止 server

        if (mcpHostedService != null)
        {
            await mcpHostedService.StopAsync(CancellationToken.None).ConfigureAwait(false);

            mcpHostedService.Server.SetListToolsHandler((_, _) => Task.FromResult(new ListToolsResult() { Tools = tools }));
            mcpHostedService.Server.SetCallToolHandler(async (request, cancellationToken) =>
            {
                if (request.Params is null || !callbacks.TryGetValue(request.Params.Name, out var callback))
                {
                    throw new McpServerException($"Unknown tool '{request.Params?.Name}'");
                }

                return await callback(request, cancellationToken).ConfigureAwait(false);
            });

            await mcpHostedService.StartAsync(CancellationToken.None).ConfigureAwait(false);
        }

        return app;
    }
}

