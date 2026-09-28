using ConquerBot;
using ConquerBot.Services;
using Discord;
using Discord.WebSocket;
using Quartz;

var builder = Host.CreateApplicationBuilder(args);

var socketConfig = new DiscordSocketConfig
{
    // GuildMembers é necessário para o bot enxergar/gerenciar membros e roles corretamente
    GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers
};

builder.Services.AddSingleton(socketConfig);
builder.Services.AddSingleton<DiscordSocketClient>();

builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("EventNotifierJob");
    q.AddJob<EventNotifierJob>(opts => opts.WithIdentity(jobKey));
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("EventNotifierJob-trigger")
        .WithCronSchedule("0 * * * * ?")); // dispara no segundo 0 de cada minuto
});
builder.Services.AddQuartzHostedService(opts => opts.WaitForJobsToComplete = true);

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
