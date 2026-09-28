using ConquerBot.Models;

namespace ConquerBot.Services;

public static class ProximoEventoCalculator
{
    public static (ConquerEvent Evento, TimeSpan TempoRestante)? Calcular(
        List<ConquerEvent> eventos, DateTime agora)
    {
        if (eventos.Count == 0) return null;

        (ConquerEvent Evento, DateTime Quando)? melhor = null;

        foreach (var evento in eventos)
        {
            var proximaOcorrencia = ProximaOcorrencia(evento, agora);
            if (melhor == null || proximaOcorrencia < melhor.Value.Quando)
            {
                melhor = (evento, proximaOcorrencia);
            }
        }

        if (melhor == null) return null;
        return (melhor.Value.Evento, melhor.Value.Quando - agora);
    }

    private static DateTime ProximaOcorrencia(ConquerEvent evento, DateTime agora)
    {
        // Evento diário (Queen/DeityLand): procura o próximo horário igual, hoje ou amanhã
        if (evento.DiaSemana == null)
        {
            var candidatoHoje = agora.Date + evento.Inicio;
            return candidatoHoje > agora ? candidatoHoje : candidatoHoje.AddDays(1);
        }

        // Evento semanal: soma dias até o dia da semana certo
        var diasAte = ((int)evento.DiaSemana.Value - (int)agora.DayOfWeek + 7) % 7;
        var candidato = agora.Date.AddDays(diasAte) + evento.Inicio;

        // Se caiu hoje mas o horário já passou, empurra pra próxima semana
        if (diasAte == 0 && candidato <= agora)
        {
            candidato = candidato.AddDays(7);
        }

        return candidato;
    }
}