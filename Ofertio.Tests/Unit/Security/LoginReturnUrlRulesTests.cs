using Ofertio.Business.Service.Common;
using Xunit;

namespace Ofertio.Tests.Unit.Security;

public sealed class LoginReturnUrlRulesTests
{
    [Theory]
    [InlineData("/busca", "/busca")]
    [InlineData("/busca?q=iphone%2015&cat=1", "/busca?q=iphone%2015&cat=1")]
    [InlineData("/integracoes/whatsapp/agendador", "/integracoes/whatsapp/agendador")]
    [InlineData("  /meu-plano  ", "/meu-plano")]
    public void Sanitize_ShouldKeepInternalPaths(string input, string expected) =>
        Assert.Equal(expected, LoginReturnUrlRules.Sanitize(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("busca")]
    [InlineData("//evil.com")]
    [InlineData("/\\evil.com")]
    [InlineData("https://evil.com/x")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/")]
    [InlineData("/entrar")]
    [InlineData("/entrar?returnUrl=%2Fbusca")]
    [InlineData("/auth/logout")]
    [InlineData("/cadastro")]
    [InlineData("/busca\r\nSet-Cookie: x=1")]
    public void Sanitize_ShouldRejectUnsafeOrLoopingTargets(string? input) =>
        Assert.Null(LoginReturnUrlRules.Sanitize(input));

    [Fact]
    public void Sanitize_ShouldRejectVeryLongValues() =>
        Assert.Null(LoginReturnUrlRules.Sanitize("/" + new string('a', 3000)));

    [Fact]
    public void BuildLoginPath_ShouldCarryCurrentScreen()
    {
        Assert.Equal("/entrar?returnUrl=%2Fbusca%3Fq%3Dtv", LoginReturnUrlRules.BuildLoginPath("busca?q=tv"));
        Assert.Equal("/entrar", LoginReturnUrlRules.BuildLoginPath(""));
        Assert.Equal("/entrar", LoginReturnUrlRules.BuildLoginPath("entrar"));
    }

    [Fact]
    public void AppendTo_ShouldUseAmpersandWhenUrlHasQuery() =>
        Assert.Equal(
            "/auth/complete-signin?ticket=abc&returnUrl=%2Fbusca",
            LoginReturnUrlRules.AppendTo("/auth/complete-signin?ticket=abc", "/busca"));

    [Fact]
    public void ResolveAfterLogin_ShouldFallBackToDashboard()
    {
        Assert.Equal("/busca", LoginReturnUrlRules.ResolveAfterLogin("/busca"));
        Assert.Equal("/dashboard", LoginReturnUrlRules.ResolveAfterLogin("https://evil.com"));
        Assert.Equal("/dashboard", LoginReturnUrlRules.ResolveAfterLogin(null));
    }

    [Fact]
    public void QueryKey_ShouldMatchCookieMiddlewareReturnUrlIgnoringCase()
    {
        // O middleware de cookie manda "ReturnUrl"; o login lê "returnUrl".
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery("?ReturnUrl=%2Fbusca%3Fq%3Dtv");

        Assert.True(query.TryGetValue(LoginReturnUrlRules.QueryKey, out var value));
        Assert.Equal("/busca?q=tv", LoginReturnUrlRules.Sanitize(value.ToString()));
    }
}