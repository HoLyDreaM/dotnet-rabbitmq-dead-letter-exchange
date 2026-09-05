namespace RabbitMqDlx.Shared.Models;

/// <summary>
/// Demo amaçlı hata senaryosu. Gerçek sistemlerde FailureMode gövdede taşınmaz;
/// iş mantığı ve istisna türü karar verir.
/// </summary>
public enum FailureMode
{
    None = 0,
    Transient = 1,
    Poison = 2
}
