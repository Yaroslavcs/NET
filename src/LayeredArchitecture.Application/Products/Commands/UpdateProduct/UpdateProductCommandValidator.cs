using FluentValidation;
using LayeredArchitecture.Application.Products.Commands.UpdateProduct;

namespace LayeredArchitecture.Application.Products.Commands.UpdateProduct;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(v => v.Id)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(100).WithMessage("Product name must not exceed 100 characters.");

        RuleFor(v => v.Description)
            .MaximumLength(500).WithMessage("Product description must not exceed 500 characters.");

        RuleFor(v => v.Price)
            .GreaterThan(0).WithMessage("Product price must be greater than 0.");

        RuleFor(v => v.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-letter code.");

        RuleFor(v => v.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity must be 0 or greater.");

        RuleFor(v => v.Category)
            .NotEmpty().WithMessage("Product category is required.")
            .MaximumLength(50).WithMessage("Product category must not exceed 50 characters.");
    }
}