using System.Net.Mail;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Mail;
using Ofertio.Business.Service.Master;
using Ofertio.Infrastructure.Services.Email;

namespace Ofertio.Tests.Unit.Security;

public class TransactionalEmailSenderTests
{
    private static readonly MasterActor Master = new(1, "master@ofertio.com.br", "MasterAdmin", null);

    private static TransactionalEmail Sample(string html = "<p>Olá, ação</p>") => new()
    {
        To = "ana@exemplo.com",
        Subject = "Redefina sua senha do Ofertio",
        TextBody = "Olá, ação",
        HtmlBody = html
    };

    [Fact]
    public void Defaults_SendFromAndReplyToContact()
    {
        var options = new EmailOptions();

        Assert.Equal(OfertioMailAddresses.Contact, options.FromAddress);
        Assert.Equal(OfertioMailAddresses.Contact, options.ReplyToAddress);
        Assert.False(TransactionalEmailSender.IsConfigured(options));
    }

    [Fact]
    public void BuildMessage_KeepsTextBodyAndAddsHtmlAlternative()
    {
        using var mail = TransactionalEmailSender.BuildMessage(new EmailOptions(), Sample());

        Assert.Equal("contato@ofertio.com.br", mail.From!.Address);
        Assert.Equal("Ofertio", mail.From.DisplayName);
        Assert.Equal("contato@ofertio.com.br", Assert.Single(mail.ReplyToList).Address);
        Assert.Equal("ana@exemplo.com", Assert.Single(mail.To).Address);
        Assert.False(mail.IsBodyHtml);
        Assert.Equal("Olá, ação", mail.Body);
        var html = Assert.Single(mail.AlternateViews);
        Assert.Equal("text/html", html.ContentType.MediaType);
    }

    [Fact]
    public void BuildMessage_WithGmailAccount_SendsFromTheAccountAndRepliesToContact()
    {
        var options = new EmailOptions
        {
            FromAddress = "contato@ofertio.com.br",
            Smtp = new EmailSmtpOptions { Host = "smtp.gmail.com", User = " ofertiotecso@gmail.com " }
        };

        using var mail = TransactionalEmailSender.BuildMessage(options, Sample());

        Assert.Equal("ofertiotecso@gmail.com", mail.From!.Address);
        Assert.Equal("Ofertio", mail.From.DisplayName);
        Assert.Equal("contato@ofertio.com.br", Assert.Single(mail.ReplyToList).Address);
    }

    [Fact]
    public void BuildMessage_WithDomainProvider_UsesTheConfiguredSender()
    {
        var options = new EmailOptions
        {
            FromAddress = "contato@ofertio.com.br",
            Smtp = new EmailSmtpOptions { Host = "smtp-relay.brevo.com", User = "abc123@smtp-brevo.com" }
        };

        using var mail = TransactionalEmailSender.BuildMessage(options, Sample());

        Assert.Equal("contato@ofertio.com.br", mail.From!.Address);
    }

    [Fact]
    public void BuildMessage_WithoutHtml_SendsTextOnly()
    {
        using var mail = TransactionalEmailSender.BuildMessage(new EmailOptions(), Sample(html: ""));

        Assert.Empty(mail.AlternateViews);
    }

    [Theory]
    [InlineData("abcd efgh ijkl mnop", "abcdefghijklmnop")]
    [InlineData(" abcdefghijklmnop ", "abcdefghijklmnop")]
    [InlineData(null, "")]
    public void NormalizePassword_RemovesTheGmailBlockSpaces(string? input, string expected)
    {
        Assert.Equal(expected, TransactionalEmailSender.NormalizePassword(input));
    }

    [Fact]
    public async Task SendAsync_WithoutSmtpHost_OnlyLogsAndDoesNotThrow()
    {
        var sender = new TransactionalEmailSender(
            new TestOptionsMonitor(new EmailOptions()),
            NullLogger<TransactionalEmailSender>.Instance);

        await sender.SendAsync(Sample());
    }

    [Fact]
    public void Catalog_ExposesTheSmtpKeysWithThePasswordAsSecret()
    {
        var password = SystemSettingCatalog.Find("Email:Smtp:Password");
        Assert.NotNull(password);
        Assert.True(password!.IsSecret);
        Assert.Equal(SystemSettingCatalog.CategoryEmail, password.Category);
        Assert.False(SystemSettingCatalog.Find("Email:Smtp:Host")!.IsSecret);
        Assert.False(SystemSettingCatalog.Find("Email:Smtp:User")!.IsSecret);
        Assert.NotNull(SystemSettingCatalog.Find("Email:FromAddress"));
    }

