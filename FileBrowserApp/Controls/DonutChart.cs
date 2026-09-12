using System.Windows;
using System.Windows.Media;
using FileBrowserApp.Models;

namespace FileBrowserApp.Controls;

/// <summary>
/// Draws a donut chart from category totals. Pure rendering: it knows nothing about
/// the filesystem, only how to turn a set of (category, bytes) pairs into an annular chart.
/// </summary>
public sealed class DonutChart : FrameworkElement
{
    public static readonly DependencyProperty SlicesProperty = DependencyProperty.Register(
        nameof(Slices), typeof(IReadOnlyList<CategoryTotal>), typeof(DonutChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public IReadOnlyList<CategoryTotal>? Slices
    {
        get => (IReadOnlyList<CategoryTotal>?)GetValue(SlicesProperty);
        set => SetValue(SlicesProperty, value);
    }

    private static readonly Pen GapPen = CreateGapPen();
    private static readonly SolidColorBrush EmptyStrokeBrush = CreateEmptyStrokeBrush();

    protected override void OnRender(DrawingContext dc)
    {
        double size = Math.Min(ActualWidth, ActualHeight);
        if (size <= 0)
            return;

        double cx = ActualWidth / 2;
        double cy = ActualHeight / 2;
        double outerRadius = size / 2 * 0.92;
        double innerRadius = outerRadius * 0.55;

        var slices = Slices?.Where(s => s.TotalBytes > 0).ToList() ?? new List<CategoryTotal>();
        long total = slices.Sum(s => s.TotalBytes);

        if (total <= 0)
        {
            var ringPen = new Pen(EmptyStrokeBrush, outerRadius - innerRadius);
            dc.DrawEllipse(null, ringPen, new Point(cx, cy), (outerRadius + innerRadius) / 2, (outerRadius + innerRadius) / 2);
            return;
        }

        double startAngle = -90; // 12 o'clock
        foreach (var slice in slices)
        {
            double fraction = (double)slice.TotalBytes / total;
            double sweep = Math.Min(fraction * 360.0, 359.999);
            if (sweep <= 0)
                continue;

            var geometry = BuildAnnularSector(cx, cy, innerRadius, outerRadius, startAngle, sweep);
            dc.DrawGeometry(CategoryPalette.GetGradientBrush(slice.Category), GapPen, geometry);

            startAngle += sweep;
        }
    }

    private static StreamGeometry BuildAnnularSector(
        double cx, double cy, double innerRadius, double outerRadius, double startDeg, double sweepDeg)
    {
        double startRad = startDeg * Math.PI / 180.0;
        double endRad = (startDeg + sweepDeg) * Math.PI / 180.0;

        Point OuterAt(double angle) => new(cx + outerRadius * Math.Cos(angle), cy + outerRadius * Math.Sin(angle));
        Point InnerAt(double angle) => new(cx + innerRadius * Math.Cos(angle), cy + innerRadius * Math.Sin(angle));

        var outerStart = OuterAt(startRad);
        var outerEnd = OuterAt(endRad);
        var innerEnd = InnerAt(endRad);
        var innerStart = InnerAt(startRad);

        bool isLargeArc = sweepDeg > 180.0;

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(outerStart, isFilled: true, isClosed: true);
            ctx.ArcTo(outerEnd, new Size(outerRadius, outerRadius), 0, isLargeArc,
                SweepDirection.Clockwise, isStroked: true, isSmoothJoin: false);
            ctx.LineTo(innerEnd, isStroked: true, isSmoothJoin: false);
            ctx.ArcTo(innerStart, new Size(innerRadius, innerRadius), 0, isLargeArc,
                SweepDirection.Counterclockwise, isStroked: true, isSmoothJoin: false);
        }
        geometry.Freeze();
        return geometry;
    }

    private static Pen CreateGapPen()
    {
        // Matches the app's dark surface color so slices read as cleanly separated.
        var brush = new SolidColorBrush(Color.FromRgb(0x15, 0x1A, 0x2E));
        brush.Freeze();
        var pen = new Pen(brush, 2);
        pen.Freeze();
        return pen;
    }

    private static SolidColorBrush CreateEmptyStrokeBrush()
    {
        var brush = new SolidColorBrush(Color.FromRgb(0x2A, 0x32, 0x52));
        brush.Freeze();
        return brush;
    }
}
