namespace QuizArena.Entities.Enums;

/// <summary>Odadaki katılımcının rolü.</summary>
public enum ParticipantRole
{
    /// <summary>Odayı kuran. Yarışmayı başlatma/iptal etme yetkisi vardır.</summary>
    Host = 1,

    /// <summary>Normal oyuncu.</summary>
    Player = 2
}
