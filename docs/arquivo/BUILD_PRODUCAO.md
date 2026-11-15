# 🚀 Guia e Script de Implantação em Produção (CD) - Ofertio

> Substituído por [`CONFIGURACAO_PRODUCAO_DOCKER.md`](./CONFIGURACAO_PRODUCAO_DOCKER.md) e [`EXECUCAO_PRODUCAO.md`](./EXECUCAO_PRODUCAO.md). Use esses dois arquivos. O domínio publicado é `https://ofertio.com.br`.

Este documento orienta o Cursor AI ou o desenvolvedor a realizar a implantação automatizada das atualizações no servidor VPS (Hetzner Cloud).

---

## 📋 Pré-requisitos
- **IP do Servidor:** `138.199.212.202`
- **Diretório no Servidor:** `/var/www/ofertio`
- **Domínio:** `dev.ofertio.com.br`

---

## 🛠️ Passo a Passo de Execução

### 1. Garantir o Push Local
Antes de conectar ao servidor, certifique-se de que todas as alterações locais do projeto foram enviadas ao GitHub:
```bash
git add .
git commit -m "feat/fix: atualizações para produção"
git push origin main


2. Conectar ao Servidor e Atualizar o Código
Conecte via SSH ao servidor Hetzner e puxe as alterações mais recentes do repositório privado:

Bash
ssh root@138.199.212.202 "cd /var/www/ofertio && eval \$(ssh-agent -s) && ssh-add ~/.ssh/id_ed25519_ofertio && git pull origin main"
3. Garantir os Arquivos de Configuração do Build
Garanta que os arquivos nuget.config e Caddyfile estejam atualizados na raiz do servidor:

nuget.config
XML
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="[https://api.nuget.org/v3/index.json](https://api.nuget.org/v3/index.json)" protocolVersion="3" />
  </packageSources>
  <config>
    <add key="signatureValidationMode" value="accept" />
  </config>
</configuration>
4. Reconstruir e Reiniciar os Containers
Execute o rebuild dos containers com o Docker Compose no servidor:

Bash
ssh root@138.199.212.202 "cd /var/www/ofertio && docker compose build --no-cache && docker compose up -d"
5. Verificação pós-implantação
Para confirmar se todos os serviços subiram corretamente e sem erros:

Bash
ssh root@138.199.212.202 "docker ps && docker compose logs --tail=20"
🤖 Prompt para o Cursor AI executar a Implantação
Quando quiser publicar uma nova versão, basta pedir ao Cursor AI no chat:

"Cursor, execute o fluxo de implantação definido no arquivo BUILD_PRODUCAO.md para enviar o código atualizado do Ofertio para a VPS Hetzner."


---

### Próximo passo no Servidor

Agora que o arquivo `nuget.config` com os repositórios oficiais foi ajustado no passo anterior, execute a compilação final no terminal da VPS para subir os containers:

```bash
docker compose build --no-cache && docker compose up -d
Assim que a compilação terminar, digite docker ps para ver os 4 containers (ofertio-db, ofertio-api, ofertio-webui, ofertio-caddy)