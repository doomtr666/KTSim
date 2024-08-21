using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using KTSim.Gui.ViewModels;
using KTSim.Models;

namespace KTSim.Gui.Controls;

public class KillzoneViewControl : Control
{
    private Simulator? _simulator;

    public Simulator? Simulator
    {
        get { return _simulator; }
        set { SetAndRaise(SimulatorProperty, ref _simulator, value); }
    }

    public static readonly DirectProperty<KillzoneViewControl, Simulator?> SimulatorProperty =
        AvaloniaProperty.RegisterDirect<KillzoneViewControl, Simulator?>(
            nameof(Simulator),
            o => o.Simulator,
            (o, v) => o.Simulator = v);

    public sealed override void Render(DrawingContext context)
    {
        var background = Brush.Parse("White");
        context.FillRectangle(background, new Rect(Bounds.Size));

        var blackBrush = Brush.Parse("Black");
        var orangeBrush = Brush.Parse("Orange");
        var blackPen = new Pen(blackBrush, 2);

        if (Simulator == null)
            return;

        foreach (var objective in Simulator.KillZone.Objectives)
        {
            context.DrawEllipse(orangeBrush, blackPen,
                new Rect(objective.Position.X - KillZone.CircleDistance / 2, objective.Position.Y - KillZone.CircleDistance / 2, KillZone.CircleDistance, KillZone.CircleDistance));
        }



#if false
        if (Shapes != null)
        {
            foreach (var shape in Shapes)
            {
                switch (shape)
                {
                    case Circle circle:
                        break;
                    case Rectangle rectangle:
                        var brush = Brush.Parse(rectangle.FillColor);
                        var pen = new Pen(Brush.Parse(rectangle.StrokeColor), 2);
                        context.DrawEllipse(brush, pen, new Rect(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height));
                        break;
                    case Line line:
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
#endif

        base.Render(context);
    }
}
