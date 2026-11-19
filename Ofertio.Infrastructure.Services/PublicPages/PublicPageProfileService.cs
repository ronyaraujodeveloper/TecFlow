using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Master;
using Ofertio.Business.Service.PublicPages;
using Ofertio.Business.Service.WhatsApp;
using Ofertio.Core.Entities;
using Ofertio.Database;

namespace Ofertio.Infrastructure.Services.PublicPages;

public sealed class PublicPageProfileService : IPublicPageProfileService
{
    private readonly AppDbContext _context;
    private readonly IUploadStorage _uploads;
    private readonly IPlanLimitsService _planLimits;
    private readonly IEvolutionApiService _evolution;
    private readonly ITelegramApiService _telegram;
    private readonly ILogger<PublicPageProfileService> _logger;

    public PublicPageProfileService(
        AppDbContext context,
        IUploadStorage uploads,
        IPlanLimitsService planLimits,
        IEvolutionApiService evolution,
        ITelegramApiService telegram,
        ILogger<PublicPageProfileService> logger)
    {
        _context = context;
        _uploads = uploads;
        _planLimits = planLimits;
        _evolution = evolution;
        _telegram = telegram;
        _logger = logger;
    }

    public async Task<PublicPageProfileResponseDto> GetMineAsync(int userId, CancellationToken cancellationToken = default) =>
        await OkAsync(userId, "OK", cancellationToken);

    public async Task<PublicPageProfileResponseDto> SaveAvatarAsync(
        int userId,
        Stream content,
        long length,
        CancellationToken cancellationToken = default)
    {
        var header = new byte[16];
        var read = await ReadHeaderAsync(content, header, cancellationToken);
        var error = AvatarImageRules.Validate(length, header.AsSpan(0, read));
        if (error is not null)
        {
            return Fail(error);
        }

        var tenantId = await TenantOfAsync(userId, cancellationToken);
        if (tenantId == Guid.Empty)
        {
            return Fail("Usuário não encontrado.");
        }

        var extension = AvatarImageRules.DetectExtension(header.AsSpan(0, read))!;
        var fileName = AvatarImageRules.BuildFileName(extension);
        var directory = Path.Combine(_uploads.UploadsRoot, "avatars", tenantId.ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, fileName);

        content.Position = 0;
        await using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await content.CopyToAsync(file, cancellationToken);
        }

        var profile = await LoadOrCreateProfileAsync(userId, tenantId, cancellationToken);
        var previous = profile.AvatarUrl;
        profile.AvatarUrl = AvatarImageRules.BuildPublicUrl(tenantId, fileName);
        profile.Touch();
        await _context.SaveChangesAsync(cancellationToken);
        DeleteStoredAvatar(previous);

