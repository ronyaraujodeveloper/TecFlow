using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Ofertio.Infrastructure.Security;

namespace Ofertio.Tests.Unit.Security;

public class DataProtectionKeysPathTests
{
    private static IConfiguration Build(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    [Fact]
    public void ResolveKeysPath_ShouldCombineRelativePathWithContentRoot()
    {
        var configuration = Build(
            (DataProtectionRegistrationExtensions.KeysPathSetting, "App_Data/OfertioDataProtectionKeys"),
            (HostDefaults.ContentRootKey, @"C:\inetpub\ofertio\api"));

        var path = DataProtectionRegistrationExtensions.ResolveKeysPath(configuration);

        Assert.Equal(@"C:\inetpub\ofertio\api\App_Data\OfertioDataProtectionKeys", path);
    }

    [Fact]
    public void ResolveKeysPath_ShouldKeepAbsolutePath()
    {
        var configuration = Build(
            (DataProtectionRegistrationExtensions.KeysPathSetting, @"C:\inetpub\ofertio\api\App_Data\OfertioDataProtectionKeys"),
            (HostDefaults.ContentRootKey, @"C:\Programacao\Ofertio.API"));

        var path = DataProtectionRegistrationExtensions.ResolveKeysPath(configuration);

        Assert.Equal(@"C:\inetpub\ofertio\api\App_Data\OfertioDataProtectionKeys", path);
    }

    [Fact]
    public void ResolveKeysPath_ShouldFallBackToProgramDataOfertio()
    {
        var path = DataProtectionRegistrationExtensions.ResolveKeysPath(Build());

        Assert.EndsWith(Path.Combine("Ofertio", "DataProtectionKeys"), path);
    }

    [Fact]
    public void ApplicationName_ShouldStayLegacyToKeepProtectedTokensReadable()
    {
        Assert.Equal("TecFlow", DataProtectionRegistrationExtensions.ApplicationName);
        Assert.Equal("TecFlow.MarketplaceTokens.v1", SecurityEncryptionService.Purpose);
    }
}
