namespace UnitTests.TestHelpers;

public class MockTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow = TestTimeConstants.Today;

    public MockTimeProvider()
    {
    }

    public MockTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public void SetUtcNow(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return _utcNow;
    }
}