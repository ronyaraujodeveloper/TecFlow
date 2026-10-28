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

    public string? City { get; set; }

    public string? State { get; set; }
}

public class UserProfileResponseDto : ResponseDto
{
    public UserProfileDto? Data { get; set; }

    /// <summary>Campos que faltam (ou estão inválidos) para assinar um plano.</summary>
    public List<string> MissingFields { get; set; } = [];
}
