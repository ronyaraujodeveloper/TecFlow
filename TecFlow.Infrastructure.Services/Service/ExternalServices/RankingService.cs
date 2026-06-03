// Arquivo: TecFlow.Core.Services/RankingService.cs

using Microsoft.Extensions.Logging;
using TecFlow.Infrastructure.Configuration;
using TecFlow.Database;
using Microsoft.EntityFrameworkCore;
using TecFlow.Core.Entities;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Interfaces.Repositories;

namespace TecFlow.Infrastructure.Services.Service.ExternalServices
{
    public class RankingService : IRankingService
    {
        private readonly AppDbContext _context; // Se precisar buscar dados de entidades diretamente
        private readonly IAnaliseService _analiseService; // Para obter pontuações
        private readonly ILogger<RankingService> _logger;

        public RankingService(AppDbContext context, IAnaliseService analyseService, ILogger<RankingService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _analiseService = analyseService ?? throw new ArgumentNullException(nameof(analyseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<RankedItem>> RankItemsAsync(IEnumerable<int> itemIds, CancellationToken cancellationToken = default)
        {
            if (itemIds == null || !itemIds.Any())
            {
                _logger.LogWarning("Nenhum ID de item fornecido para ranqueamento.");
                return Enumerable.Empty<RankedItem>();
            }

            var rankedItems = new List<RankedItem>();
            _logger.LogInformation("Iniciando ranqueamento para {ItemCount} itens...", itemIds.Count());

            // Executa as chamadas de cálculo de pontuação em paralelo para eficiência
            var scoreTasks = itemIds.Select(async itemId =>
            {
                try
                {
                    var score = await _analiseService.CalcularPontuacaoDeRelevanciaAsync(itemId);
                    return new { ItemId = itemId, Score = score };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao calcular pontuação para item ID {ItemId}.", itemId);
                    return new { ItemId = itemId, Score = 0m }; // Retorna 0 em caso de erro
                }
            });

            var calculatedScores = await Task.WhenAll(scoreTasks);

            // Busca os nomes/outros dados básicos dos itens em paralelo
            // Assumindo que Item é o nome da entidade no EF Core
            var currentItemIds = calculatedScores.Select(s => s.ItemId).ToList();
            var itemsData = await _context.Set<Item>() // Substitua 'Item' pelo nome da sua entidade
                                        .Where(i => currentItemIds.Contains(i.Id)) // Filtra pelo ID
                                        .ToDictionaryAsync(i => i.Id, cancellationToken: cancellationToken); // Mapeia para dicionário para acesso rápido

            foreach (var scoreInfo in calculatedScores)
            {
                if (itemsData.TryGetValue(scoreInfo.ItemId, out var item))
                {
                    rankedItems.Add(new RankedItem
                    {
                        ItemId = scoreInfo.ItemId,
                        Score = scoreInfo.Score,
                        Name = item.Name // Assumindo que sua entidade Item tem um campo 'Name'
                    });
                }
                else
                {
                    _logger.LogWarning("Dados do item com ID {ItemId} não encontrados durante o ranqueamento.", scoreInfo.ItemId);
                    // Pode adicionar um item genérico com score 0 ou omitir
                }
            }

            // Ordena os resultados pelo Score (do maior para o menor)
            rankedItems = rankedItems.OrderByDescending(ri => ri.Score).ToList();

            _logger.LogInformation("Ranqueamento concluído. {RankedCount} itens ranqueados.", rankedItems.Count);
            return rankedItems;
        }

        public async Task<IEnumerable<RankedItem>> RankItemsByPopularityAsync(IEnumerable<int> itemIds, CancellationToken cancellationToken = default)
        {
            if (itemIds == null || !itemIds.Any())
            {
                _logger.LogWarning("Nenhum ID de item fornecido para ranqueamento por popularidade.");
                return Enumerable.Empty<RankedItem>();
            }

            _logger.LogInformation("Iniciando ranqueamento por popularidade para {ItemCount} itens...", itemIds.Count());

            // Para ranquear por popularidade, você precisaria de uma métrica de popularidade no seu banco de dados
            // Ex: um campo 'ViewCount', 'OrderCount', ou uma tabela relacionada.
            // Vamos assumir que a entidade 'Item' tem um campo 'PopularityScore'.

            var currentItemIds = itemIds.ToList(); // Converter para lista para usar em Where
            var items = await _context.Set<Item>() // Substitua 'Item' pelo nome da sua entidade
                                      .Where(i => currentItemIds.Contains(i.Id))
                                      .Select(i => new RankedItem // Mapeia diretamente para a estrutura de retorno
                                      {
                                          ItemId = i.Id,
                                          Score = i.PopularityScore, // Assumindo que 'Item' tem este campo
                                          Name = i.Name // Assumindo que 'Item' tem um campo 'Name'
                                      })
                                      .ToListAsync(cancellationToken);

            // Ordena os resultados pela popularidade (do maior para o menor)
            var rankedItems = items.OrderByDescending(ri => ri.Score).ToList();

            _logger.LogInformation("Ranqueamento por popularidade concluído. {RankedCount} itens ranqueados.", rankedItems.Count);
            return rankedItems;
        }
    }
}