    [Fact]
    public async Task SendTest_WithoutHost_SaysItIsNotConfiguredAndSendsNothing()
    {
        var sender = new Moq.Mock<ITransactionalEmailSender>();
        var service = new EmailTestService(
            sender.Object,
            new TestOptionsMonitor(new EmailOptions()),
            new Moq.Mock<IMasterAuditService>().Object,
            NullLogger<EmailTestService>.Instance);

        var result = await service.SendTestAsync(Master);

        Assert.False(result.Success);
        Assert.Contains("não configurado", result.Message);
        sender.Verify(item => item.SendAsync(It.IsAny<TransactionalEmail>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendTest_WithHost_SendsToTheLoggedMaster()
    {
        TransactionalEmail? sent = null;
        var sender = new Moq.Mock<ITransactionalEmailSender>();
        sender.Setup(item => item.SendAsync(It.IsAny<TransactionalEmail>(), It.IsAny<CancellationToken>()))
            .Callback<TransactionalEmail, CancellationToken>((message, _) => sent = message)
            .Returns(Task.CompletedTask);
        var options = new EmailOptions { Smtp = new EmailSmtpOptions { Host = "smtp.gmail.com" } };
        var service = new EmailTestService(
            sender.Object,
            new TestOptionsMonitor(options),
            new Moq.Mock<IMasterAuditService>().Object,
            NullLogger<EmailTestService>.Instance);

        var result = await service.SendTestAsync(Master);

        Assert.True(result.Success);
        Assert.Equal("master@ofertio.com.br", sent!.To);
    }

    [Fact]
    public void DescribeFailure_GmailLoginRejected_AsksForTheAppPassword()
    {
        var ex = new SmtpException("5.7.8 Username and Password not accepted.");

        Assert.Contains("senha de app", EmailTestService.DescribeFailure(ex));
    }

    [Fact]
    public void DescribeFailure_NetworkUnreachable_ShowsTheRealCause()
    {
        var ex = new SmtpException("Failure sending mail.", new System.Net.Sockets.SocketException(101));

        var message = EmailTestService.DescribeFailure(ex);

        Assert.Contains("não alcançou o servidor SMTP", message);
        Assert.Contains("→", message);
    }

    [Fact]
    public void Catalog_EveryKeyHasAGroupAndField_AndMarketplacesHaveOneBoxPerCompany()
    {
        Assert.All(SystemSettingCatalog.All, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.Group), item.Key);
            Assert.False(string.IsNullOrWhiteSpace(item.Field), item.Key);
        });

        var companies = SystemSettingCatalog.All
            .Where(item => item.Category == SystemSettingCatalog.CategoryMarketplaces)
            .Select(item => item.Group)
            .Distinct()
            .ToList();
        Assert.Equal(["Mercado Livre", "Shopee", "Amazon", "TikTok Shop"], companies);
        Assert.Equal("Amazon", SystemSettingCatalog.Find("Integrations:AmazonPaApi:PartnerTag")!.Group);
        Assert.Equal("Mercado Livre — App ID", SystemSettingCatalog.Find("Integrations:MercadoLivre:AppId")!.Label);
    }

    [Fact]
    public void RequiresRestart_OnlyEmailAppliesWithoutRestart()
    {
        Assert.False(SystemSettingCatalog.RequiresRestart(SystemSettingCatalog.CategoryEmail));
        Assert.True(SystemSettingCatalog.RequiresRestart(SystemSettingCatalog.CategoryMarketplaces));
        Assert.True(SystemSettingCatalog.RequiresRestart(SystemSettingCatalog.CategoryBilling));
    }

    [Theory]
    [InlineData("smtp.gmail.com", "smtp.gmail.com")]
    [InlineData("  SMTP.Gmail.com ", "smtp.gmail.com")]
    [InlineData("smtp://smtp.gmail.com:587", "smtp.gmail.com")]
    [InlineData("https://smtp.gmail.com/", "smtp.gmail.com")]
    [InlineData(null, "")]
    public void NormalizeSmtpHost_CleansWhatUsuallyGetsPasted(string? input, string expected)
    {
        Assert.Equal(expected, SystemSettingCatalog.NormalizeSmtpHost(input));
    }

    [Theory]
    [InlineData("ofertiotecso@gmail.com", "ofertiotecso@gmail.com", "é um e-mail")]
    [InlineData("localhost", "ofertiotecso@gmail.com", "próprio container")]
    [InlineData("127.0.0.1", null, "próprio container")]
    [InlineData("smtp.google.com", "ofertiotecso@gmail.com", "Google Workspace")]
    [InlineData("smtp gmail.com", null, "não é um servidor válido")]
    public void ValidateSmtpHost_RejectsTheMistakesThatBreakSending(string host, string? user, string expected)
    {
        var error = SystemSettingCatalog.ValidateSmtpHost(host, user);

        Assert.NotNull(error);
        Assert.Contains(expected, error);
    }

    [Theory]
    [InlineData("smtp.gmail.com", "ofertiotecso@gmail.com")]
    [InlineData("", "ofertiotecso@gmail.com")]
    [InlineData("smtp-relay.brevo.com", "conta@ofertio.com.br")]
    public void ValidateSmtpHost_AcceptsValidCombinations(string host, string user)
    {
        Assert.Null(SystemSettingCatalog.ValidateSmtpHost(host, user));
    }

    [Fact]
    public void DescribeFailure_ConnectionRefused_ShowsTheDestination()
    {
        var ex = new SmtpException("Failure sending mail.", new System.Net.Sockets.SocketException(111));
        var destination = EmailTestService.Destination(new EmailOptions { Smtp = new EmailSmtpOptions { Host = "smtp.google.com", Port = 587 } });

        var message = EmailTestService.DescribeFailure(ex, destination);

        Assert.Contains("smtp.google.com:587", message);
        Assert.Contains("smtp.gmail.com", message);
    }

    private sealed class TestOptionsMonitor : IOptionsMonitor<EmailOptions>
    {
        public TestOptionsMonitor(EmailOptions value) => CurrentValue = value;

        public EmailOptions CurrentValue { get; }

        public EmailOptions Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<EmailOptions, string?> listener) => null;
    }
}
