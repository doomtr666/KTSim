namespace KTSim.Models;

public class AIGrid
{
    public const int OverSamplingFactor = 4;
    public const int GridWidth = KillZone.GridWidth * OverSamplingFactor;
    public const int GridHeight = KillZone.GridHeight * OverSamplingFactor;
    public const float GridStep = KillZone.GridStep / OverSamplingFactor;

    // colliision grid
    public bool[,] CollisionGrid { get; } = new bool[GridWidth, GridHeight];

    public AIGrid(KillZone killZone)
    {
        // collision grid
        foreach (var agent in killZone.Operatives)
        {
            SplatCircle(agent.Position.X, agent.Position.Y, agent.Type.BaseDiameter / 2);
        }

        foreach (var terrain in killZone.Terrains)
        {
            SplatRectangle(terrain.Position.X - terrain.Width / 2, terrain.Position.Y - terrain.Height / 2, terrain.Width, terrain.Height);
        }
    }

    private void SplatCircle(float x, float y, float radius)
    {
        const float epsilon = 0.001f;

        var sx = (int)((x - radius + epsilon) / GridStep);
        var sy = (int)((y - radius + epsilon) / GridStep);
        var ex = (int)((x + radius - epsilon) / GridStep);
        var ey = (int)((y + radius - epsilon) / GridStep);

        for (var i = sx; i <= ex; i++)
        {
            for (var j = sy; j <= ey; j++)
            {
                if (i >= 0 && i < GridWidth && j >= 0 && j < GridHeight && SquareInCircle(x, y, radius, i * GridStep, j * GridStep, GridStep))
                {
                    CollisionGrid[i, j] = true;
                }
            }
        }
    }

    bool SquareInCircle(float x, float y, float radius, float rx, float ry, float rwidth)
    {
        return true;
    }

    private void SplatRectangle(float x, float y, float width, float height)
    {
        const float epsilon = 0.001f;

        var sx = (int)((x + epsilon) / GridStep);
        var sy = (int)((y + epsilon) / GridStep);
        var ex = (int)((x + width - epsilon) / GridStep);
        var ey = (int)((y + height - epsilon) / GridStep);

        for (var i = sx; i <= ex; i++)
        {
            for (var j = sy; j <= ey; j++)
            {
                if (i >= 0 && i < GridWidth && j >= 0 && j < GridHeight)
                    CollisionGrid[i, j] = true;
            }
        }
    }
}
