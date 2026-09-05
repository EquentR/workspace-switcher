using WorkspaceSwitcher.Core.Models;
using WorkspaceSwitcher.Core.Native;
using Xunit;

namespace WorkspaceSwitcher.Tests;

public class WindowPlacementTests
{
    [Fact]
    public void WindowRect_WidthAndHeight_CalculateCorrectly()
    {
        var rect = new WindowRect(100, 200, 900, 800);
        Assert.Equal(800, rect.Width);
        Assert.Equal(600, rect.Height);
    }

    [Fact]
    public void NativeRect_RoundtripConversion_PreservesCoordinates()
    {
        var original = new WindowRect(50, 100, 1920, 1080);
        var native = original.ToNative();
        var converted = WindowRect.FromNative(native);

        Assert.Equal(original.Left, converted.Left);
        Assert.Equal(original.Top, converted.Top);
        Assert.Equal(original.Right, converted.Right);
        Assert.Equal(original.Bottom, converted.Bottom);
    }

    [Fact]
    public void NativeWindowPlacement_Roundtrip_PreservesStateAndPositions()
    {
        var wp = new WindowPlacementInfo
        {
            Flags = 2,
            State = WindowState.Maximized,
            MinPosition = new WindowPoint(-1, -1),
            MaxPosition = new WindowPoint(-1, -1),
            NormalPosition = new WindowRect(200, 150, 1200, 850)
        };

        var native = wp.ToNative();
        Assert.Equal(NativeMethods.SW_SHOWMAXIMIZED, native.showCmd);

        var restored = WindowPlacementInfo.FromNative(native);
        Assert.Equal(WindowState.Maximized, restored.State);
        Assert.Equal(wp.NormalPosition.Left, restored.NormalPosition.Left);
        Assert.Equal(wp.NormalPosition.Top, restored.NormalPosition.Top);
        Assert.Equal(wp.NormalPosition.Width, restored.NormalPosition.Width);
        Assert.Equal(wp.NormalPosition.Height, restored.NormalPosition.Height);
    }
}
