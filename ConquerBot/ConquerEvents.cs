namespace ConquerBot.Models;

public enum ServidorTipo
{
    Americano,
    Europeu
}

// DiaSemana = null significa "todos os dias" (ex.: Queen/DeityLand)
public record ConquerEvent(
    string Nome,
    ServidorTipo Servidor,
    DayOfWeek? DiaSemana,
    TimeSpan Inicio,
    TimeSpan? Fim = null,
    int MinutosAvisoAntecipado = 10
);