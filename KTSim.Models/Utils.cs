global using Position = System.Numerics.Vector2;

namespace KTSim.Models;

public static class Extensions
{
    const float epsilon = 0.0001f;

    public static bool AlmostZero(this float value)
    {
        return MathF.Abs(value) < epsilon;
    }

    public static bool AlmostEqual(this float value, float other)
    {
        return MathF.Abs(value - other) < epsilon;
    }

    public static float Cross(this Position v1, Position v2)
    {
        return v1.X * v2.Y - v1.Y * v2.X;
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
            new Position(Center.X + Width/2, Center.Y + Height/2),
            new Position(Center.X + Width/2, Center.Y - Height/2),
        ];
    }

    public Segment[] GetSegments()
    {
        var corners = GetCorners();

        return [
            new Segment(corners[0], corners[1]),
            new Segment(corners[1], corners[2]),
            new Segment(corners[2], corners[3]),
            new Segment(corners[3], corners[0]),
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
        var s = rectangle.GetSegments();

        if (Intersects(segment, s[0]) || Intersects(segment, s[1]) || Intersects(segment, s[2]) || Intersects(segment, s[3]))
            return true;

        return false;
    }

    public static bool Intersects(Segment s1, Segment s2)
    {

        var r = s1.End - s1.Start;
        var s = s2.End - s2.Start;
        var rxs = r.Cross(s);
        var qpxr = (s2.Start - s1.Start).Cross(r);

        if (rxs.AlmostZero())
            return false;

        var t = (s2.Start - s1.Start).Cross(s) / rxs;
        var u = (s2.Start - s1.Start).Cross(r) / rxs;

        if (0 <= t && t <= 1 && 0 <= u && u <= 1)
            return true;

        return false;
    }
}