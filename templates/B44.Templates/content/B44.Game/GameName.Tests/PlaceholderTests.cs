using Xunit;

namespace GameName.Tests;

public class PlaceholderTests
{
    [Fact]
    public void Core_IsReachableFromTests()
    {
        // Delete with Placeholder. Its only job is to prove the wiring —
        // engine-free Core, test project, and the Godot guard — before any
        // game code exists.
        Assert.Equal("GameName", Placeholder.Name);
    }
}
