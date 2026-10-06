using WorkspaceSwitcher.Core.Models;
using Xunit;

namespace WorkspaceSwitcher.Tests;

/// <summary>
/// Workspace taskbar pinning is opt-in: a freshly created configuration is disabled for
/// its workspace until the user explicitly enables it on the taskbar tab.
/// </summary>
public class TaskbarConfigurationTests
{
    [Fact]
    public void NewConfiguration_IsDisabledForThisWorkspaceByDefault()
    {
        Assert.False(new TaskbarConfiguration().Enabled);
    }
}
