namespace TypingRealm.Typing.Tests;

public class InitialTest
{
    [Fact]
    public void Test()
    {
        var info = new TypingSessionInfo(
            "id", "text", DateTime.UtcNow, 10);

        Assert.Equal(10, info.LengthSeconds);
    }
}
