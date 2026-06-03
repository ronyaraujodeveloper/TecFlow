using System;
using System.Collections.Generic;

namespace TecFlow.Business.Service.Application
{
    public class ConfiguracaoApplicationService
    {
        private readonly Dictionary<string, string> _settings = new();

        public void SetSetting(string key, string value) => _settings[key] = value;

        public string? GetSetting(string key) => _settings.TryGetValue(key, out var value) ? value : null;

        // Opcional: carrega configurações padrão para facilitar testes
        public void LoadDefaults()
        {
            SetSetting("OpenAI_ApiKey", "default-openai-key");
            SetSetting("Gemini_ApiKey", "default-gemini-key");
        }
    }
}