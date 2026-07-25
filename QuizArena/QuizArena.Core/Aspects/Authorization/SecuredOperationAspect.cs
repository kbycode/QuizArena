using QuizArena.Core.Exceptions;
using QuizArena.Core.Utilities.Security;
using Microsoft.Extensions.DependencyInjection;

namespace QuizArena.Core.Aspects.Authorization;

/// <summary>
/// İş metodunu yetki kontrolüyle korur.
/// </summary>
/// <remarks>
/// <para>
/// Kullanım: <c>[SecuredOperationAspect(Roles.Admin, Roles.QuestionManage)]</c>
/// (verilen yetkilerden <b>herhangi biri</b> yeterlidir.)
/// </para>
/// <para>
/// <b>Controller'daki <c>[Authorize]</c> varken bu neden gerekli?</b> Katmanlı
/// savunma (defense in depth). <c>[Authorize]</c> yalnızca HTTP kapısını korur.
/// Aynı servis metodunu bir SignalR hub'ı, arka plan görevi ya da yeni yazılan
/// bir controller yetki niteliği unutularak çağırdığında koruma ortadan kalkar.
/// Yetki kontrolünü iş metodunun kendisine bağlamak, "yeni bir giriş noktası
/// eklendi ve yetkilendirme unutuldu" hatasını yapısal olarak engeller.
/// </para>
/// </remarks>
public sealed class SecuredOperationAspect : AspectAttribute
{
    private readonly string[] _requiredRoles;

    public SecuredOperationAspect(params string[] requiredRoles)
    {
        if (requiredRoles is null or { Length: 0 })
        {
            throw new ArgumentException("En az bir yetki adı verilmelidir.", nameof(requiredRoles));
        }

        _requiredRoles = requiredRoles;

        // Her şeyden önce: yetkisiz çağrı için doğrulama/önbellek/transaction
        // maliyeti dahi doğmasın.
        Order = 1;
    }

    public override void OnBefore(AspectContext context)
    {
        var currentUser = context.Services.GetRequiredService<ICurrentUserService>();

        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException();
        }

        if (!Array.Exists(_requiredRoles, currentUser.IsInRole))
        {
            throw new ForbiddenException();
        }
    }
}
