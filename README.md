# McpDotNet WebAPI

本项目基于 [https://github.com/PederHP/mcpdotnet](https://github.com/PederHP/mcpdotnet) 开发，特此感谢原作者的开源贡献。

## 简介

本库为 Model Context Protocol (MCP) 的 .NET 实现，适用于 AI、LLM 等相关场景。

## 使用方法

Demo 参考 https://github.com/iwaitu/mcpwebapi 

服务端：

program.cs
```
using McpDotNet;
using McpDotNet.Webapi;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddMcpServer()
    .WithHttpListenerSseServerTransport("testserver", 8100);



builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 这里将所有返回类型为非 IActionResult 的 public 方法注册为 mcp 方法
await app.UseMcpApiFunctions(typeof(Program).Assembly);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

```

控制器代码：

```
[McpToolType]
[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    [McpTool, Description("获取天气情况")]
    public IEnumerable<WeatherForecast> Get([Description("城市名称")]string city)
    {
        return Enumerable.Range(1, 1).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)],
            City = city
        })
        .ToArray();
    }
}
```

客户端代码：

```
McpServerConfig serverConfig = new()
{
    Id = "everything",
    Name = "Everything",
    TransportType = TransportTypes.Sse,
    Location = "http://localhost:3500/sse",
};
McpClientOptions clientOptions = new()
{
    ClientInfo = new() { Name = "SimpleToolsConsole", Version = "1.0.0" }
};
var mcpClient = await McpClientFactory.CreateAsync(serverConfig, clientOptions);
var tools = await mcpClient.GetAIFunctionsAsync();

var messages = new List<ChatMessage>
    {
        new ChatMessage(ChatRole.User, "你有什么能力？")
    };
var chatOptions = new ChatOptions
{
    Temperature = 0.5f,
    Tools = tools.ToArray()
};
           
var res = await _chatClient.GetResponseAsync(messages, chatOptions);
return res.Messages.FirstOrDefault()?.Text;
```

## 致谢

感谢 [PederHP/mcpdotnet](https://github.com/PederHP/mcpdotnet) 项目的启发与贡献。

---

This project is based on [https://github.com/PederHP/mcpdotnet](https://github.com/PederHP/mcpdotnet). Many thanks to the original author for the open-source contribution.
