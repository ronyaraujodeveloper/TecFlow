namespace TecFlow.Core.Entities;

/// <summary>Visibilidade de um item de menu para um plano SaaS. Sem registro, o item é visível.</summary>
public class PlanMenuPermission : BaseEntity
{
    public int PlanId { get; set; }

    public SaasPlan? Plan { get; set; }

    public int MenuItemId { get; set; }

    public MenuItem? MenuItem { get; set; }

    public bool IsVisible { get; set; } = true;
}
