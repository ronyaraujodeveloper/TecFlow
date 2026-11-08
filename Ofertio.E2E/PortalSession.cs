using Microsoft.Playwright;

namespace Ofertio.E2E;

/// <summary>Abre a página inicial, entra com e-mail e senha e espera o painel.</summary>
public sealed class PortalSession : IAsyncDisposable
{
    private readonly IPlaywright _playwright;
    private readonly IBrowser _browser;

    private PortalSession(IPlaywright playwright, IBrowser browser, IPage page, E2ESettings settings)
    {
        _playwright = playwright;
        _browser = browser;
        Page = page;
        Settings = settings;
    }

    public IPage Page { get; }

    public E2ESettings Settings { get; }

    public static async Task<PortalSession> OpenAsync(E2ESettings settings)
    {
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = !settings.Headed
        });
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            Locale = "pt-BR",
            TimezoneId = "America/Sao_Paulo",
            IgnoreHTTPSErrors = true
        });
        await context.AddInitScriptAsync(
            """
            (() => {
              const stats = window.__ofertioCircuit = window.__ofertioCircuit || { sends: 0, receives: 0, receivesAtLastSend: 0 };
              const proto = WebSocket.prototype;
              if (proto.__ofertioPatched) return;
              proto.__ofertioPatched = true;
              const originalSend = proto.send;
              proto.send = function () {
                if (!this.__ofertioListening) {
                  this.__ofertioListening = true;
                  this.addEventListener('message', () => { stats.receives++; });
                }
                stats.sends++;
                stats.receivesAtLastSend = stats.receives;
                return originalSend.apply(this, arguments);
              };
            })();
            """);
        var page = await context.NewPageAsync();
        return new PortalSession(playwright, browser, page, settings);
    }

    public async Task LoginAsync()
    {
        await Page.GotoAsync(Settings.LoginUri.AbsoluteUri, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await Page.Locator("#portal-email").WaitForAsync();
        await Page.FillAsync("#portal-email", Settings.Email);
        await Page.FillAsync("#portal-password", Settings.Password);
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Entrar", Exact = true }).ClickAsync();
        await Page.WaitForURLAsync("**/dashboard", new PageWaitForURLOptions
        {
            Timeout = 30_000,
            WaitUntil = WaitUntilState.Commit
        });
    }

    /// <summary>Grava o valor de uma vez e espera o Blazor confirmar, antes do próximo clique.</summary>
    public static async Task FillAndCommitAsync(ILocator field, string value)
    {
        var page = field.Page;
        var sendsBefore = await page.EvaluateAsync<int>("() => (window.__ofertioCircuit && window.__ofertioCircuit.sends) || 0");
        await field.EvaluateAsync(
            """
            (el, value) => {
              const prototype = el instanceof HTMLTextAreaElement
                ? window.HTMLTextAreaElement.prototype
                : window.HTMLInputElement.prototype;
              const descriptor = Object.getOwnPropertyDescriptor(prototype, 'value');
              descriptor.set.call(el, value);
              el.dispatchEvent(new Event('input', { bubbles: true }));
              el.dispatchEvent(new Event('change', { bubbles: true }));
            }
            """,
            value);
        await page.WaitForFunctionAsync(
            """
            before => {
              const circuit = window.__ofertioCircuit;
              return !!circuit
                && circuit.sends > before
                && circuit.receives > circuit.receivesAtLastSend;
            }
            """,
            sendsBefore,
            new PageWaitForFunctionOptions { Timeout = 5_000 });
    }

    /// <summary>Abre a rota e espera o título visível. Não preenche formulário nem grava dado.</summary>
    public async Task ExpectHeadingAsync(string path, string heading)
    {
        var target = new Uri(Settings.BaseUrl, path);
        await Page.GotoAsync(target.AbsoluteUri, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        var title = Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions
        {
            Name = heading,
            Level = 1,
            Exact = true
        });
        await Assertions.Expect(title).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
        {
            Timeout = 30_000
        });
    }

    /// <summary>
    /// Quem não é master é enviado para a entrada. Nesse caso a fumaça segue sem falhar.
    /// </summary>
    public async Task ExpectMasterHeadingWhenAllowedAsync()
    {
        var target = new Uri(Settings.BaseUrl, "/master");
        await Page.GotoAsync(target.AbsoluteUri, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        var title = Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions
        {
            Name = "Dashboard executivo SaaS",
            Level = 1,
            Exact = true
        });
        try
        {
            await title.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 15_000
            });
        }
        catch (TimeoutException)
        {
            if (Page.Url.Contains("/master", StringComparison.OrdinalIgnoreCase))
            {
                throw;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }
}
