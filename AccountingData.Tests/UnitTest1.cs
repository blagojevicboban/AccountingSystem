namespace AccountingData.Tests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var hash = AccountingDbContext.HashPassword("admin");
        Assert.True(AccountingDbContext.VerifyPassword("admin", hash));
    }
}