using Microsoft.EntityFrameworkCore;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Legal;
using Ofertio.Core.Entities;
using Ofertio.Database;
using Ofertio.Database.Filter;
using Ofertio.Database.Pagin;

namespace Ofertio.Infrastructure.Services.Legal;

public sealed class TermsDocumentService : ITermsDocumentService
{
    private readonly AppDbContext _context;

    public TermsDocumentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TermsVersionResponseDto> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var items = await _context.TermsVersions
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.DocumentKind)
            .ToListAsync(cancellationToken);

        return new TermsVersionResponseDto
        {
            Status = true,
            Descricao = "OK",
            DataList = items
        };
    }

    public async Task<TermsVersionResponseDto> ListAsync(TermsVersionFilter filter, CancellationToken cancellationToken = default)
    {
        filter ??= new TermsVersionFilter();
        var query = _context.TermsVersions.AsNoTracking().AsQueryable();
        var kind = TermsDocumentRules.NormalizeKind(filter.DocumentKind);
        if (kind is not null)
        {
            query = query.Where(item => item.DocumentKind == kind);
        }

        if (!string.IsNullOrWhiteSpace(filter.Version))
        {
            var version = filter.Version.Trim();
            query = query.Where(item => item.Version == version);
        }

        if (filter.IsActive is bool active)
        {
            query = query.Where(item => item.IsActive == active);
        }

        var page = PagedListHelper.NormalizePage(filter.Page);
        var pageSize = PagedListHelper.NormalizePageSize(filter.PageSize);
        var items = await query
            .OrderByDescending(item => item.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new TermsVersionResponseDto
        {
            Status = true,
            Descricao = "OK",
            DataList = items
        };
    }

    public async Task<TermsVersionResponseDto> PublishAsync(
        TermsVersionDto request,
        CancellationToken cancellationToken = default)
    {
        var kind = TermsDocumentRules.NormalizeKind(request.DocumentKind);
        if (kind is null)
        {
            return FailVersion("Escolha Termos de Uso ou Política de Privacidade.");
        }

        var contentError = TermsDocumentRules.ValidateContent(request.Content);
        if (contentError is not null)
        {
            return FailVersion(contentError);
        }

        var latest = await _context.TermsVersions
            .Where(item => item.DocumentKind == kind)
            .OrderByDescending(item => item.Id)
            .Select(item => item.Version)
            .FirstOrDefaultAsync(cancellationToken);

        var current = await _context.TermsVersions
            .Where(item => item.DocumentKind == kind && item.IsActive)
            .ToListAsync(cancellationToken);
        foreach (var item in current)
        {
            item.IsActive = false;
            item.Touch();
        }

        var created = new TermsVersion
        {
            DocumentKind = kind,
            Version = TermsDocumentRules.NextVersion(latest),
            Content = TermsDocumentRules.Sanitize(request.Content),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.TermsVersions.Add(created);
        await _context.SaveChangesAsync(cancellationToken);

        return new TermsVersionResponseDto
        {
            Status = true,
            Descricao = $"Versão {created.Version} publicada. Quem ainda não aceitou esta versão verá o pedido de novo aceite.",
            Data = created
        };
    }

    public async Task<UserTermsAcceptanceResponseDto> GetStatusAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await FindUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return FailAcceptance("Usuário não encontrado.");
        }

        var active = await ActiveMapAsync(cancellationToken);
        var requiredTerms = TermsDocumentRules.RequiredVersion(VersionOf(active, TermsDocumentRules.Terms));
        var privacyAccepted = await LatestAcceptedVersionAsync(userId, TermsDocumentRules.Privacy, cancellationToken);
        var hasExternal = await _context.UserTermsAcceptances.AsNoTracking().AnyAsync(
            row => row.UserId == userId
                && row.Purpose == TermsDocumentRules.PurposeExternalApi
                && row.DocumentKind == TermsDocumentRules.Terms
                && row.Version == requiredTerms,
            cancellationToken);

        return new UserTermsAcceptanceResponseDto
        {
            Status = true,
            Descricao = "OK",
            RequiresReacceptance = TermsDocumentRules.IsReacceptanceRequired(user.TermsAcceptedVersion, VersionOf(active, TermsDocumentRules.Terms))
                || TermsDocumentRules.IsReacceptanceRequired(privacyAccepted, VersionOf(active, TermsDocumentRules.Privacy)),
            RequiresExternalApiConsent = !hasExternal,
            ActiveTermsVersion = requiredTerms,
            ActivePrivacyVersion = TermsDocumentRules.RequiredVersion(VersionOf(active, TermsDocumentRules.Privacy))
        };
    }

    public async Task<UserTermsAcceptanceResponseDto> AcceptAsync(
        int userId,
        UserTermsAcceptanceDto request,
        string? clientIp,
        CancellationToken cancellationToken = default)
    {
        if (!request.Accepted)
        {
            return FailAcceptance(TermsDocumentRules.AcceptanceRequiredMessage);
        }

        var user = await FindUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return FailAcceptance("Usuário não encontrado.");
        }

        var recorded = await RecordAsync(
            user,
            TermsDocumentRules.NormalizePurpose(request.Purpose),
            clientIp,
            cancellationToken);

        return new UserTermsAcceptanceResponseDto
        {
            Status = true,
            Descricao = "Aceite registrado.",
            Data = recorded,
            RequiresReacceptance = false,
            RequiresExternalApiConsent = recorded.Purpose != TermsDocumentRules.PurposeExternalApi
                && (await GetStatusAsync(userId, cancellationToken)).RequiresExternalApiConsent
        };
    }

    public async Task<UserTermsAcceptanceResponseDto> ListMineAsync(
        UserTermsAcceptanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        filter ??= new UserTermsAcceptanceFilter();
        if (filter.UserId is not > 0)
        {
            return FailAcceptance("Usuário não encontrado.");
        }

        var userId = filter.UserId.Value;
        var query = _context.UserTermsAcceptances.AsNoTracking().Where(row => row.UserId == userId);
        var kind = TermsDocumentRules.NormalizeKind(filter.DocumentKind);
        if (kind is not null)
        {
            query = query.Where(row => row.DocumentKind == kind);
        }

        if (!string.IsNullOrWhiteSpace(filter.Purpose))
        {
            var purpose = TermsDocumentRules.NormalizePurpose(filter.Purpose);
            query = query.Where(row => row.Purpose == purpose);
        }

        if (!string.IsNullOrWhiteSpace(filter.Version))
        {
            var version = filter.Version.Trim();
            query = query.Where(row => row.Version == version);
        }

        var page = PagedListHelper.NormalizePage(filter.Page);
        var pageSize = PagedListHelper.NormalizePageSize(filter.PageSize);
        var items = await query
            .OrderByDescending(row => row.AcceptedAt)
            .ThenByDescending(row => row.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        foreach (var item in items)
        {
            item.User = null;
        }

        return new UserTermsAcceptanceResponseDto
        {
            Status = true,
            Descricao = "OK",
            DataList = items
        };
    }

    public async Task RecordRegistrationAsync(UserAccount user, string? clientIp, CancellationToken cancellationToken = default)
    {
        await RecordAsync(user, TermsDocumentRules.PurposeAccount, clientIp, cancellationToken);
    }

    private async Task<UserTermsAcceptance> RecordAsync(
        UserAccount user,
        string purpose,
        string? clientIp,
        CancellationToken cancellationToken)
    {
        var active = await ActiveMapAsync(cancellationToken);
        var termsVersion = TermsDocumentRules.RequiredVersion(VersionOf(active, TermsDocumentRules.Terms));
        var privacyVersion = TermsDocumentRules.RequiredVersion(VersionOf(active, TermsDocumentRules.Privacy));
        var now = DateTime.UtcNow;
        var ip = TermsDocumentRules.ResolveClientIp(clientIp, null, null);

        user.TermsAcceptedVersion = termsVersion;
        user.TermsAcceptedAt = now;
        user.TermsAcceptedIp = ip;
        user.Touch();

        var termsRow = NewRow(user.Id, TermsDocumentRules.Terms, termsVersion, now, ip, purpose);
        var privacyRow = NewRow(user.Id, TermsDocumentRules.Privacy, privacyVersion, now, ip, purpose);
        _context.UserTermsAcceptances.AddRange(termsRow, privacyRow);
        await _context.SaveChangesAsync(cancellationToken);
        return termsRow;
    }

    private async Task<UserAccount?> FindUserAsync(int userId, CancellationToken cancellationToken) =>
        await _context.UserAccounts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

    private async Task<Dictionary<string, string>> ActiveMapAsync(CancellationToken cancellationToken)
    {
        var rows = await _context.TermsVersions
            .AsNoTracking()
            .Where(item => item.IsActive)
            .Select(item => new { item.DocumentKind, item.Version })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(item => item.DocumentKind, item => item.Version, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<string?> LatestAcceptedVersionAsync(int userId, string kind, CancellationToken cancellationToken) =>
        await _context.UserTermsAcceptances
            .AsNoTracking()
            .Where(row => row.UserId == userId && row.DocumentKind == kind)
            .OrderByDescending(row => row.AcceptedAt)
            .ThenByDescending(row => row.Id)
            .Select(row => row.Version)
            .FirstOrDefaultAsync(cancellationToken);

    private static string? VersionOf(IReadOnlyDictionary<string, string> active, string kind) =>
        active.TryGetValue(kind, out var version) ? version : null;

    private static UserTermsAcceptance NewRow(
        int userId,
        string kind,
        string version,
        DateTime acceptedAt,
        string? ip,
        string purpose) =>
        new()
        {
            UserId = userId,
            DocumentKind = kind,
            Version = version,
            AcceptedAt = acceptedAt,
            AcceptedIp = ip,
            Purpose = purpose,
            CreatedAt = acceptedAt
        };

    private static TermsVersionResponseDto FailVersion(string message) =>
        new() { Status = false, Descricao = message };

    private static UserTermsAcceptanceResponseDto FailAcceptance(string message) =>
        new() { Status = false, Descricao = message };
}
