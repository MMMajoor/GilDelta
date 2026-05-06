namespace GilDelta.Windows.Dashboard;

public interface IDashboardTab
{
    DashboardTab Identity { get; }
    string Title { get; }
    void Draw(DashboardContext ctx);
}
