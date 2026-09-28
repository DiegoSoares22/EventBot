using ConquerBot.Models;

namespace ConquerBot.Data;

public static class EventosData
{
    // Horários assumidos em fuso America/Sao_Paulo — confirme se é esse o fuso correto
    // antes de colocar em produção (ver Worker.cs / EventNotifierJob.cs).
    public static readonly List<ConquerEvent> Americano = new()
    {
        new("Class Pk", ServidorTipo.Americano, DayOfWeek.Monday, new TimeSpan(23, 25, 0), new TimeSpan(23, 30, 0)),
        new("Skill Team Pk", ServidorTipo.Americano, DayOfWeek.Wednesday, new TimeSpan(23, 45, 0)),
        new("Elite Pk", ServidorTipo.Americano, DayOfWeek.Friday, new TimeSpan(23, 55, 0)),
        new("Cross Capture the Flag", ServidorTipo.Americano, DayOfWeek.Saturday, new TimeSpan(1, 0, 0), new TimeSpan(1, 30, 0)),
        new("Team Pk", ServidorTipo.Americano, DayOfWeek.Saturday, new TimeSpan(22, 45, 0), new TimeSpan(23, 0, 0)),
        new("Capture the Flag", ServidorTipo.Americano, DayOfWeek.Sunday, new TimeSpan(1, 0, 0), new TimeSpan(1, 30, 0)),
        new("Guild War", ServidorTipo.Americano, DayOfWeek.Sunday, new TimeSpan(18, 30, 0), new TimeSpan(19, 0, 0)),
        new("Queen (DeityLand)", ServidorTipo.Americano, null, new TimeSpan(23, 10, 0)),
        new("Queen (DeityLand)", ServidorTipo.Americano, null, new TimeSpan(2, 10, 0)),
    };

    // PARCIAL: faltam Class Pk, Team Pk, Capture the Flag, Guild War e Queen/DeityLand do europeu.
    // CrossCTF: dia assumido como Sexta (mesmo dia do Elite Pk) — CONFIRME antes de rodar em produção.
    public static readonly List<ConquerEvent> Europeu = new()
    {
        new("Skill Team Pk", ServidorTipo.Europeu, DayOfWeek.Wednesday, new TimeSpan(17, 0, 0)),
        new("Elite Pk", ServidorTipo.Europeu, DayOfWeek.Friday, new TimeSpan(16, 0, 0)),
        new("Cross Capture the Flag", ServidorTipo.Europeu, DayOfWeek.Friday, new TimeSpan(17, 0, 0)),
    };
}