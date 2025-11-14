using System.Numerics;
using ImGuiNET;
using Lilly.Engine.Commands;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Data.Logger;
using Lilly.Engine.Interfaces.Debuggers;
using Lilly.Engine.Rendering.Core.Collections;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Types;
using Serilog.Events;

namespace Lilly.Engine.Debuggers;

/// <summary>
/// Provides an ImGui debugger for displaying and filtering logs with deduplication.
/// Similar to Unity's console window.
/// </summary>
public class LogViewerDebugger : IImGuiDebugger
{
    private readonly LogViewer _logViewer;
    private string _searchBuffer = string.Empty;
    private bool _shouldScrollToBottom;
    private int _selectedLogIndex = -1;

    /// <summary>
    /// Gets or sets the unique identifier for this debugger.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// Gets or sets the name of this debugger.
    /// </summary>
    public string Name { get; set; } = "Console";

    /// <summary>
    /// Gets the rendering order.
    /// </summary>
    public ushort Order { get; } = 1001;

    /// <summary>
    /// Gets or sets whether this debugger is visible.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets the render layer for this debugger.
    /// </summary>
    public RenderLayer Layer { get; set; } = RenderLayer.Debug;

    /// <summary>
    /// Gets or sets the parent game object.
    /// </summary>
    public IGameObject? Parent { get; set; }

    /// <summary>
    /// Gets the collection of child game objects.
    /// </summary>
    public GameObjectCollection<IGameObject> Children { get; } = new();

    /// <summary>
    /// Initializes a new instance of the LogViewerDebugger class.
    /// </summary>
    /// <param name="logViewer">The log viewer instance to display.</param>
    public LogViewerDebugger(LogViewer logViewer)
    {
        _logViewer = logViewer;
        _logViewer.OnLogsChanged += OnLogsChanged;
        _searchBuffer = _logViewer.Settings.SearchFilter;
        logViewer.Initialize();
    }

