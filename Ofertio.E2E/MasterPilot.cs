namespace Ofertio.E2E;

/// <summary>Limite da fase Master: filtro só lê; crédito e suspensão ficam fora.</summary>
public static class MasterPilot
{
    public static readonly string[] ReadOnlyIds =
    [
        "audit-action",
        "audit-actor",
        "audit-from",
        "audit-to",
        "tenant-search",
        "tenant-status",
        "tenant-plan",
        "tenant-renewal",
        "tenant-disk"
    ];

    public static readonly string[] UntouchedIds =
    [
        "tenant-credits",
        "tenant-credits-reason",
        "tenant-action",
        "tenant-reason"
    ];
}
