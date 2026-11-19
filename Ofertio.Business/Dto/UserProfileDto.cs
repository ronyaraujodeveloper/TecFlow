using Ofertio.Core.Entities;

namespace Ofertio.Business.Dto;

/// <summary>Dados cadastrais do formulário Meus dados (pessoa física ou empresa).</summary>
public class UserProfileDto
{
    /// <summary>PF ou PJ.</summary>
    public string PersonType { get; set; } = PersonTypes.Individual;

    /// <summary>Nome de exibição na plataforma (coluna Nome do usuário).</summary>
    public string? DisplayName { get; set; }

    /// <summary>Somente leitura: e-mail de acesso.</summary>
    public string? Email { get; set; }

    /// <summary>Nome completo (PF) ou razão social (PJ).</summary>
    public string? FullName { get; set; }

    /// <summary>Responsável pela empresa (PJ).</summary>
    public string? ResponsibleName { get; set; }

    /// <summary>CPF (PF) ou CNPJ (PJ), com ou sem pontuação.</summary>
    public string? Document { get; set; }

    public string? Phone { get; set; }

    public string? PostalCode { get; set; }

    public string? Street { get; set; }

    public string? Number { get; set; }

    public string? Complement { get; set; }

    public string? District { get; set; }

    /// <summary>Cidade da tabela de referência. É o que fica gravado; a UF sai dela.</summary>
    public int? CityId { get; set; }

    /// <summary>Somente leitura: nome da cidade gravada, para exibir.</summary>
    public string? CityName { get; set; }

    /// <summary>UF escolhida no formulário para filtrar as cidades. Não é gravada: ao carregar, vem da cidade.</summary>
    public string? State { get; set; }
}

public class UserProfileResponseDto : ResponseDto
{
    public UserProfileDto? Data { get; set; }

    /// <summary>Campos que faltam (ou estão inválidos) para assinar um plano.</summary>
    public List<string> MissingFields { get; set; } = [];
}
