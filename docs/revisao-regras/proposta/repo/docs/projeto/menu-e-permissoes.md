# Menu dinâmico e permissões por plano

> Substitui `docs/rules/backend-architecture.md` §7 e `docs/rules/frontend-ui.md` §7.1 (que passam a ser uma seção só). Conteúdo mantido; só a organização mudou.

## 1. Registro de telas

- DEVE: toda tela nova tem registro em `MenuItems` (`Title`, `Route`, `Icon`, `OrderIndex`, `ParentId`). NÃO DEVE: link ou submenu fixo no `NavMenu.razor` (exceção: links do portal anônimo, como `/entrar` e `/recuperar-senha`).
- O seeding garimpa as rotas e o item aparece em `/master/menu-permissions` para permissão por plano (`PlanMenuPermissions`).
- O `NavMenu.razor` respeita `OrderIndex` e os agrupamentos por `ParentId` definidos pelo Master.

## 2. Hierarquia única: Grupo → Tela → Componente

Todo pedido de "controlar" algo por plano (tela, gráfico, campo, botão, seção, recurso) entra nesta árvore (`MenuItem.Kind`), nunca como flag avulsa ou `if (plano == ...)`.

| Tipo | Regra |
|---|---|
| Grupo (`Group`) | título sem rota; contém telas e outros grupos |
| Tela (`Page`) | tem `Route`. Negada: some da sidebar e a rota é bloqueada (`AccessRouteGuard` → `/sem-acesso`) |
| Componente (`Component` + `ComponentType`) | parte de uma tela; pai **obrigatoriamente** uma Tela; chave `{chaveDaTela}.{slug}`; registro com `Component(...)` no `DefaultMenuCatalog` + constante em `ScreenComponents`; nunca aparece na sidebar |
| Chave (`Feature`) | só na raiz; libera um conjunto de telas (`FeatureDependencies`) |

- Cascata: item negado nega tudo abaixo (`MenuHierarchyRules.ResolveDeniedKeys`). Sem registro em `PlanMenuPermissions`, o item é visível.
- `MenuHierarchyRules.ValidateParent` garante a hierarquia no drag & drop.

## 3. Checagem no servidor

- DEVE: item que expõe dados ou ações tem `[RequiresAccess(chave)]` no endpoint ou `IMenuPermissionService.IsAllowedAsync(userId, chave)` no serviço. O `AccessGate` da tela é só experiência do usuário. Verificar: teste de integração com plano sem a chave.
- DEVE: o filtro responder **HTTP 403** com `ProblemDetails` e `errorCode: "ACCESS_DENIED"` (ADR 0010). O cliente trata 403 como "sem acesso" e só o 401 como sessão expirada (`arquitetura.md` §4).
- Transição: hoje o filtro responde HTTP 200 com `{ status: false, errorCode: "ACCESS_DENIED" }` (`RequiresAccessAttribute.cs:16`) porque o cliente tratava 403 como sessão expirada. A troca acontece junto com o ajuste do cliente HTTP (PLANO, etapa 15).

## 4. Na tela

- DEVE: componente controlado = `<AccessGate Key="@ScreenComponents.Tela.Item">` envolvendo o markup.
- DEVE: componente negado é ocultado (sem placeholder nem cadeado); monte o grid para fechar o buraco (`col-12 col-lg` em pares, com o `AccessGate` envolvendo a coluna).
- NÃO DEVE: repetir na página a checagem da tela negada; o `AccessRouteGuard` do `MainLayout` já faz.
- Em `/master/menu-permissions` a matriz é um acordeão (grupos fechados, "N componentes", busca e "Expandir/Recolher tudo").
