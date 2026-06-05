using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Core.Entities;
using TecFlow.Database.Filter;
using TecFlow.Database.Pagin;
using TecFlow.Util.Validation;

namespace TecFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/vendas/clientes")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerRepository _customerRepository;

    public CustomersController(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    [HttpGet]
    public async Task<ActionResult<CustomerResponseDto>> GetByFilterAsync([FromQuery] CustomerFilter filter)
    {
        var filtered = (await _customerRepository.ListAsync()).ApplyFilter(filter);
        var (items, meta) = PagedListHelper.Slice(filtered, filter);

        return Ok(new CustomerResponseDto
        {
            Status = true,
            Descricao = "OK",
            DataList = items,
            Paging = PagingInfoDto.FromMeta(meta)
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerResponseDto>> GetByIdAsync(int id)
    {
        var customer = await _customerRepository.GetByIdAsync(id);
        if (customer is null)
        {
            return NotFound(new CustomerResponseDto { Status = false, Descricao = "Cliente não encontrado." });
        }

        return Ok(new CustomerResponseDto { Status = true, Descricao = "OK", Data = customer });
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponseDto>> CreateAsync([FromBody] CustomerDto dto)
    {
        var validation = ValidateCustomerDto(dto);
        if (validation is not null)
        {
            return BadRequest(new CustomerResponseDto { Status = false, Descricao = validation });
        }

        var customer = MapToEntity(dto);
        var created = await _customerRepository.CreateAsync(customer);

        return Ok(new CustomerResponseDto
        {
            Status = true,
            Descricao = "Cliente criado com sucesso.",
            Data = created
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CustomerResponseDto>> UpdateAsync(int id, [FromBody] CustomerDto dto)
    {
        var customer = await _customerRepository.GetByIdAsync(id);
        if (customer is null)
        {
            return NotFound(new CustomerResponseDto { Status = false, Descricao = "Cliente não encontrado." });
        }

        var validation = ValidateCustomerDto(dto);
        if (validation is not null)
        {
            return BadRequest(new CustomerResponseDto { Status = false, Descricao = validation });
        }

        ApplyDto(customer, dto);
        var updated = await _customerRepository.UpdateAsync(customer);

        return Ok(new CustomerResponseDto
        {
            Status = true,
            Descricao = "Cliente atualizado com sucesso.",
            Data = updated
        });
    }

    private static string? ValidateCustomerDto(CustomerDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return "Nome é obrigatório.";
        }

        if (!string.IsNullOrWhiteSpace(dto.Email) && !ValidationHelper.IsValidEmail(dto.Email))
        {
            return "E-mail inválido.";
        }

        if (!string.IsNullOrWhiteSpace(dto.DocumentNumber))
        {
            var doc = dto.DocumentNumber.Trim();
            var digits = new string(doc.Where(char.IsDigit).ToArray());
            if (digits.Length == 11 && !ValidationHelper.IsValidCpf(doc))
            {
                return "CPF inválido.";
            }

            if (digits.Length is 14 && !ValidationHelper.IsValidCnpj(doc))
            {
                return "CNPJ inválido.";
            }
        }

        return null;
    }

    private static Customer MapToEntity(CustomerDto dto)
    {
        var entity = new Customer();
        ApplyDto(entity, dto);
        return entity;
    }

    private static void ApplyDto(Customer entity, CustomerDto dto)
    {
        entity.Name = dto.Name.Trim();
        entity.DocumentNumber = dto.DocumentNumber?.Trim();
        entity.Email = dto.Email?.Trim();
        entity.Phone = dto.Phone?.Trim();
        entity.Street = dto.Street.Trim();
        entity.StreetNumber = dto.StreetNumber.Trim();
        entity.ZipCode = dto.ZipCode.Trim();
        entity.City = dto.City.Trim();
        entity.State = dto.State.Trim().ToUpperInvariant();
    }
}
