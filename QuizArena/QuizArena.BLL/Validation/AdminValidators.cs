using QuizArena.Entities.Dtos.Admin;
using FluentValidation;

namespace QuizArena.BLL.Validation;

public sealed class SaveOperationClaimRequestValidator : AbstractValidator<SaveOperationClaimRequest>
{
    public SaveOperationClaimRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Yetki adı zorunludur.")
            .MaximumLength(64)
            // Yetki adı JWT'ye rol claim'i olarak yazılıyor ve
            // [Authorize(Roles = "...")] ile karşılaştırılıyor. Boşluk veya
            // virgül içeren bir ad, virgülle ayrılmış rol listelerinde
            // beklenmedik şekilde bölünür.
            .Matches("^[A-Za-z][A-Za-z0-9._-]*$")
            .WithMessage("Yetki adı harf ile başlamalı; yalnızca harf, rakam, nokta, alt çizgi ve tire içerebilir.");

        RuleFor(x => x.Description).MaximumLength(256).When(x => x.Description is not null);
    }
}

public sealed class AssignOperationClaimRequestValidator : AbstractValidator<AssignOperationClaimRequest>
{
    public AssignOperationClaimRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Kullanıcı seçilmelidir.");
        RuleFor(x => x.OperationClaimId).NotEmpty().WithMessage("Yetki seçilmelidir.");
    }
}
