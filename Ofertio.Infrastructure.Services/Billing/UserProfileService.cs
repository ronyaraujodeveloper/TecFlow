using Microsoft.EntityFrameworkCore;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Billing;
using Ofertio.Core.Entities;
using Ofertio.Database;

namespace Ofertio.Infrastructure.Services.Billing;

public sealed class UserProfileService : IUserProfileService
{
    private readonly AppDbContext _context;

    public UserProfileService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfileResponseDto> GetAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await FindUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return Fail("Usuário não encontrado.");
        }

        var profile = await ProfilesWithCity().AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        var dto = ToDto(user, profile);
        return new UserProfileResponseDto
        {
            Status = true,
            Descricao = "OK",
            Data = dto,
            MissingFields = UserProfileRules.MissingFields(dto)
        };
    }

    /// <summary>Monta o cadastro do usuário; sem perfil salvo, sugere nome e celular da conta.</summary>
    public async Task<UserProfileDto?> LoadAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await FindUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var profile = await ProfilesWithCity().AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        return ToDto(user, profile);
    }

    public async Task<UserProfileResponseDto> SaveAsync(int userId, UserProfileDto request, CancellationToken cancellationToken = default)
    {
        var user = await _context.UserAccounts.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return Fail("Usuário não encontrado.");
        }

        var normalized = UserProfileRules.Normalize(request);
        var errors = UserProfileRules.Validate(normalized);
        City? city = null;
        if (normalized.CityId is int cityId)
        {
            city = await _context.Cities.Include(item => item.State)
                .FirstOrDefaultAsync(item => item.Id == cityId, cancellationToken);
            if (city is null)
            {
                errors.Add("Cidade inválida. Escolha uma cidade da lista.");
            }
            else if (normalized.State is not null && !string.Equals(city.State?.Code.Trim(), normalized.State, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("A cidade escolhida não é da UF selecionada.");
            }
        }

        if (errors.Count > 0)
        {
            return Fail(string.Join(" ", errors));
        }

        var profile = await ProfilesWithCity().FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (profile is null)
        {
            profile = new UserProfile { UserId = userId };
            _context.UserProfiles.Add(profile);
        }

        profile.PersonType = normalized.PersonType;
        profile.FullName = normalized.FullName;
        profile.ResponsibleName = normalized.ResponsibleName;
        profile.Document = normalized.Document;
        profile.Phone = normalized.Phone;
        profile.PostalCode = normalized.PostalCode;
        profile.Street = normalized.Street;
        profile.Number = normalized.Number;
        profile.Complement = normalized.Complement;
        profile.District = normalized.District;
        profile.CityId = city?.Id;
        profile.City = city;
        profile.Touch();

        user.Name = normalized.DisplayName!;
        user.Touch();
        await _context.SaveChangesAsync(cancellationToken);

        var dto = ToDto(user, profile);
        dto.State ??= normalized.State; // UF escolhida sem cidade continua na tela
        var missing = UserProfileRules.MissingFields(dto);
        return new UserProfileResponseDto
        {
            Status = true,
            Descricao = missing.Count == 0
                ? "Dados cadastrais salvos."
                : $"Dados salvos. Para assinar um plano ainda falta: {string.Join(", ", missing)}.",
            Data = dto,
            MissingFields = missing
        };
    }

    private IQueryable<UserProfile> ProfilesWithCity() =>
        _context.UserProfiles.Include(item => item.City).ThenInclude(city => city!.State);

    private Task<UserAccount?> FindUserAsync(int userId, CancellationToken cancellationToken) =>
        _context.UserAccounts.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);

    private static UserProfileDto ToDto(UserAccount user, UserProfile? profile) =>
        new()
        {
            PersonType = profile?.PersonType ?? PersonTypes.Individual,
            DisplayName = user.Name,
            Email = user.Email,
            FullName = profile?.FullName,
            ResponsibleName = profile?.ResponsibleName,
            Document = profile?.Document,
            Phone = profile is null ? user.WhatsAppPhone : profile.Phone,
            PostalCode = profile?.PostalCode,
            Street = profile?.Street,
            Number = profile?.Number,
            Complement = profile?.Complement,
            District = profile?.District,
            CityId = profile?.CityId,
            CityName = profile?.City?.Name,
            State = profile?.City?.State?.Code.Trim()
        };

    private static UserProfileResponseDto Fail(string message) => new() { Status = false, Descricao = message };
}
