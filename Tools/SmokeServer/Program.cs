using CardGameServer.Data;
using CardGameServer.Rooms;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

var project = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
Directory.SetCurrentDirectory(Path.Combine(project, "../CardGameServer"));
await using var factory = new SmokeFactory();
factory.UseKestrel(0);
using var client = factory.CreateClient();
Directory.CreateDirectory(Path.Combine(project, "Temp"));
File.WriteAllText(Path.Combine(project, "Temp/Blackjack-smoke-server.txt"), client.BaseAddress!.AbsoluteUri);
Console.WriteLine("Isolated smoke server is ready.");
await Task.Delay(Timeout.Infinite);

sealed class SmokeFactory : WebApplicationFactory<RoomHost>
{
    readonly SqliteConnection database = new("Data Source=:memory:");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../CardGameServer/CardGameServer")));
        database.Open(); builder.UseEnvironment("Development");
        builder.UseSetting("Authentication:Google:ClientId", "smoke-test");
        builder.UseSetting("Authentication:Google:ClientSecret", "smoke-test");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(database));
            services.AddSingleton<SmokeDisconnect>(); services.AddSignalR(options => options.AddFilter<SmokeDisconnect>());
            services.AddTransient<IStartupFilter, SmokeEndpoints>();
        });
    }
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope(); scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
        return host;
    }
    public override async ValueTask DisposeAsync() { await base.DisposeAsync(); await database.DisposeAsync(); }
}
// The isolated fixture deliberately interrupts one real hub connection for recovery checks.
sealed class SmokeDisconnect : IHubFilter
{
    readonly ConcurrentDictionary<string, HubCallerContext> connections = new();
    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    { connections[context.Context.ConnectionId] = context.Context; await next(context); }
    public async Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
    { connections.TryRemove(context.Context.ConnectionId, out _); await next(context, exception); }
    public bool Disconnect(string userId)
    {
        var matches = connections.Values.Where(context => context.UserIdentifier == userId).ToArray();
        foreach (var context in matches) context.Features.Get<Microsoft.AspNetCore.Connections.Features.IConnectionLifetimeFeature>()!.Abort();
        return matches.Length > 0;
    }
}
// Test control exists only in this isolated, ephemeral loopback fixture.
sealed class SmokeEndpoints(SmokeDisconnect connections) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, following) =>
        {
            if (context.Request.Path != "/_smoke/disconnect") { await following(); return; }
            context.Response.StatusCode = connections.Disconnect(context.Request.Query["id"].ToString()) ? 204 : 404;
        });
        next(app);
    };
}
