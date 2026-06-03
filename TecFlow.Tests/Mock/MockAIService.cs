using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Entities;

namespace TecFlow.Tests.Mock
{
    public class MockAIService : IAIService
    {
        public Task<string> GerarDescricaoProdutoAsync(Product produto, string contextoAdicional = "")
        {
            string retorno = $"[Mock IA] Descrição para {produto.Name}. ";
            if (!string.IsNullOrEmpty(contextoAdicional)) retorno += $"Contexto: {contextoAdicional}.";
            return Task.FromResult(retorno);
        }

        public Task<List<string>> GerarVariacoesTitulosAsync(Product produto, int quantidade = 5, string contexto = "")
        {
            var titles = new List<string>();
            for (int i = 0; i < quantidade; i++)
            {
                titles.Add($"[Mock IA] {produto.Name} - Título {i + 1}");
            }
            return Task.FromResult(titles);
        }

        public Task<string> GerarScriptVideoAsync(Product produto, string formato, int duracaoEstimadaSegundos)
        {
            return Task.FromResult($"[Mock IA] Script de vídeo simulado para '{produto.Name}'. Formato: {formato}. Duração: {duracaoEstimadaSegundos}s.");
        }

        public Task<string> GerarPromptCriativoVisualAsync(string descricaoConteudo, string estiloVisual, string plataformaAlvo)
        {
            return Task.FromResult($"[Mock IA] Prompt visual para: '{descricaoConteudo}'. Estilo: {estiloVisual}. Plataforma: {plataformaAlvo}.");
        }

        public Task<string> GerarResumoAsync(string text)
        {
            string resumo = $"[Mock IA] Resumo de texto: {text?.Substring(0, Math.Min(text?.Length ?? 0, 50))}...";
            return Task.FromResult(resumo);
        }

        public Task<string> GerarResumoAsync(string prompt, CancellationToken cancellationToken = default)
        {
            string resumo = $"[Mock IA] Resumo de texto: {prompt?.Substring(0, Math.Min(prompt?.Length ?? 0, 50))}...";
            return Task.FromResult(resumo);
        }

        // Outras implementações se houver
    }
}