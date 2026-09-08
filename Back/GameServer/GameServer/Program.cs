using GameServer.Hubs;
using GameServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        var origins = Environment.GetEnvironmentVariable("PEPINO_FRONTEND_ORIGINS")
            ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? new[] { "http://localhost:5174", "http://localhost:5173" };

        builder.WithOrigins(origins)
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials();
    });
});

// Register services
builder.Services.AddSingleton<GameRoomManager>();

var app = builder.Build();

// Aplicar CORS antes de cualquier otra cosa
app.UseCors();

app.MapHub<GameHub>("/gamehub");

// HTTP endpoints for room management
app.MapGet("/api/rooms", (GameRoomManager roomManager) =>
{
    var activeRooms = roomManager.GetActiveRooms();
    return Results.Ok(activeRooms.Select(r => new
    {
        r.Id,
        PlayerCount = r.Players.Count,
        r.IsGameStarted,
        r.CreatedAt,
        r.GameStartedAt
    }));
});

app.MapGet("/api/rooms/{roomId}", (string roomId, GameRoomManager roomManager) =>
{
    var room = roomManager.GetRoom(roomId);
    if (room == null) return Results.NotFound();
    
    return Results.Ok(new
    {
        room.Id,
        Players = room.Players.Select(p => new { p.Name, CardCount = p.Hand.Count, p.HasWon }),
        room.IsGameStarted,
        room.CreatedAt,
        room.GameStartedAt,
        room.CanStartGame,
        room.IsGameActive
    });
});

app.Run();
