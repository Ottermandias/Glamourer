using Glamourer.Interop;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;

namespace Glamourer.Gui.Tabs.DebugTab;

public class JobPanel(JobService _jobs) : IDebugTabTree
{
    public string Label
        => "Job Service";

    public bool Disabled
        => false;

    public void Draw()
    {
        DrawJobs();
        DrawJobGroups();
        DrawValidJobGroups();
    }

    private void DrawJobs()
    {
        using var t = ImRaii.TreeNode("Jobs");
        if (!t)
            return;

        using var table = ImRaii.Table("##jobs", 3, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);
        if (!table)
            return;

        foreach (var (id, job) in _jobs.Jobs)
        {
            ImGuiUtil.DrawTableColumn(id.Id.ToString("D3"));
            ImGuiUtil.DrawTableColumn(job.Name);
            ImGuiUtil.DrawTableColumn(job.Abbreviation);
        }
    }

    private void DrawJobGroups()
    {
        using var t = ImRaii.TreeNode("All Job Groups");
        if (!t)
            return;

        using var table = ImRaii.Table("##groups", 3, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);
        if (!table)
            return;

        foreach (var (group, idx) in _jobs.AllJobGroups.WithIndex())
        {
            ImGuiUtil.DrawTableColumn(idx.ToString("D3"));
            ImGuiUtil.DrawTableColumn(group.Name);
            ImGuiUtil.DrawTableColumn(group.Count.ToString());
        }
    }

    private void DrawValidJobGroups()
    {
        using var t = ImRaii.TreeNode("Valid Job Groups");
        if (!t)
            return;

        using var table = ImRaii.Table("##groups", 3, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);
        if (!table)
            return;

        foreach (var (id, group) in _jobs.JobGroups)
        {
            ImGuiUtil.DrawTableColumn(id.Id.ToString("D3"));
            ImGuiUtil.DrawTableColumn(group.Name);
            ImGuiUtil.DrawTableColumn(group.Count.ToString());
        }
    }
}
