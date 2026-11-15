namespace Ofertio.Business.Service.Mail;

/// <summary>Mensagem pronta para o remetente automático. A tela não monta este conteúdo.</summary>
public sealed class TransactionalEmail
{
    public required string To { get; init; }

    public required string Subject { get; init; }

    public required string TextBody { get; init; }

    public required string HtmlBody { get; init; }
}
