namespace QuizArena.Core.Entities.Concrete;

/// <summary>
/// Yetki tanımı. Rol yerine "işlem yetkisi" (operation claim) modeli:
/// <c>Admin</c> gibi kaba rollerin yanında <c>Question.Create</c> gibi
/// ince taneli yetkiler de aynı tabloda durur ve JWT'ye rol claim'i
/// olarak yazılır.
/// </summary>
public class OperationClaim : EntityBase
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public ICollection<UserOperationClaim> UserOperationClaims { get; set; } = [];
}
