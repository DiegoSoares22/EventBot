using ConquerBot.Data;
using ConquerBot.Models;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;

namespace ConquerBot.Services;

// Roda 1x por minuto (agendado no Program.cs). Compara o horário atual (America/Sao_Paulo)
// contra as listas de eventos e posta aviso de "faltam X minutos" e "começou agora".
public class EventNotifierJob : IJob
{
    private readonly DiscordSocketClient _client;
    private readonly IConfiguration _config;
    private readonly ILogger<EventNotifierJob> _logger;

    private static readonly TimeZoneInfo FusoHorario =
        TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "E. South America Standard Time" : "America/Sao_Paulo");

    public EventNotifierJob(DiscordSocketClient client, IConfiguration config, ILogger<EventNotifierJob> logger)
    {
        _client = client;
        _config = config;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        if (_client.ConnectionState != ConnectionState.Connected) return;

        var agora = TimeZoneInfo.ConvertTime(DateTime.UtcNow, FusoHorario);

        await VerificarEventos(EventosData.Americano, agora, "Discord:CanalAmericanoId", "Servidor Americano");
        await VerificarEventos(EventosData.Europeu, agora, "Discord:CanalEuropeuId", "Servidor Europeu");
    }

    private async Task VerificarEventos(List<ConquerEvent> eventos, DateTime agora, string chaveCanal, string nomeRoleMencionada)
    {
        var canalIdTexto = _config[chaveCanal];
        if (string.IsNullOrWhiteSpace(canalIdTexto) || !ulong.TryParse(canalIdTexto, out var canalId))
        {
            return; // canal não configurado ainda (ex.: europeu sem horários/canal definidos)
        }

        foreach (var evento in eventos)
        {
            if (evento.DiaSemana != null && evento.DiaSemana != agora.DayOfWeek) continue;

            var avisoAntecipado = evento.Inicio - agora.TimeOfDay;
            var comecouAgora = agora.TimeOfDay.Hours == evento.Inicio.Hours && agora.TimeOfDay.Minutes == evento.Inicio.Minutes;

            if (comecouAgora)
            {
                await PostarAviso(canalId, $"🔔 **{evento.Nome}** começou agora!", nomeRoleMencionada);
            }
            else if (avisoAntecipado.TotalMinutes > 0 &&
                     Math.Round(avisoAntecipado.TotalMinutes) == evento.MinutosAvisoAntecipado)
            {
                await PostarAviso(canalId, $"⏰ **{evento.Nome}** começa em {evento.MinutosAvisoAntecipado} minutos.", nomeRoleMencionada);
            }
        }
    }

    private async Task PostarAviso(ulong canalId, string mensagem, string nomeRole)
    {
        if (_client.GetChannel(canalId) is not IMessageChannel canal)
        {
            _logger.LogWarning($"Canal {canalId} não encontrado.");
            return;
        }

        // A menção de role real (@Role) exige o Id do cargo; aqui posta texto simples com o nome.
        // Depois de criar as roles, você pode trocar "nomeRole" pelo formato <@&ID_DA_ROLE> para mencionar de verdade.
        await canal.SendMessageAsync($"{mensagem} ({nomeRole})");
    }
}