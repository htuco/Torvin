using Torvin.Model.Enums;

namespace Torvin.Services.Notifications;

// Notifikacija prije upisa: kome, kojeg tipa, tekst i zapis na koji se odnosi
public sealed record NotificationDraft(int UserId, NotificationType Type, string Title, string Message, int? RelatedEntityId);
