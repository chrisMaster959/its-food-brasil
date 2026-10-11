using System.ComponentModel.DataAnnotations;

namespace ItsFoodBrasil.ViewModels
{
    public class NovaMovimentacaoViewModel
    {
        [Required(ErrorMessage = "Informe o tipo.")]
        public string Tipo { get; set; } = "Saída";

        [Required(ErrorMessage = "Informe a data.")]
        [DataType(DataType.Date)]
        public DateTime? Data { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Escolha a categoria.")]
        public string Categoria { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a descrição.")]
        [StringLength(200, ErrorMessage = "A descrição pode ter no máximo 200 caracteres.")]
        public string Descricao { get; set; } = string.Empty;

        // vem como texto mascarado ("1.234,56") e é convertido no controller
        [Required(ErrorMessage = "Informe o valor.")]
        public string Valor { get; set; } = string.Empty;

        public string[] CategoriasEntrada { get; set; } = Array.Empty<string>();
        public string[] CategoriasSaida { get; set; } = Array.Empty<string>();
    }
}