    /// <summary>
    /// Disposes the debugger and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        _logViewer.OnLogsChanged -= OnLogsChanged;
    }

    /// <summary>
    /// Renders the game object (IGameObject implementation).
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    public IEnumerable<RenderCommand> Render(GameTime gameTime)
    {
        yield return RenderCommandHelpers.ImGuiRender(new(Name, Render));
    }

    /// <summary>
    /// Renders the console window using ImGui.
    /// </summary>
    public void Render()
    {
        if (!IsVisible)
        {
            return;
        }

        RenderToolbar();
        RenderLogList();
        RenderFooter();
    }

    private void OnLogsChanged(object? sender, EventArgs e)
    {
        if (_logViewer.Settings.AutoScroll)
        {
            _shouldScrollToBottom = true;
        }
    }

    private void RenderFooter()
    {
        ImGui.Separator();

        // Statistics bar
        ImGui.Text($"Total: {_logViewer.TotalLogCount} | Unique: {_logViewer.UniqueLogCount} | ");
        ImGui.SameLine();

        ImGui.TextColored(new(1f, 1f, 1f, 1f), $"Info: {_logViewer.InfoCount}");
        ImGui.SameLine();

        ImGui.TextColored(new(1f, 1f, 0f, 1f), $"Warnings: {_logViewer.WarningCount}");
        ImGui.SameLine();

        ImGui.TextColored(new(1f, 0.4f, 0.4f, 1f), $"Errors: {_logViewer.ErrorCount}");
    }

    private void RenderLevelToggle(string label, LogEventLevel level, ref bool isEnabled, Vector4 color)
    {
        var stats = _logViewer.GetStatistics();
        var count = stats.GetValueOrDefault(level, 0);

        if (!isEnabled)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.2f, 0.2f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.3f, 0.3f, 1f));
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Button, color * 0.6f);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color * 0.8f);
        }

        if (ImGui.Button($"{label} ({count})"))
        {
            isEnabled = !isEnabled;
        }

        ImGui.PopStyleColor(2);
    }

    private void RenderLogEntry(LogEntry entry, int index)
    {
        var (r, g, b, a) = entry.GetLevelColor();
        var color = new Vector4(r / 255f, g / 255f, b / 255f, a / 255f);

        // Selectable for the log entry
        var isSelected = _selectedLogIndex == index;

        ImGui.PushStyleColor(ImGuiCol.Text, color);

        var displayText = entry.GetDisplayText();

        if (ImGui.Selectable(displayText, isSelected, ImGuiSelectableFlags.AllowDoubleClick))
        {
            _selectedLogIndex = index;

            // Double-click to expand/show details
            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left) && entry.Exception != null)
            {
                // Could open a detail window here
            }
        }

        ImGui.PopStyleColor();

        // Show exception details if selected
        if (isSelected && entry.Exception != null)
        {
            ImGui.Indent();
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.5f, 0.5f, 1f));
            ImGui.TextWrapped($"Exception: {entry.Exception.Message}");

            if (!string.IsNullOrEmpty(entry.Exception.StackTrace))
            {
                ImGui.TextWrapped(entry.Exception.StackTrace);
            }
            ImGui.PopStyleColor();
            ImGui.Unindent();
        }

        // Context menu
        if (ImGui.BeginPopupContextItem($"LogContext{index}"))
        {
            if (ImGui.MenuItem("Copy"))
            {
                ImGui.SetClipboardText(entry.Message);
            }

            if (entry.SourceContext != null && ImGui.MenuItem("Copy Source"))
            {
                ImGui.SetClipboardText(entry.SourceContext);
            }

            if (entry.Exception != null && ImGui.MenuItem("Copy Exception"))
            {
                ImGui.SetClipboardText(entry.Exception.ToString());
            }

            ImGui.EndPopup();
        }
    }

    private void RenderLogList()
    {
        var filteredLogs = _logViewer.GetFilteredLogs();

        ImGui.Text($"Showing {filteredLogs.Count} of {_logViewer.UniqueLogCount} logs");
        ImGui.Separator();

        ImGui.BeginChild("ScrollingRegion", new(0, -30), ImGuiChildFlags.Borders, ImGuiWindowFlags.HorizontalScrollbar);

        for (var i = 0; i < filteredLogs.Count; i++)
        {
            RenderLogEntry(filteredLogs[i], i);
        }

        // Auto-scroll to bottom
        if (_shouldScrollToBottom && _logViewer.Settings.AutoScroll)
        {
            ImGui.SetScrollHereY(1.0f);
            _shouldScrollToBottom = false;
        }

        ImGui.EndChild();
    }

    private void RenderToolbar()
    {
        // Search bar
        ImGui.SetNextItemWidth(250);

        if (ImGui.InputText("##search", ref _searchBuffer, 256))
        {
            _logViewer.Settings.SearchFilter = _searchBuffer;
        }

        ImGui.SameLine();

        // Level filters with count badges
        var showVerbose = _logViewer.Settings.ShowVerbose;
        RenderLevelToggle("Verbose", LogEventLevel.Verbose, ref showVerbose, new(0.5f, 0.5f, 0.5f, 1f));
        _logViewer.Settings.ShowVerbose = showVerbose;
        ImGui.SameLine();

        var showDebug = _logViewer.Settings.ShowDebug;
        RenderLevelToggle("Debug", LogEventLevel.Debug, ref showDebug, new(0.6f, 0.6f, 0.6f, 1f));
        _logViewer.Settings.ShowDebug = showDebug;
        ImGui.SameLine();

        var showInfo = _logViewer.Settings.ShowInformation;
        RenderLevelToggle("Info", LogEventLevel.Information, ref showInfo, new(1f, 1f, 1f, 1f));
        _logViewer.Settings.ShowInformation = showInfo;
        ImGui.SameLine();

        var showWarning = _logViewer.Settings.ShowWarning;
        RenderLevelToggle("Warning", LogEventLevel.Warning, ref showWarning, new(1f, 1f, 0f, 1f));
        _logViewer.Settings.ShowWarning = showWarning;
        ImGui.SameLine();

        var showError = _logViewer.Settings.ShowError;
        RenderLevelToggle("Error", LogEventLevel.Error, ref showError, new(1f, 0.4f, 0.4f, 1f));
        _logViewer.Settings.ShowError = showError;

        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();

        // Clear button
        if (ImGui.Button("Clear"))
        {
            _logViewer.Clear();
            _selectedLogIndex = -1;
        }

        ImGui.SameLine();

        // Options
        var autoScroll = _logViewer.Settings.AutoScroll;

        if (ImGui.Checkbox("Auto Scroll", ref autoScroll))
        {
            _logViewer.Settings.AutoScroll = autoScroll;
        }

        ImGui.SameLine();

        var showCollapsed = _logViewer.Settings.ShowCollapsed;

        if (ImGui.Checkbox("Collapse", ref showCollapsed))
        {
            _logViewer.Settings.ShowCollapsed = showCollapsed;
        }

        ImGui.SameLine();

        // Export button
        if (ImGui.Button("Copy to Clipboard"))
        {
            var text = _logViewer.ExportToText();
            ImGui.SetClipboardText(text);
        }

        ImGui.Separator();
    }
}
