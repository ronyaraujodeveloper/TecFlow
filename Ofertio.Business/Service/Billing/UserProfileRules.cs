using Ofertio.Business.Dto;
using Ofertio.Business.Service.Common;
using Ofertio.Core.Entities;
using Validation = Ofertio.Util.Validation.ValidationHelper;

namespace Ofertio.Business.Service.Billing;

/// <summary>Normalização, validação e pendências dos dados cadastrais exigidos para assinar um plano.</summary>
public static class UserProfileRules
{
    public const int NameMaxLength = 160;
    public const int DisplayNameMaxLength = 120;

    public static bool IsCompany(string? personType) =>
        string.Equals(personType?.Trim(), PersonTypes.Company, StringComparison.OrdinalIgnoreCase);

    /// <summary>Cópia limpa do formulário: textos aparados, documento/CEP/celular só com caracteres úteis, UF maiúscula.</summary>
    public static UserProfileDto Normalize(UserProfileDto? source)
    {
        source ??= new UserProfileDto();
        var company = IsCompany(source.PersonType);
        return new UserProfileDto
        {
            PersonType = company ? PersonTypes.Company : PersonTypes.Individual,
            DisplayName = Clean(source.DisplayName),
            Email = Clean(source.Email),
            FullName = Clean(source.FullName),
            ResponsibleName = company ? Clean(source.ResponsibleName) : null,
            Document = CleanDocument(source.Document),
            Phone = Validation.NormalizeBrazilianCellPhone(source.Phone) ?? OnlyDigits(source.Phone),
            PostalCode = Validation.NormalizeCepDigits(source.PostalCode) ?? OnlyDigits(source.PostalCode),
            Street = Clean(source.Street),
            Number = Clean(source.Number),
            Complement = Clean(source.Complement),
            District = Clean(source.District),
            City = Clean(source.City),
            State = Clean(source.State)?.ToUpperInvariant()
        };
    }

    /// <summary>Erros de formato dos campos preenchidos; campos vazios ficam para <see cref="MissingFields"/>.</summary>
    public static List<string> Validate(UserProfileDto profile)
    {
        var errors = new List<string>();
        var company = IsCompany(profile.PersonType);
        if (string.IsNullOrWhiteSpace(profile.DisplayName))
        {
            errors.Add("Informe o nome de exibição.");
        }
        else if (profile.DisplayName.Length > DisplayNameMaxLength)
        {
            errors.Add($"O nome de exibição deve ter no máximo {DisplayNameMaxLength} caracteres.");
        }

        if (profile.FullName?.Length > NameMaxLength || profile.ResponsibleName?.Length > NameMaxLength)
        {
            errors.Add($"Nomes devem ter no máximo {NameMaxLength} caracteres.");
        }

        if (!string.IsNullOrEmpty(profile.Document) && !IsValidDocument(profile.Document, company))
        {
            errors.Add(company ? "CNPJ inválido." : "CPF inválido.");
        }

        if (!string.IsNullOrEmpty(profile.Phone) && !Validation.IsValidBrazilianCellPhone(profile.Phone))
        {
            errors.Add("Celular inválido. Use DDD + 9 dígitos.");
        }

        if (!string.IsNullOrEmpty(profile.PostalCode) && !Validation.IsValidCep(profile.PostalCode))
        {
            errors.Add("CEP inválido. Use 8 dígitos.");
        }

        if (!string.IsNullOrEmpty(profile.State) && BrazilTimeZoneRules.NormalizeStateCode(profile.State) is null)
        {
            errors.Add("UF inválida. Escolha um estado da lista.");
        }

        if (profile.Street?.Length > 160 || profile.Number?.Length > 16 || profile.Complement?.Length > 80
            || profile.District?.Length > 80 || profile.City?.Length > 80)
        {
            errors.Add("Endereço com campo maior que o permitido.");
        }

        return errors;
    }

    /// <summary>Rótulos dos campos vazios ou inválidos que impedem a assinatura (ordem do formulário).</summary>
    public static List<string> MissingFields(UserProfileDto? profile)
    {
        var missing = new List<string>();
        if (profile is null)
        {
            profile = new UserProfileDto();
        }

        var company = IsCompany(profile.PersonType);
        if (string.IsNullOrWhiteSpace(profile.FullName))
        {
            missing.Add(company ? "Razão social" : "Nome completo");
        }

        if (!IsValidDocument(profile.Document, company))
        {
            missing.Add(company ? "CNPJ" : "CPF");
        }

        if (company && string.IsNullOrWhiteSpace(profile.ResponsibleName))
        {
            missing.Add("Nome do responsável");
        }

        if (!Validation.IsValidBrazilianCellPhone(profile.Phone))
        {
            missing.Add("Celular");
        }

        if (!Validation.IsValidCep(profile.PostalCode))
        {
            missing.Add("CEP");
        }

        AddIfEmpty(missing, profile.Street, "Rua");
        AddIfEmpty(missing, profile.Number, "Número");
        AddIfEmpty(missing, profile.District, "Bairro");
        AddIfEmpty(missing, profile.City, "Cidade");
        if (BrazilTimeZoneRules.NormalizeStateCode(profile.State) is null)
        {
            missing.Add("UF");
        }

        return missing;
    }

    public static bool IsValidDocument(string? document, bool company) =>
        company ? Validation.IsValidCnpj(document) : Validation.IsValidCpf(document);

    /// <summary>CPF 000.000.000-00 ou CNPJ 00.000.000/0000-00; outros valores voltam como vieram.</summary>
    public static string? FormatDocument(string? document)
    {
        var clean = CleanDocument(document);
        return clean?.Length switch
        {
            11 => $"{clean[..3]}.{clean[3..6]}.{clean[6..9]}-{clean[9..]}",
            14 => $"{clean[..2]}.{clean[2..5]}.{clean[5..8]}/{clean[8..12]}-{clean[12..]}",
            _ => document
        };
    }

    private static void AddIfEmpty(List<string> missing, string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            missing.Add(label);
        }
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? OnlyDigits(string? value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length == 0 ? null : digits;
    }

    private static string? CleanDocument(string? value)
    {
        var clean = new string((value ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
        return clean.Length == 0 ? null : clean;
    }
}
