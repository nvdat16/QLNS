using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qlns.Api.Modules.Contracts.Shared;
using Qlns.BusinessLogic.Modules.Contracts.Addenda;

namespace Qlns.Api.Modules.Contracts.Addenda;

/// <summary>CON-03.1: addenda of one contract — list and draft. The original contract row is never touched.</summary>
[Route("api/v1/contracts/{contractId:long:min(1)}/addenda")]
public sealed class ContractAddendaController(ContractAddendumService service) : ContractsControllerBase
{
    [HttpGet(Name = "listContractAddenda")]
    [Authorize(Policy = ContractPolicies.Read)]
    [ProducesResponseType<ContractAddendumResponse[]>(StatusCodes.Status200OK)]
    public Task<IActionResult> List(long contractId, CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            var addenda = await service.ListAsync(contractId, actor, cancellationToken);
            return Ok(addenda.Select(ContractAddendumResponse.From).ToArray());
        });

    [HttpPost(Name = "createContractAddendum")]
    [Authorize(Policy = ContractPolicies.Write)]
    [ProducesResponseType<ContractAddendumResponse>(StatusCodes.Status201Created)]
    public Task<IActionResult> Create(
        long contractId,
        [FromBody] ContractAddendumWriteRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            var created = await service.CreateAsync(
                new CreateContractAddendumCommand(contractId, request.ToWrite(), actor),
                cancellationToken);

            SetETag(created.Version);
            return Created(
                string.Create(CultureInfo.InvariantCulture, $"/api/v1/contract-addenda/{created.Id}"),
                ContractAddendumResponse.From(created));
        });
}
