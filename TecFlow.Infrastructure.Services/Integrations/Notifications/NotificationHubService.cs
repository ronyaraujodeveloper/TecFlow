using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.Notifications;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Entities;

namespace TecFlow.Infrastructure.Services.Integrations.Notifications;

public class NotificationHubService : INotificationHubService
{
    private static readonly object FirebaseInitLock = new();
    private static bool _firebaseInitialized;

    private readonly IUserDeviceTokenRepository _deviceTokenRepository;
    private readonly FirebaseOptions _options;
    private readonly ILogger<NotificationHubService> _logger;

    public NotificationHubService(
        IUserDeviceTokenRepository deviceTokenRepository,
        IOptions<FirebaseOptions> options,
        ILogger<NotificationHubService> logger)
    {
        _deviceTokenRepository = deviceTokenRepository;
        _options = options.Value;
        _logger = logger;
        EnsureFirebaseApp();
    }

    public bool IsConfigured => _firebaseInitialized;

    public async Task<DeviceRegisterResponseDto> RegisterDeviceAsync(
        int ownerId,
        DeviceRegisterDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.DeviceToken))
        {
            return new DeviceRegisterResponseDto { Status = false, Descricao = "DeviceToken é obrigatório." };
        }

        if (string.IsNullOrWhiteSpace(dto.Platform))
        {
            return new DeviceRegisterResponseDto { Status = false, Descricao = "Platform é obrigatória (android/ios)." };
        }

        var entity = new UserDeviceToken
        {
            OwnerId = ownerId,
            Token = dto.DeviceToken.Trim(),
            Platform = dto.Platform.Trim().ToLowerInvariant(),
            DeviceId = dto.DeviceId?.Trim()
        };

        await _deviceTokenRepository.UpsertAsync(entity);

        return new DeviceRegisterResponseDto
        {
            Status = true,
            Descricao = "Dispositivo registado.",
            DeviceRegistrationId = entity.Id
        };
    }

    public async Task<NotificationSendResponseDto> SendToOwnerAsync(
        int ownerId,
        PushNotificationDto notification,
        CancellationToken cancellationToken = default)
    {
        if (!_firebaseInitialized)
        {
            return new NotificationSendResponseDto
            {
                Status = false,
                Descricao = "Firebase não configurado. Defina Firebase:CredentialsPath ou CredentialsJson."
            };
        }

        var tokens = await _deviceTokenRepository.GetActiveByOwnerIdAsync(ownerId);
        if (tokens.Count == 0)
        {
            return new NotificationSendResponseDto
            {
                Status = false,
                Descricao = "Nenhum dispositivo ativo para o utilizador."
            };
        }

        var data = new Dictionary<string, string>(notification.Data, StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(notification.DeepLinkRoute))
        {
            data["route"] = notification.DeepLinkRoute!;
        }

        var message = new MulticastMessage
        {
            Tokens = tokens.Select(t => t.Token).ToList(),
            Notification = new Notification
            {
                Title = notification.Title,
                Body = notification.Body
            },
            Data = data
        };

        var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message, cancellationToken);

        _logger.LogInformation(
            "Push enviado para OwnerId={OwnerId}: sucesso={Success}, falhas={Failure}",
            ownerId,
            response.SuccessCount,
            response.FailureCount);

        return new NotificationSendResponseDto
        {
            Status = response.SuccessCount > 0,
            Descricao = $"Enviado para {response.SuccessCount} dispositivo(s).",
            SentCount = response.SuccessCount,
            FailedCount = response.FailureCount
        };
    }

    private void EnsureFirebaseApp()
    {
        if (_firebaseInitialized || FirebaseApp.DefaultInstance is not null)
        {
            _firebaseInitialized = true;
            return;
        }

        lock (FirebaseInitLock)
        {
            if (_firebaseInitialized || FirebaseApp.DefaultInstance is not null)
            {
                _firebaseInitialized = true;
                return;
            }

            try
            {
                GoogleCredential? credential = null;

                if (!string.IsNullOrWhiteSpace(_options.CredentialsJson))
                {
                    credential = GoogleCredential.FromJson(_options.CredentialsJson);
                }
                else if (!string.IsNullOrWhiteSpace(_options.CredentialsPath) && File.Exists(_options.CredentialsPath))
                {
                    credential = GoogleCredential.FromFile(_options.CredentialsPath);
                }

                if (credential is null)
                {
                    _logger.LogWarning("Firebase Admin SDK não inicializado: credenciais ausentes.");
                    return;
                }

                FirebaseApp.Create(new AppOptions
                {
                    Credential = credential,
                    ProjectId = _options.ProjectId
                });

                _firebaseInitialized = true;
                _logger.LogInformation("Firebase Admin SDK inicializado para push notifications.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao inicializar Firebase Admin SDK.");
            }
        }
    }
}
