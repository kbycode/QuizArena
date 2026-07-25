namespace QuizArena.Core.Entities;

/// <summary>
/// Veritabanında satır karşılığı olan her nesnenin uyması gereken sözleşme.
/// Jenerik repository'nin <c>where TEntity : IEntity</c> kısıtı buna dayanır.
/// </summary>
public interface IEntity
{
    Guid Id { get; set; }
}
