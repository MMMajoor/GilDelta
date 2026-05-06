namespace GilDelta.Windows.Widget;

public interface IWidgetRenderer
{
    WidgetDensity Density { get; }
    void Draw(WidgetContext ctx);
}