        return await OkAsync(userId, "Foto atualizada.", cancellationToken);
    }

    public async Task<PublicPageProfileResponseDto> RemoveAvatarAsync(int userId, CancellationToken cancellationToken = default)
    {
        var profile = await _context.PublicPageProfiles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (profile?.AvatarUrl is { } avatar)
        {
            profile.AvatarUrl = null;
            profile.Touch();
            await _context.SaveChangesAsync(cancellationToken);
            DeleteStoredAvatar(avatar);
        }

        return await OkAsync(userId, "Foto removida.", cancellationToken);
    }

    public async Task<PublicPageProfileResponseDto> CreateJoinLinkAsync(
        int userId,
        JoinLinkCreateDto request,
        CancellationToken cancellationToken = default)
    {
        var channel = JoinLinkRules.FindChannel(request.Channel);
        if (channel is null)
        {
            return Fail("Escolha WhatsApp ou Telegram.");
        }

        var tenantId = await TenantOfAsync(userId, cancellationToken);
        if (tenantId == Guid.Empty)
        {
            return Fail("Usuário não encontrado.");
        }

        var existing = await _context.PublicPageJoinLinks.IgnoreQueryFilters()
            .Where(link => link.UserId == userId)
            .ToListAsync(cancellationToken);
        var limits = await _planLimits.GetForUserAsync(userId, cancellationToken);
        if (JoinLinkRules.ExceedsLimit(limits.MaxJoinLinks, existing.Count))
        {
            return Fail(PlanLimitRules.JoinLinksLimitMessage(limits));
        }

        string inviteUrl;
        string? sourceGroupId = null;
        string fallbackLabel;
        int? memberCount = null;

        if (!string.IsNullOrWhiteSpace(request.GroupId))
        {
            var resolved = await ResolveFromGroupAsync(userId, channel, request.GroupId.Trim(), existing, cancellationToken);
            if (resolved.Error is not null)
            {
                return Fail(resolved.Error);
            }

            inviteUrl = resolved.InviteUrl!;
            sourceGroupId = request.GroupId.Trim();
            fallbackLabel = resolved.GroupName!;
            memberCount = resolved.MemberCount;
        }
        else
        {
            if (!JoinLinkRules.TryNormalizeInviteUrl(channel.Name, request.InviteUrl, out inviteUrl, out var error))
            {
                return Fail(error ?? "Link de convite inválido.");
            }

            if (existing.Exists(link => string.Equals(link.InviteUrl, inviteUrl, StringComparison.OrdinalIgnoreCase)))
            {
                return Fail("Este link de convite já está na sua página.");
            }

            fallbackLabel = $"Grupo do {channel.Label}";
        }

        var link = new PublicPageJoinLink
        {
            UserId = userId,
            TenantId = tenantId,
            Channel = channel.Name,
            Label = JoinLinkRules.NormalizeLabel(request.Label, fallbackLabel),
            InviteUrl = inviteUrl,
            SourceGroupId = sourceGroupId,
            ShowMemberCount = request.ShowMemberCount && memberCount is > 0,
            MemberCount = memberCount,
            Status = JoinLinkRules.StatusActive,
            LastCheckedAt = sourceGroupId is null ? null : DateTime.UtcNow,
            SortOrder = existing.Count == 0 ? 1 : existing.Max(item => item.SortOrder) + 1
        };
        await _context.PublicPageJoinLinks.AddAsync(link, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return await OkAsync(userId, sourceGroupId is null ? "Link adicionado (não verificável pelo sistema)." : "Link adicionado.", cancellationToken);
    }

    public async Task<PublicPageProfileResponseDto> UpdateJoinLinkAsync(
        int userId,
        int joinLinkId,
        JoinLinkUpdateDto request,
        CancellationToken cancellationToken = default)
    {
        var link = await _context.PublicPageJoinLinks.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == joinLinkId && item.UserId == userId, cancellationToken);
        if (link is null)
        {
            return Fail("Link não encontrado.");
        }

        link.Label = JoinLinkRules.NormalizeLabel(request.Label, link.Label);
        link.ShowMemberCount = request.ShowMemberCount && link.MemberCount is > 0;
        if (request.IsActive)
        {
            if (link.Status == JoinLinkRules.StatusInactive)
            {
                // Reativar um link que o sistema desativou: volta a ser conferido do zero.
                link.Status = JoinLinkRules.StatusActive;
                link.StatusReason = null;
                link.FailedChecks = 0;
            }
        }
        else
        {
            link.Status = JoinLinkRules.StatusInactive;
            link.StatusReason = "Desativado por você.";
        }

        link.Touch();
        await _context.SaveChangesAsync(cancellationToken);
        return await OkAsync(userId, "Link atualizado.", cancellationToken);
    }

    public async Task<PublicPageProfileResponseDto> DeleteJoinLinkAsync(
        int userId,
        int joinLinkId,
        CancellationToken cancellationToken = default)
    {
        var link = await _context.PublicPageJoinLinks.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == joinLinkId && item.UserId == userId, cancellationToken);
        if (link is null)
        {
            return Fail("Link não encontrado.");
        }

        _context.PublicPageJoinLinks.Remove(link);
        await _context.SaveChangesAsync(cancellationToken);
        return await OkAsync(userId, "Link removido.", cancellationToken);
    }

    public async Task<JoinRedirectTarget?> ResolveJoinTargetAsync(int joinLinkId, CancellationToken cancellationToken = default)
    {
        var link = await _context.PublicPageJoinLinks.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == joinLinkId, cancellationToken);
        if (link is null
            || !JoinLinkRules.IsPublic(link.Status)
            || !JoinLinkRules.TryNormalizeInviteUrl(link.Channel, link.InviteUrl, out var invite, out _))
        {
            return null;
        }

        return new JoinRedirectTarget(link.TenantId, link.UserId, link.Channel, invite);
    }

    private async Task<(string? InviteUrl, string? GroupName, int? MemberCount, string? Error)> ResolveFromGroupAsync(
        int userId,
        JoinChannel channel,
        string groupId,
        List<PublicPageJoinLink> existing,
        CancellationToken cancellationToken)
    {
        if (existing.Exists(link => string.Equals(link.Channel, channel.Name, StringComparison.OrdinalIgnoreCase)
            && string.Equals(link.SourceGroupId, groupId, StringComparison.Ordinal)))
        {
            return (null, null, null, "Este grupo já está na sua página.");
        }

        string groupName;
        int participants;
        GroupInviteResult invite;

        if (channel.Name == JoinLinkRules.WhatsApp)
        {
            var group = await _context.WhatsAppGroups.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(item => item.UserId == userId && item.Jid == groupId && item.IsActive, cancellationToken);
            if (group is null)
            {
                return (null, null, null, "Grupo não encontrado. Sincronize seus grupos em Gerenciamento de conexões.");
            }

            if (!group.IsAdmin)
            {
                return (null, null, null, "Só é possível gerar o convite onde a sua conta do WhatsApp é administradora do grupo.");
            }

            var instance = await _context.WhatsAppIntegrations.IgnoreQueryFilters().AsNoTracking()
                .Where(item => item.UserId == userId)
                .Select(item => item.InstanceName)
                .FirstOrDefaultAsync(cancellationToken) ?? WhatsAppSessionRules.BuildInstanceName(userId);
            var state = await _evolution.GetConnectionStateAsync(instance, cancellationToken);
            if (!string.Equals(state.State, "open", StringComparison.OrdinalIgnoreCase))
            {
                return (null, null, null, "O WhatsApp não está conectado agora. Conecte em Gerenciamento de conexões e tente de novo.");
            }

            groupName = group.Name;
            participants = group.ParticipantCount;
            invite = await _evolution.FetchGroupInviteAsync(instance, groupId, cancellationToken);
        }
        else
        {
            var group = await _context.TelegramGroups.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(item => item.UserId == userId && item.ChatId == groupId && item.IsActive, cancellationToken);
            if (group is null)
            {
                return (null, null, null, "Grupo não encontrado. Sincronize seus grupos em Gerenciamento de conexões.");
            }

            if (!group.IsAdmin)
            {
                return (null, null, null, "O bot do Telegram precisa ser administrador do grupo para gerar o convite.");
            }

            var token = await _context.TelegramIntegrations.IgnoreQueryFilters().AsNoTracking()
                .Where(item => item.UserId == userId)
                .Select(item => item.Token)
                .FirstOrDefaultAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(token))
            {
                return (null, null, null, "O bot do Telegram não está conectado. Conecte em Gerenciamento de conexões.");
            }

            groupName = group.Name;
            participants = group.ParticipantCount;
            invite = await _telegram.GetChatInviteAsync(token, groupId, cancellationToken);
        }

        switch (invite.Outcome)
        {
            case GroupInviteOutcome.NotFound:
                return (null, null, null, "Não foi possível gerar o convite: o grupo não existe mais ou a conta deixou de ser administradora.");
            case GroupInviteOutcome.Unavailable:
                return (null, null, null, $"O {channel.Label} não respondeu agora. Tente novamente em instantes.");
        }

        if (!JoinLinkRules.TryNormalizeInviteUrl(channel.Name, invite.InviteUrl, out var normalized, out var error))
        {
            _logger.LogWarning("Convite devolvido pelo {Channel} fora do formato esperado para o grupo {GroupId}.", channel.Name, groupId);
            return (null, null, null, error ?? "O convite devolvido não é válido.");
        }

        return (normalized, groupName, invite.MemberCount ?? (participants > 0 ? participants : null), null);
    }

    private async Task<PublicPageProfileResponseDto> OkAsync(int userId, string message, CancellationToken cancellationToken) =>
        new() { Status = true, Descricao = message, Data = await BuildAsync(userId, cancellationToken) };

    private static PublicPageProfileResponseDto Fail(string message) => new() { Status = false, Descricao = message };

    private async Task<PublicPageProfileDto> BuildAsync(int userId, CancellationToken cancellationToken)
    {
        var profile = await _context.PublicPageProfiles.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        var links = await _context.PublicPageJoinLinks.IgnoreQueryFilters().AsNoTracking()
            .Where(link => link.UserId == userId)
            .OrderBy(link => link.SortOrder)
            .ThenBy(link => link.Id)
            .ToListAsync(cancellationToken);
        var clicks = await _context.PublicPageEvents.IgnoreQueryFilters().AsNoTracking()
            .Where(item => item.UserId == userId && item.Kind == PublicPageEvent.KindJoin && !item.IsBot && item.JoinLinkId != null)
            .GroupBy(item => item.JoinLinkId!.Value)
            .Select(group => new { Id = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Id, item => item.Count, cancellationToken);
        var limits = await _planLimits.GetForUserAsync(userId, cancellationToken);
        var whatsApp = await _context.WhatsAppGroups.IgnoreQueryFilters().AsNoTracking()
            .Where(group => group.UserId == userId && group.IsActive)
            .OrderBy(group => group.Name)
            .ToListAsync(cancellationToken);
        var telegram = await _context.TelegramGroups.IgnoreQueryFilters().AsNoTracking()
            .Where(group => group.UserId == userId && group.IsActive)
            .OrderBy(group => group.Name)
            .ToListAsync(cancellationToken);

        var names = whatsApp.ToDictionary(group => (JoinLinkRules.WhatsApp, group.Jid), group => group.Name);
        foreach (var group in telegram)
        {
            names[(JoinLinkRules.Telegram, group.ChatId)] = group.Name;
        }

        bool Used(string channel, string id) => links.Exists(link =>
            link.Channel == channel && string.Equals(link.SourceGroupId, id, StringComparison.Ordinal));

        return new PublicPageProfileDto
        {
            AvatarUrl = profile?.AvatarUrl,
            MaxJoinLinks = limits.MaxJoinLinks,
            CanAddJoinLink = !JoinLinkRules.ExceedsLimit(limits.MaxJoinLinks, links.Count),
            JoinLinks = links.Select(link => new PublicPageJoinLinkDto
            {
                Id = link.Id,
                Channel = link.Channel,
                Label = link.Label,
                InviteUrl = link.InviteUrl,
                SourceGroupName = link.SourceGroupId is not null && names.TryGetValue((link.Channel, link.SourceGroupId), out var name) ? name : null,
                IsVerifiable = link.SourceGroupId is not null,
                ShowMemberCount = link.ShowMemberCount,
                MemberCount = link.MemberCount,
                Status = link.Status,
                StatusReason = link.StatusReason,
                LastCheckedAt = link.LastCheckedAt,
                Clicks = clicks.GetValueOrDefault(link.Id)
            }).ToList(),
            AvailableGroups = whatsApp
                .Select(group => new AvailableJoinGroupDto
                {
                    Channel = JoinLinkRules.WhatsApp,
                    GroupId = group.Jid,
                    Name = group.Name,
                    ParticipantCount = group.ParticipantCount,
                    CanInvite = group.IsAdmin,
                    AlreadyUsed = Used(JoinLinkRules.WhatsApp, group.Jid)
                })
                .Concat(telegram.Select(group => new AvailableJoinGroupDto
                {
                    Channel = JoinLinkRules.Telegram,
                    GroupId = group.ChatId,
                    Name = group.Name,
                    ParticipantCount = group.ParticipantCount,
                    CanInvite = group.IsAdmin,
                    AlreadyUsed = Used(JoinLinkRules.Telegram, group.ChatId)
                }))
                .ToList()
        };
    }

    private async Task<Guid> TenantOfAsync(int userId, CancellationToken cancellationToken) =>
        await _context.UserAccounts.IgnoreQueryFilters().AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.TenantId)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<PublicPageProfile> LoadOrCreateProfileAsync(int userId, Guid tenantId, CancellationToken cancellationToken)
    {
        var profile = await _context.PublicPageProfiles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (profile is not null)
        {
            return profile;
        }

        profile = new PublicPageProfile { UserId = userId, TenantId = tenantId };
        await _context.PublicPageProfiles.AddAsync(profile, cancellationToken);
        return profile;
    }

    private void DeleteStoredAvatar(string? publicUrl)
    {
        var relative = AvatarImageRules.ToRelativeStoragePath(publicUrl);
        if (relative is null)
        {
            return;
        }

        try
        {
            var full = Path.GetFullPath(Path.Combine(_uploads.UploadsRoot, relative));
            var root = Path.GetFullPath(_uploads.UploadsRoot) + Path.DirectorySeparatorChar;
            if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(full))
            {
                File.Delete(full);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Não foi possível apagar a foto antiga do afiliado.");
        }
    }

    private static async Task<int> ReadHeaderAsync(Stream content, byte[] buffer, CancellationToken cancellationToken)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await content.ReadAsync(buffer.AsMemory(total, buffer.Length - total), cancellationToken);
            if (read == 0)
            {
                break;
            }

            total += read;
        }

        return total;
    }
}
