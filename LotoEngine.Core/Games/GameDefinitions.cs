using LotoEngine.Core.Domain;

namespace LotoEngine.Core.Games;

public interface IGameDefinition
{
    GameType Game { get; }
    int RangeMin { get; }
    int RangeMax { get; }
    int NumbersPerTicket { get; }
    IReadOnlyList<GroupRange> Groups { get; }
    bool HasExtra { get; }
    IReadOnlyList<string>? ExtraDomain { get; }
}

public sealed class LotofacilDefinition : IGameDefinition
{
    public GameType Game => GameType.Lotofacil;
    public int RangeMin => 1;
    public int RangeMax => 25;
    public int NumbersPerTicket => 15;
    public IReadOnlyList<GroupRange> Groups => new[]
    {
        new GroupRange("G1",1,5), new GroupRange("G2",6,9), new GroupRange("G3",10,14), new GroupRange("G4",15,19), new GroupRange("G5",20,25)
    };
    public bool HasExtra => false;
    public IReadOnlyList<string>? ExtraDomain => null;
}

public sealed class MegaSenaDefinition : IGameDefinition
{
    public GameType Game => GameType.MegaSena;
    public int RangeMin => 1;
    public int RangeMax => 60;
    public int NumbersPerTicket => 6;
    public IReadOnlyList<GroupRange> Groups => new[]
    {
        new GroupRange("G1",1,12), new GroupRange("G2",13,24), new GroupRange("G3",25,36), new GroupRange("G4",37,48), new GroupRange("G5",49,60)
    };
    public bool HasExtra => false;
    public IReadOnlyList<string>? ExtraDomain => null;
}

public sealed class DiaDeSorteDefinition : IGameDefinition
{
    public GameType Game => GameType.DiaDeSorte;
    public int RangeMin => 1;
    public int RangeMax => 31;
    public int NumbersPerTicket => 7;
    public IReadOnlyList<GroupRange> Groups => new[]
    {
        new GroupRange("G1",1,7), new GroupRange("G2",8,14), new GroupRange("G3",15,21), new GroupRange("G4",22,28), new GroupRange("G5",29,31)
    };
    public bool HasExtra => true;
    public IReadOnlyList<string>? ExtraDomain => new[]
    {
        "Janeiro","Fevereiro","Março","Abril","Maio","Junho","Julho","Agosto","Setembro","Outubro","Novembro","Dezembro"
    };
}

public static class GameDefinitions
{
    public static IGameDefinition For(GameType game) => game switch
    {
        GameType.Lotofacil => new LotofacilDefinition(),
        GameType.MegaSena => new MegaSenaDefinition(),
        GameType.DiaDeSorte => new DiaDeSorteDefinition(),
        _ => throw new ArgumentOutOfRangeException(nameof(game), game, null)
    };
}
