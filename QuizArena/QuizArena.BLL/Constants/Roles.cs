using QuizArena.DAL.Seed;

namespace QuizArena.BLL.Constants;

/// <summary>
/// Yetki adları. Değerler <see cref="SeedRoles"/>'ten devralınır ki
/// "veritabanına yazılan ad" ile "kontrol edilen ad" hiçbir zaman ayrışmasın.
/// </summary>
public static class Roles
{
    public const string Admin = SeedRoles.Admin;
    public const string CategoryManage = SeedRoles.CategoryManage;
    public const string QuestionManage = SeedRoles.QuestionManage;
    public const string UserManage = SeedRoles.UserManage;
}
