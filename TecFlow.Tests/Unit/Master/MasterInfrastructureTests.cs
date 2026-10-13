using System.Security.Claims;
using System.Text;
using TecFlow.Business.Service.Master;
using TecFlow.Core.Security;
using TecFlow.Infrastructure.Services.Master;
using TecFlow.SharedUi.Services.Auth;

namespace TecFlow.Tests.Unit.Master;

public class ApiRequestCounterTests
{
    [Fact]
    public void CountLast24Hours_ShouldDropRequestsOlderThanWindow()
    {
        var now = new DateTime(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc);
        var counter = new ApiRequestCounter(() => now);

        counter.Increment();
        now = now.AddHours(5);
        counter.Increment();
        counter.Increment();

        Assert.Equal(3, counter.CountLast24Hours());

        now = now.AddHours(20);
        Assert.Equal(2, counter.CountLast24Hours());
    }
}

public class JwtPayloadReaderTests
{
    private static string Token(string payloadJson)
    {
        static string Encode(string value) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        return $"{Encode("{\"alg\":\"none\"}")}.{Encode(payloadJson)}.sig";
    }

    [Fact]
    public void ReadRole_ShouldAcceptLongAndShortClaimNames()
    {
        Assert.Equal("SuperAdmin", JwtPayloadReader.ReadRole(Token($"{{\"{JwtPayloadReader.RoleClaim}\":\"SuperAdmin\"}}")));
        Assert.Equal("FinanceiroSaaS", JwtPayloadReader.ReadRole(Token("{\"role\":[\"FinanceiroSaaS\"]}")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("nao-e-jwt")]
    [InlineData("a.%%%.c")]
    public void Read_ShouldReturnEmptyForInvalidTokens(string? token)
    {
        Assert.Empty(JwtPayloadReader.Read(token));
    }

    [Fact]
    public void Read_ShouldKeepNumericClaimsAsText()
    {
        var payload = JwtPayloadReader.Read(Token("{\"exp\":1760000000,\"email\":\"a@b.com\"}"));

        Assert.Equal("1760000000", payload["exp"]);
        Assert.Equal("a@b.com", JwtPayloadReader.Get(payload, JwtPayloadReader.EmailClaim, "email"));
    }
}

public class MasterAccessTests
{
    private static ClaimsPrincipal User(params Claim[] claims) => new(new ClaimsIdentity(claims, "test"));

    [Fact]
    public void IsMasterView_ShouldFollowRoleAndViewMode()
    {
        var master = User(new Claim(ClaimTypes.Role, MasterRoles.SuperAdmin));
        var masterAsClient = User(
            new Claim(ClaimTypes.Role, MasterRoles.SuperAdmin),
            new Claim(TecFlowClaimTypes.ViewMode, MasterAccess.ViewClient));
        var tenant = User(new Claim(ClaimTypes.Role, MasterRoles.TenantUser));

        Assert.True(MasterAccess.IsMasterView(master));
        Assert.False(MasterAccess.IsMasterView(masterAsClient));
        Assert.False(MasterAccess.IsMasterView(tenant));
        Assert.False(MasterAccess.IsMaster(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    [Fact]
    public void Has_ShouldUsePermissionMatrix()
    {
        var finance = User(new Claim(ClaimTypes.Role, MasterRoles.FinanceiroSaaS));

        Assert.True(MasterAccess.Has(finance, MasterPermissions.AdjustCredits));
        Assert.False(MasterAccess.Has(finance, MasterPermissions.Impersonate));
    }

    [Fact]
    public void IsImpersonating_ShouldDetectStoredMasterToken()
    {
        var impersonated = User(
            new Claim(ClaimTypes.Role, MasterRoles.TenantUser),
            new Claim(TecFlowClaimTypes.ImpersonatorAccessToken, "master-token"),
            new Claim(TecFlowClaimTypes.ImpersonatedTenantName, "Loja X"));

        Assert.True(MasterAccess.IsImpersonating(impersonated));
        Assert.Equal("Loja X", MasterAccess.ImpersonatedTenant(impersonated));
    }
}
