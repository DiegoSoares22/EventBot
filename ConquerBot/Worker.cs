using ConquerBot.Data;
using ConquerBot.Models;
using ConquerBot.Services;
using Discord;
using Discord.WebSocket;

namespace ConquerBot;

public class Worker : BackgroundService
{
    private readonly DiscordSocketClient _client;
    private readonly IConfiguration _config;
    private readonly ILogger<Worker> _logger;

    public const string RoleAmericano = "Servidor Americano";
    public const string RoleEuropeu = "Servidor Europeu";

    public Worker(DiscordSocketClient client, IConfiguration config, ILogger<Worker> logger)
    {
        _client = client;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _client.SlashCommandExecuted += SlashCommandHandler;

        var token = _config["Discord:Token"];
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogError("Token do Discord não encontrado. Configure em User Secrets como Discord:Token.");
            return;
        }

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        // mantém o Worker vivo até o host ser encerrado
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private Task LogAsync(LogMessage msg)
    {
        _logger.LogInformation(msg.ToString());
        return Task.CompletedTask;
    }

    private async Task ReadyAsync()
    {
        var registrar = new SlashCommandBuilder()
            .WithName("registrar")
            .WithDescription("Define em qual servidor você joga (Americano ou Europeu)")
            .AddOption("servidor", ApplicationCommandOptionType.String, "Escolha o servidor", isRequired: true,
                choices: new[]
                {
                    new ApplicationCommandOptionChoiceProperties { Name = "Americano", Value = "americano" },
                    new ApplicationCommandOptionChoiceProperties { Name = "Europeu", Value = "europeu" }
                });

        var meuServidor = new SlashCommandBuilder()
            .WithName("meuservidor")
            .WithDescription("Mostra qual servidor você tem registrado");

        var proximoEvento = new SlashCommandBuilder()
            .WithName("proximoevento")
            .WithDescription("Mostra o próximo evento do seu servidor");

        foreach (var guild in _client.Guilds)
        {
            try
            {
                // Comandos por guild (aparecem na hora). Comandos globais do Discord
                // podem levar até 1h para propagar — não use isso enquanto testa.
                await guild.CreateApplicationCommandAsync(registrar.Build());
                await guild.CreateApplicationCommandAsync(meuServidor.Build());
                await guild.CreateApplicationCommandAsync(proximoEvento.Build());
                _logger.LogInformation($"Comandos registrados no servidor {guild.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Falha ao registrar comandos em {guild.Name}");
            }
        }
    }

    private Task SlashCommandHandler(SocketSlashCommand command)
    {
        _ = command.Data.Name switch
        {
            "registrar" => HandleRegistrar(command),
            "meuservidor" => HandleMeuServidor(command),
            "proximoevento" => HandleProximoEvento(command),
            _ => Task.CompletedTask
        };
        return Task.CompletedTask;
    }

    private async Task HandleRegistrar(SocketSlashCommand command)
    {
        var servidorEscolhido = (string)command.Data.Options.First().Value;

        if (command.User is not SocketGuildUser guildUser)
        {
            await command.RespondAsync("Esse comando só funciona dentro do servidor.", ephemeral: true);
            return;
        }

        var nomeRoleAlvo = servidorEscolhido == "americano" ? RoleAmericano : RoleEuropeu;
        var nomeRoleOposto = servidorEscolhido == "americano" ? RoleEuropeu : RoleAmericano;

        var roleAlvo = guildUser.Guild.Roles.FirstOrDefault(r => r.Name == nomeRoleAlvo);
        var roleOposto = guildUser.Guild.Roles.FirstOrDefault(r => r.Name == nomeRoleOposto);

        if (roleAlvo == null)
        {
            await command.RespondAsync(
                $"A role '{nomeRoleAlvo}' não existe nesse servidor ainda. Peça para um admin criá-la (Configurações do Servidor → Cargos).",
                ephemeral: true);
            return;
        }

        try
        {
            if (roleOposto != null && guildUser.Roles.Any(r => r.Id == roleOposto.Id))
                await guildUser.RemoveRoleAsync(roleOposto);

            if (!guildUser.Roles.Any(r => r.Id == roleAlvo.Id))
                await guildUser.AddRoleAsync(roleAlvo);

            await command.RespondAsync(
                $"Registrado como jogador do servidor **{nomeRoleAlvo.Replace("Servidor ", "")}**.", ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao atribuir role no /registrar");
            await command.RespondAsync(
                "Não consegui atribuir a role. Verifique se a role do bot está acima das roles de servidor na hierarquia de Cargos.",
                ephemeral: true);
        }
    }

    private async Task HandleMeuServidor(SocketSlashCommand command)
    {
        if (command.User is not SocketGuildUser guildUser)
        {
            await command.RespondAsync("Use esse comando dentro do servidor.", ephemeral: true);
            return;
        }

        var temAmericano = guildUser.Roles.Any(r => r.Name == RoleAmericano);
        var temEuropeu = guildUser.Roles.Any(r => r.Name == RoleEuropeu);

        if (temAmericano) await command.RespondAsync("Você está registrado no servidor **Americano**.", ephemeral: true);
        else if (temEuropeu) await command.RespondAsync("Você está registrado no servidor **Europeu**.", ephemeral: true);
        else await command.RespondAsync("Você ainda não está registrado. Use /registrar primeiro.", ephemeral: true);
    }

    private async Task HandleProximoEvento(SocketSlashCommand command)
    {
        if (command.User is not SocketGuildUser guildUser)
        {
            await command.RespondAsync("Use esse comando dentro do servidor.", ephemeral: true);
            return;
        }

        ServidorTipo? servidor = null;
        if (guildUser.Roles.Any(r => r.Name == RoleAmericano)) servidor = ServidorTipo.Americano;
        else if (guildUser.Roles.Any(r => r.Name == RoleEuropeu)) servidor = ServidorTipo.Europeu;

        if (servidor == null)
        {
            await command.RespondAsync("Você ainda não está registrado. Use /registrar primeiro.", ephemeral: true);
            return;
        }

        var eventos = servidor == ServidorTipo.Americano ? EventosData.Americano : EventosData.Europeu;

        if (eventos.Count == 0)
        {
            await command.RespondAsync("Nenhum evento cadastrado para o seu servidor ainda.", ephemeral: true);
            return;
        }

        var resultado = ProximoEventoCalculator.Calcular(eventos, DateTime.Now);
        if (resultado == null)
        {
            await command.RespondAsync("Não consegui calcular o próximo evento.", ephemeral: true);
            return;
        }

        var (evento, tempoRestante) = resultado.Value;
        var horas = (int)tempoRestante.TotalHours;
        var minutos = tempoRestante.Minutes;

        await command.RespondAsync(
            $"Próximo evento: **{evento.Nome}** — em {horas}h{minutos:D2}min.", ephemeral: true);
    }
}