namespace KTSim.Models;

public struct Position
{
    public float X;
    public float Y;

    public Position(float x, float y)
    {
        X = x;
        Y = y;
    }
}

public struct Rectangle
{
    public Position Center;
    public float Width;
    public float Height;

    public Rectangle(Position center, float width, float height)
    {
        Center = center;
        Width = width;
        Height = height;
    }

    public Position[] GetCorners()
    {
        return [
            new Position(Center.X - Width/2, Center.Y - Height/2),
            new Position(Center.X - Width/2, Center.Y + Height/2),
            new Position(Center.X + Width/2, Center.Y - Height/2),
            new Position(Center.X + Width/2, Center.Y + Height/2),
        ];
    }
}

public struct Circle
{
    public Position Center;
    public float Radius;

    public Circle(Position center, float radius)
    {
        Center = center;
        Radius = radius;
    }
}

public struct Segment
{
    public Position Start;
    public Position End;

    public Segment(Position start, Position end)
    {
        Start = start;
        End = end;
    }
}

public static class Utils
{
    public static bool Intersects(Circle circle, Rectangle rectangle)
    {
        // Find the closest point to the circle within the rectangle
        float closestX = Math.Clamp(circle.Center.X, rectangle.Center.X - rectangle.Width / 2, rectangle.Center.X + rectangle.Width / 2);
        float closestY = Math.Clamp(circle.Center.Y, rectangle.Center.Y - rectangle.Height / 2, rectangle.Center.Y + rectangle.Height / 2);

        // Calculate the distance between the circle's center and this closest point
        float distanceX = circle.Center.X - closestX;
        float distanceY = circle.Center.Y - closestY;

        // If the distance is less than the circle's radius, an intersection occurs
        float distanceSquared = (distanceX * distanceX) + (distanceY * distanceY);
        return distanceSquared < (circle.Radius * circle.Radius);
    }

    public static bool Intersects(Circle c1, Circle c2)
    {
        return Distance(c1.Center, c2.Center) < (c1.Radius + c2.Radius);
    }

    public static float Distance(Position p1, Position p2)
    {
        return MathF.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
    }

    public static bool Intersects(Segment segment, Circle circle)
    {
        return false;
    }

    public static bool Intersects(Segment segment, Rectangle rectangle)
    {
        return false;
    }
}