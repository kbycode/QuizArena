using QuizArena.Entities.Dtos.Categories;
using FluentValidation;

namespace QuizArena.BLL.Validation;

internal static class CategoryRules
{
    /// <summary>Renk, arayüzde CSS değeri olarak kullanılacağı için biçimi kısıtlanır.</summary>
    internal static IRuleBuilderOptions<T, string?> HexColor<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Matches("^#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$")
            .WithMessage("Renk #RRGGBB veya #RRGGBBAA biçiminde olmalıdır.");

    /// <summary>İkon tek bir emoji/kısa simge olmalı; uzun metin arayüzü bozar.</summary>
    internal static IRuleBuilderOptions<T, string?> IconValue<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(8).WithMessage("İkon en fazla 8 karakter olabilir.");
}

public sealed class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Kategori adı zorunludur.")
            .MinimumLength(2).WithMessage("Kategori adı en az 2 karakter olmalıdır.")
            .MaximumLength(64);

        RuleFor(x => x.Description).MaximumLength(512).When(x => x.Description is not null);
        RuleFor(x => x.Icon).IconValue().When(x => !string.IsNullOrWhiteSpace(x.Icon));
        RuleFor(x => x.ColorHex).HexColor().When(x => !string.IsNullOrWhiteSpace(x.ColorHex));
        RuleFor(x => x.DisplayOrder).InclusiveBetween(0, 1_000);
    }
}

public sealed class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Kategori adı zorunludur.")
            .MinimumLength(2)
            .MaximumLength(64);

        RuleFor(x => x.Description).MaximumLength(512).When(x => x.Description is not null);
        RuleFor(x => x.Icon).IconValue().When(x => !string.IsNullOrWhiteSpace(x.Icon));
        RuleFor(x => x.ColorHex).HexColor().When(x => !string.IsNullOrWhiteSpace(x.ColorHex));
        RuleFor(x => x.DisplayOrder).InclusiveBetween(0, 1_000);
    }
}
