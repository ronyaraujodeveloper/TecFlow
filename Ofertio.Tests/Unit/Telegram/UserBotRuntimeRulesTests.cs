using Ofertio.Business.Service.Telegram;

namespace Ofertio.Tests.Unit.Telegram;

public class UserBotRuntimeRulesTests
{
    [Fact]
    public void EnsureLocalFolders_ShouldCreateUploadsAndAppDataSessions()
    {
        var root = Path.Combine(Path.GetTempPath(), "ofertio-userbot-" + Guid.NewGuid().ToString("N"));
        try
        {
            var created = UserBotRuntimeRules.EnsureLocalFolders(webRootPath: null, baseDirectory: root);
            Assert.True(Directory.Exists(created.UploadsPath));
            Assert.True(Directory.Exists(created.SessionsPath));
            Assert.Contains("uploads", created.UploadsPath, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("telegram-sessions", created.SessionsPath, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
