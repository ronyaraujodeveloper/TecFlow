namespace TecFlow.Core.Enums;

/// <summary>Ciclo de vida do processamento de um evento de engajamento (comentário, DM, etc.).</summary>
public enum EngagementStatus
{
    Pendente = 1,
    Processado = 2,
    LinkEnviado = 3,
    Falhou = 4
}
