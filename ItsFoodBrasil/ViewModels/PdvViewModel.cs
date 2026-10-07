using System.ComponentModel.DataAnnotations;

namespace ItsFoodBrasil.ViewModels
{
    public class PdvViewModel
    {
        public List<PdvItemViewModel> Itens { get; set; } = new();

        // Valores em centavos, para não depender da cultura (vírgula/ponto) no envio do formulário.
        [Range(0, int.MaxValue)]
        public int DescontoCentavos { get; set; }

        [Required]
        public string FormaPagamento { get; set; } = "Dinheiro";

        [Range(0, int.MaxValue)]
        public int? ValorRecebidoCentavos { get; set; }

        // Apenas para exibição (não vem do formulário).
        public List<PdvProdutoViewModel> Catalogo { get; set; } = new();
    }

    public class PdvItemViewModel
    {
        public int IdProduto { get; set; }
        public int Quantidade { get; set; }
    }

    public class PdvProdutoViewModel
    {
        public int IdProduto { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public int Estoque { get; set; }
    }
}