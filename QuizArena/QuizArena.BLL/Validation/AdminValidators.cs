using QuizArena.Entities.Dtos.Admin;
using FluentValidation;

namespace QuizArena.BLL.Validation;

public sealed class SaveOperationClaimRequestValidator : AbstractValidator<SaveOperationClaimRequest>
{
    public SaveOperationClaimRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_ => ValidationMessages.ClaimNameRequired)
            .MaximumLength(64)
            // Yetki adı JWT'ye rol claim'i olarak yazılıyor ve
            // [Authorize(Roles = "...")] ile karşılaştırılıyor. Boşluk veya
            // virgül içeren bir ad, virgülle ayrılmış rol listelerinde
            // beklenmedik şekilde bölünür.
            .Matches("^[A-Za-z][A-Za-z0-9._-]*$")
            .WithMessage(_ => ValidationMessages.ClaimNameCharset);

        RuleFor(x => x.Description).MaximumLength(256).When(x => x.Description is not null);
    }
}

public sealed class AssignOperationClaimRequestValidator : AbstractValidator<AssignOperationClaimRequest>
{
    public AssignOperationClaimRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage(_ => ValidationMessages.UserRequired);
        RuleFor(x => x.OperationClaimId).NotEmpty().WithMessage(_ => ValidationMessages.ClaimRequired);
    }
}
