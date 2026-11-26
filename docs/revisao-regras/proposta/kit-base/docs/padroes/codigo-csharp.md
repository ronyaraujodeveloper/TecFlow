# Convenções de código C#/.NET

> Universal (kit base). O que é específico do projeto (nomes de camadas, idioma das rotas, identificadores legados) fica em `docs/projeto/`.
> Formatação e estilo são garantidos pelo `.editorconfig` + `Directory.Build.props`. Este documento cobre o que a ferramenta não pega.

## 1. Idioma e nomes

- DEVE: identificadores C# (tipos, membros, variáveis, chaves de configuração) em inglês. Motivo: consistência com o framework e com bibliotecas.
- DEVE: texto exibido ao usuário em português, vindo de constante ou recurso, nunca montado no meio da regra de negócio.
- DEVE: comentários e documentação em português. Comentário explica o **porquê**, não repete o código.
- DEVE: mensagens de log em inglês (texto do template e nomes das propriedades: `"Order {OrderId} invoiced"`). Motivo: busca e agregação consistentes, mesmo idioma dos nomes de propriedade e das mensagens de bibliotecas. Mensagem ao usuário continua em português e não reaproveita o texto do log. Logs antigos em português são traduzidos quando o arquivo for tocado.
- DEVE: `errorCode` de API e chaves de configuração em inglês.
- DEVE: seguir a nomenclatura .NET: `PascalCase` para tipos e membros públicos, `_camelCase` para campos privados, `camelCase` para locais e parâmetros, interfaces com prefixo `I`, métodos assíncronos com sufixo `Async`. Verificar: `.editorconfig` (`dotnet_naming_rule.*`).
- NÃO DEVE: abreviar nomes (`usrSvc`, `qtd`). Exceções consagradas: `Id`, `Url`, `Dto`, `Http`.

## 2. Organização de arquivos

- DEVE: um tipo público por arquivo, com o nome do arquivo igual ao do tipo.
- DEVE: `namespace` com escopo de arquivo (`namespace X.Y;`). Verificar: `csharp_style_namespace_declarations = file_scoped:warning`.
- DEVE: namespace igual ao caminho da pasta. Verificar: IDE0130.
- RECOMENDADO: arquivo com até ~400 linhas e método com até ~40. Acima disso, extraia classe ou método. Motivo: facilita revisão e teste.

## 3. Nullable

- DEVE: `<Nullable>enable</Nullable>` em todos os projetos (centralizado no `Directory.Build.props`).
- NÃO DEVE: usar `!` (null-forgiving) para calar o compilador sem comentário explicando por que o valor nunca é nulo.
- DEVE: validar argumentos públicos com `ArgumentNullException.ThrowIfNull` / `ArgumentException.ThrowIfNullOrWhiteSpace`.

## 4. Assíncrono

- NÃO DEVE: usar `.Result`, `.Wait()`, `GetAwaiter().GetResult()` ou `async void` (exceto handlers de evento de UI). Motivo: deadlock e exceção perdida. Verificar: analisadores VSTHRD/CA2012 ou busca no CI.
- DEVE: receber `CancellationToken` em todo método assíncrono público de serviço e repassá-lo até o EF Core e o `HttpClient`.
- DEVE: em controller, aceitar `CancellationToken cancellationToken` no action e repassar.
- RECOMENDADO: `ConfigureAwait(false)` só em bibliotecas sem contexto de UI; em ASP.NET Core não é necessário.
- DEVE: `Task.WhenAll` para chamadas independentes, seguido de `await` (nunca `.Result` depois do `WhenAll`).

## 5. Injeção de dependência

- DEVE: registrar serviços em métodos de extensão por área (`AddXyz(this IServiceCollection)`), não diretamente no `Program.cs`.
- DEVE: escolher o tempo de vida assim:
  - `Scoped`: tudo que usa `DbContext` ou dados do usuário da requisição.
  - `Singleton`: sem estado ou com estado thread-safe (cache, opções, relógio).
  - `Transient`: objetos leves sem estado.
- NÃO DEVE: injetar serviço `Scoped` em `Singleton` ou em `BackgroundService`. Use `IServiceScopeFactory` e crie um escopo por unidade de trabalho. Verificar: `ValidateScopes`/`ValidateOnBuild` ativos em Development.
- DEVE: configuração via `IOptions<T>` com `const string SectionName` e `ValidateDataAnnotations().ValidateOnStart()` para opções obrigatórias. Motivo: falha na subida, não na primeira requisição.
- DEVE: `HttpClient` só via `IHttpClientFactory` (tipado ou nomeado). NÃO DEVE: `new HttpClient()`.
- DEVE: data/hora via abstração (`TimeProvider` ou interface do projeto); NÃO DEVE `DateTime.Now`. Grave em UTC.

## 6. Exceções e resultados

- DEVE: lançar exceção só para situação inesperada (bug, infraestrutura fora, configuração inválida).
- DEVE: falha de regra de negócio esperada (validação, saldo insuficiente, recurso não encontrado) volta do serviço como resultado tipado (`Result<T>` com código de erro), não como exceção. O controller traduz o código para o HTTP correto (`api.md` §2). Motivo: exceção não é controle de fluxo e custa caro.
- NÃO DEVE: `catch (Exception)` vazio ou que só registra e segue como se nada tivesse acontecido. Capture o tipo específico; se capturar genérico, registre e relance ou converta em resultado de erro explícito.
- DEVE: um único middleware de exceção por host, que registra e devolve `ProblemDetails` sem stack trace fora de Development (ver `api.md` §4).
- NÃO DEVE: usar `throw ex;` (perde a pilha). Use `throw;`.

## 7. Ferramentas automáticas

| Ferramenta | Onde | Garante |
|---|---|---|
| `.editorconfig` | raiz | estilo, nomes, severidade dos analisadores |
| `Directory.Build.props` | raiz | `Nullable`, `AnalysisLevel`, `EnforceCodeStyleInBuild`, warnings como erro (gradual) |
| `dotnet format --verify-no-changes` | CI | formatação igual ao `.editorconfig` |
| analisadores .NET (CAxxxx, IDExxxx) | build | qualidade e segurança básicas |

- DEVE: build sem warnings novos. Warning existente só sai da lista de supressão, nunca entra.
- NÃO DEVE: suprimir warning com `#pragma warning disable` sem comentário na mesma linha explicando o motivo.
