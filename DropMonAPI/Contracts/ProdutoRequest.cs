using System.ComponentModel.DataAnnotations;

namespace DropMonAPI.Contracts;

// O ID é responsabilidade do servidor e não faz parte do contrato de escrita.
public class ProdutoRequest : IValidatableObject
{
    [Required(ErrorMessage = "Informe o nome da peça.")]
    [StringLength(120, ErrorMessage = "O nome deve ter até 120 caracteres.")]
    public string Nome { get; init; } = string.Empty;

    [Required(ErrorMessage = "Informe a categoria.")]
    [StringLength(60, ErrorMessage = "A categoria deve ter até 60 caracteres.")]
    public string Categoria { get; init; } = string.Empty;

    // Nullable permite distinguir um campo ausente do valor zero.
    [Required(ErrorMessage = "Informe o preço.")]
    [Range(typeof(decimal), "0.01", "999999.99", ErrorMessage = "O preço deve estar entre 0,01 e 999.999,99.", ParseLimitsInInvariantCulture = true)]
    public decimal? Preco { get; init; }

    [Required(ErrorMessage = "Informe a quantidade em estoque.")]
    [Range(0, int.MaxValue, ErrorMessage = "O estoque deve ser um inteiro maior ou igual a zero.")]
    public int? QuantidadeEstoque { get; init; }

    public bool IsExclusivoDrop { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Preco is decimal preco && decimal.Round(preco, 2) != preco)
            yield return new ValidationResult("O preço deve ter no máximo duas casas decimais.", [nameof(Preco)]);
    }
}
