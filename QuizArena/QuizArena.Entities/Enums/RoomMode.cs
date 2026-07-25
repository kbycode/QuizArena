namespace QuizArena.Entities.Enums;

/// <summary>Oda oyun kipi.</summary>
public enum RoomMode
{
    /// <summary>Tek kişilik. Oda oluşturulur oluşturulmaz yarışma başlar.</summary>
    Solo = 1,

    /// <summary>Katılım kodu ile arkadaş davet edilen özel oda.</summary>
    PrivateMultiplayer = 2,

    /// <summary>Herkesin listeden görüp katılabildiği açık oda.</summary>
    PublicMultiplayer = 3
}
