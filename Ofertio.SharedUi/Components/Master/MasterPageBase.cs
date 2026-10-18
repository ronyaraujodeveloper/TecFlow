using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Ofertio.Business.Dto;
using Ofertio.SharedUi.Models;
using Ofertio.SharedUi.Services.Auth;

namespace Ofertio.SharedUi.Components.Master;

/// <summary>Base das páginas do Painel Master: usuário atual, permissões e mensagens.</summary>
public abstract class MasterPageBase : ComponentBase
{
    protected static readonly CultureInfo PtBr = new("pt-BR");

    [CascadingParameter]
    protected Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    protected ClaimsPrincipal CurrentUser { get; private set; } = new();

    protected string? Error { get; set; }

    protected string? Info { get; set; }

    protected bool Busy { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (AuthenticationStateTask is not null)
        {
            CurrentUser = (await AuthenticationStateTask).User;
        }

        await LoadAsync();
    }

    protected virtual Task LoadAsync() => Task.CompletedTask;

    protected bool Can(string permission) => MasterAccess.Has(CurrentUser, permission);

    protected void SetResult(bool success, string? message)
    {
        Error = success ? null : message;
        Info = success ? message : null;
    }

    protected bool Accept<T>(ApiResult<T> result)
    {
        if (result.Success)
        {
            return true;
        }

        Error = string.IsNullOrWhiteSpace(result.ErrorMessage) ? "Não foi possível concluir a operação." : result.ErrorMessage;
        Info = null;
        return false;
    }

    protected bool AcceptOperation(ApiResult<MasterOperationResultDto> result)
    {
        if (!Accept(result))
        {
            return false;
        }

        SetResult(result.Data?.Success ?? true, result.Data?.Message);
        return result.Data?.Success ?? true;
    }

    protected static string Money(decimal value) => value.ToString("C", PtBr);

    protected static string Bytes(long bytes) => bytes switch
    {
        >= 1L << 30 => $"{bytes / (double)(1L << 30):N2} GB",
        >= 1L << 20 => $"{bytes / (double)(1L << 20):N1} MB",
        >= 1L << 10 => $"{bytes / (double)(1L << 10):N0} KB",
        _ => $"{bytes} B"
    };

    protected static string Date(DateTime? value) =>
        value.HasValue ? value.Value.ToLocalTime().ToString("dd/MM/yyyy", PtBr) : "—";

    protected static string DateTimeText(DateTime? value) =>
        value.HasValue ? value.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm", PtBr) : "—";
}
