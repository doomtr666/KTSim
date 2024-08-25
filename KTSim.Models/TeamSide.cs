namespace KTSim.Models;

public enum TeamSide
{
    Attacker,
    Defender,
}

public static class TeamSideExtensions
{
    public static TeamSide GetOppositeSide(this TeamSide side)
    {
        return side switch
        {
            TeamSide.Attacker => TeamSide.Defender,
            TeamSide.Defender => TeamSide.Attacker,
            _ => throw new ArgumentException("Invalid side", nameof(side)),
        };
    }
}