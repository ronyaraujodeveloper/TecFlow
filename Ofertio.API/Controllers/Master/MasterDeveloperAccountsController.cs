using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Master;

namespace Ofertio.API.Controllers.Master;

/// <summary>Contas corporativas de teste/dev da Ofertio (escopo de sistema; chaves globais ficam no Vault).</summary>
[Route("api/master/marketplace-accounts")]
[Authorize(Policy = MasterPolicies.ManageGlobalKeys)]
public sealed class MasterDeveloperAccountsController : MasterControllerBase
{
    private readonly IDeveloperAccountService _accounts;

    public MasterDeveloperAccountsController(IDeveloperAccountService accounts)
    {
        _accounts = accounts;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DeveloperAccountDto>>> ListAsync(CancellationToken cancellationToken) =>
        Ok(await _accounts.ListAsync(cancellationToken));

    [HttpPut]
    public async Task<ActionResult<DeveloperAccountDto>> SaveAsync([FromBody] DeveloperAccountDto request, CancellationToken cancellationToken)
    {
        var (item, error) = await _accounts.SaveAsync(Actor, request ?? new DeveloperAccountDto(), cancellationToken);
        return item is null ? Error(error ?? "Não foi possível salvar a conta.") : Ok(item);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<MasterOperationResultDto>> DeleteAsync(int id, CancellationToken cancellationToken) =>
        Result(await _accounts.DeleteAsync(Actor, id, cancellationToken));

    [HttpPost("{id:int}/testar")]
    public async Task<ActionResult<DeveloperAccountCheckDto>> CheckAsync(int id, CancellationToken cancellationToken) =>
        Ok(await _accounts.CheckAsync(id, cancellationToken));
}
