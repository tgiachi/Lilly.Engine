using Lilly.Engine.Core.Attributes.Scripts;
using Lilly.Engine.Core.Interfaces.Services;
using Lilly.Engine.Interfaces.Services;

namespace Lilly.Engine.Modules;

[ScriptModule("notifications", "Notification utilities and functions")]

/// <summary>
/// Provides scripting access to notification services for displaying messages.
/// </summary>
public class NotificationsModule
{
    private readonly INotificationService _notificationService;
    private readonly IScriptEngineService _scriptEngineService;

    public NotificationsModule(INotificationService notificationService, IScriptEngineService scriptEngineService)
    {
        _notificationService = notificationService;
        _scriptEngineService = scriptEngineService;
    }

    [ScriptFunction("error", "Raises an error notification with the specified message.")]
    public void Error(string message)
    {
        _notificationService.ShowError(message, 5f);
    }

    [ScriptFunction("info", "Raises an informational notification with the specified message.")]
    public void Info(string message)
    {
        _notificationService.ShowInfo(message, 3f);
    }

    [ScriptFunction("success", "Raises a success notification with the specified message.")]
    public void Success(string message)
    {
        _notificationService.ShowSuccess(message, 3f);
    }

    [ScriptFunction("test_all", "Shows all notification types for testing purposes.")]
    public void TestAllNotifications()
    {
        _notificationService.ShowSuccess("Success notification", 3f);
        _notificationService.ShowInfo("Info notification", 3f);
        _notificationService.ShowWarning("Warning notification", 4f);
        _notificationService.ShowError("Error notification", 5f);
    }

    [ScriptFunction("test_script_error", "Triggers a Lua script error to test the error display system.")]
    public void TestScriptError()
    {
        // This will trigger a runtime error - accessing a nil variable
        var errorScript = @"
            -- Intentional error for testing the ScriptErrorDisplayService
                local test_error = nil
                test_error.some_method()  -- This will cause a runtime error
            ";

        try
        {
            _scriptEngineService.ExecuteScript(errorScript);
        }
        catch
        {
            // Expected - the error is caught by ScriptErrorDisplayService
            // through the OnScriptError event
        }
    }

    [ScriptFunction("warning", "Raises a warning notification with the specified message.")]
    public void Warning(string message)
    {
        _notificationService.ShowWarning(message, 4f);
    }
}
