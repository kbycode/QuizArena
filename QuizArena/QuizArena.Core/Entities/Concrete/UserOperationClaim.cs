namespace QuizArena.Core.Entities.Concrete;

/// <summary>
/// Kullanıcı ↔ yetki bağlantı tablosu.
/// </summary>
/// <remarks>
/// İlk hâlde <c>UserId</c> ve <c>OperationClaimId</c> nullable idi; bu
/// "hiçbir kullanıcıya bağlı olmayan yetki ataması" gibi anlamsız satırlara
/// izin veriyordu. Zorunlu hâle getirildi ve <c>(UserId, OperationClaimId)</c>
/// çiftine tekil indeks konuldu.
/// </remarks>
public class UserOperationClaim : EntityBase
{
    public Guid UserId { get; set; }
    public Guid OperationClaimId { get; set; }

    public User User { get; set; } = null!;
    public OperationClaim OperationClaim { get; set; } = null!;
}
