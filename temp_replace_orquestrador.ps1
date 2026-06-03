$path = 'c:\Programacao\TecFlow.Automacao\TecFlow.Orquestrador\OrquestradorPrincipal.cs'
$text = Get-Content -Path $path -Raw
$pattern = '        /// <summary>\r?\n        /// Executa o pipeline completo, abrangendo coleta, processamento, IA, e publicação\.\r?\n        /// </summary>\r?\n        public async Task ExecutarPipelineCompleto\(\)[\s\S]*$'
$replacement = @'
        [HttpPost("executar")]
        public async Task<IActionResult> ExecutarPipelineEndpoint()
        {
            await _orquestradorService.ExecutarPipelineCompleto();
            return Ok(new { Message = "Pipeline executado com sucesso." });
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            var status = await _orquestradorService.ObterStatusExecucaoAsync();
            return Ok(new { Status = status });
        }
    }
}
'@
$newText = [regex]::Replace($text, $pattern, $replacement)
Set-Content -Path $path -Value $newText -Encoding utf8
Write-Output 'Replaced section successfully'
