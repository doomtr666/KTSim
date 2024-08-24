using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using KTSim.Models;

namespace KTSim.Gui.Controls;

public class KillzoneViewControl : Control
{
    private MatchPlayer? _player;

    public MatchPlayer? Player
    {
        get { return _player; }
        set { SetAndRaise(PlayerProperty, ref _player, value); }
    }


    public static readonly DirectProperty<KillzoneViewControl, MatchPlayer?> PlayerProperty =
        AvaloniaProperty.RegisterDirect<KillzoneViewControl, MatchPlayer?>(
            nameof(Player),
            o => o.Player,
            (o, v) => o.Player = v);

    private IOperativeAction? _lastAction;

    public IOperativeAction? LastAction
    {
        get { return _lastAction; }
        set { SetAndRaise(LastActionProperty, ref _lastAction, value); InvalidateVisual(); }
    }

    public static readonly DirectProperty<KillzoneViewControl, IOperativeAction?> LastActionProperty =
        AvaloniaProperty.RegisterDirect<KillzoneViewControl, IOperativeAction?>(
            nameof(LastActionProperty),
            o => o.LastAction,
            (o, v) => o.LastAction = v);

    // brushes
    static private readonly IBrush WhiteBrush = Brush.Parse("White");
    static private readonly IBrush BlackBrush = Brush.Parse("Black");
    static private readonly IBrush LightGrayBrush = Brush.Parse("LightGray");
    static private readonly IBrush GrayBrush = Brush.Parse("Gray");
    static private readonly IBrush DarkGrayBrush = Brush.Parse("DarkGray");
    static private readonly IBrush OrangeBrush = Brush.Parse("Orange");
    static private readonly IBrush RedBrush = Brush.Parse("Red");
    static private readonly IBrush DarkRedBrush = Brush.Parse("DarkRed");
    static private readonly IBrush LightBlueBrush = Brush.Parse("LightBlue");
    static private readonly IBrush BlueBrush = Brush.Parse("Blue");
    static private readonly IBrush DarkBlueBrush = Brush.Parse("DarkBlue");
    static private readonly IBrush GreenBrush = Brush.Parse("Green");
    static private readonly IBrush PinkBrush = Brush.Parse("Pink");

    // pens
    static private readonly IPen BlackPen = new Pen(BlackBrush, 2);
    static private readonly IPen GrayPen = new Pen(GrayBrush, 2);
    static private readonly IPen BluePen = new Pen(BlueBrush, 2);
    static private readonly IPen GreenPen = new Pen(GreenBrush, 2);
    static private readonly IPen RedPen = new Pen(RedBrush, 2);
    static private readonly IPen OrangePen = new Pen(OrangeBrush, 2);

    public KillzoneViewControl()
    {
    }

    public sealed override void Render(DrawingContext context)
    {
        context.FillRectangle(WhiteBrush, new Rect(Bounds.Size));

        if (Player == null)
            return;

        foreach (var dropZone in Player.KillZone.DropZones)
        {
            var color = dropZone.Side == TurnSide.Attacker ? PinkBrush : LightBlueBrush;
            context.FillRectangle(color, new Rect(dropZone.Position.X, dropZone.Position.Y, dropZone.Width, dropZone.Height));
        }

        foreach (var objective in Player.KillZone.Objectives)
        {
            context.DrawEllipse(OrangeBrush, BlackPen,
                new Rect(objective.Position.X - Objective.Radius, objective.Position.Y - Objective.Radius, Objective.Radius * 2, Objective.Radius * 2));
        }

        foreach (var terrain in Player.KillZone.Terrains)
        {
            var pen = BlackPen;
            if (terrain.Type.HasFlag(TerrainType.Traversable))
                pen = GrayPen;

            var brush = LightGrayBrush;
            if (terrain.Type.HasFlag(TerrainType.Heavy))
                brush = DarkGrayBrush;

            context.DrawRectangle(brush, pen, new Rect(terrain.Position.X - terrain.Width / 2, terrain.Position.Y - terrain.Height / 2, terrain.Width, terrain.Height));
        }

        // draw neutralized operatives
        foreach (var operative in Player.CurrentOperativeStates)
        {
            if (operative.Status == OperativeStatus.Neutralized)
            {
                var brush = GrayBrush;
                var pen = BlackPen;

                float radius = operative.Type.BaseDiameter / 2;
                context.DrawEllipse(brush, pen, new Rect(operative.Position.X - radius, operative.Position.Y - radius, radius * 2, radius * 2));

                var text = new FormattedText(operative.Index.ToString(), System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 12, WhiteBrush);
                context.DrawText(text, new Point(operative.Position.X - text.Width / 2, operative.Position.Y - text.Height / 2));
            }
        }

        foreach (var operative in Player.CurrentOperativeStates)
        {
            if (operative.Status == OperativeStatus.Neutralized)
            {
                continue;
            }

            var brush = operative.Side == TurnSide.Attacker ? RedBrush : BlueBrush;
            var pen = BlackPen;

            if (_lastAction != null && _lastAction.Operative == operative.Index)
            {
                pen = OrangePen;
            }
            else if (operative.Status == OperativeStatus.Activated)
            {
                brush = operative.Side == TurnSide.Attacker ? DarkRedBrush : DarkBlueBrush;
            }

            float radius = operative.Type.BaseDiameter / 2;
            context.DrawEllipse(brush, pen, new Rect(operative.Position.X - radius, operative.Position.Y - radius, radius * 2, radius * 2));

            var text = new FormattedText(operative.Index.ToString(), System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 12, WhiteBrush);
            context.DrawText(text, new Point(operative.Position.X - text.Width / 2, operative.Position.Y - text.Height / 2));
        }

        if (_lastAction != null)
        {
            switch (_lastAction)
            {
                case OperativeMoveAction moveAction:
                    context.DrawLine(BluePen, new Point(Player.CurrentOperativeStates[moveAction.Operative].Position.X, Player.CurrentOperativeStates[moveAction.Operative].Position.Y), new Point(moveAction.Destination.X, moveAction.Destination.Y));
                    break;
                case OperativeDashAction dashAction:
                    context.DrawLine(GreenPen, new Point(Player.CurrentOperativeStates[dashAction.Operative].Position.X, Player.CurrentOperativeStates[dashAction.Operative].Position.Y), new Point(dashAction.Destination.X, dashAction.Destination.Y));
                    break;

                case OperativeShootAction shootAction:
                    context.DrawLine(RedPen, new Point(Player.CurrentOperativeStates[shootAction.Operative].Position.X, Player.CurrentOperativeStates[shootAction.Operative].Position.Y),
                        new Point(Player.CurrentOperativeStates[shootAction.Target].Position.X, Player.CurrentOperativeStates[shootAction.Target].Position.Y));
                    break;
            }
        }

        base.Render(context);
    }
}
