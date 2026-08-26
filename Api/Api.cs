namespace Zamboni3;

public class Api
{
    private readonly string _address;

    public Api()
    {
        _address = "http://0.0.0.0:"+Program.ZamboniConfig.ApiServerPort;
    }

    public async Task StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        app.MapGet("/" + Program.ZamboniConfig.ApiServerIdentifier + "/status", () => Results.Json(new
        {
            serverVersion = Program.ZamboniConfig.ApiServerIdentifier,
            onlineUsersCount = ServerManager.GetServerPlayers().Count,
            onlineUsers = string.Join(", ", ServerManager.GetServerPlayers().Values.Select(serverPlayer => serverPlayer.UserIdentification.mName)),
            queuedUsers = ServerManager.GetQueuedPlayers().Count,
            activeGames = ServerManager.GetServerGames().Count
        }));

        await app.RunAsync(_address);
    }
}