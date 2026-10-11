namespace ItsFoodBrasil.ViewModels
{
    public class VendaItemDetalhe
    {
        public string Produto { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal Subtotal => PrecoUnitario * Quantidade;
    }

    public class VendaListaItem
    {
        public int IdVenda { get; set; }
        public DateTime Data { get; set; }
        public string Funcionario { get; set; } = string.Empty;
        public int QtdItens { get; set; }          // total de unidades
        public decimal Desconto { get; set; }
        public decimal Valor { get; set; }         // total já com o desconto
        public string Status { get; set; } = string.Empty;

        public List<VendaItemDetalhe> Itens { get; set; } = new();

        public decimal Subtotal => Itens.Sum(i => i.Subtotal);
    }

    public class VendasIndexViewModel
    {
        public List<VendaListaItem> Vendas { get; set; } = new();

        // filtros
        public DateTime? De { get; set; }
        public DateTime? Ate { get; set; }
        public string? Status { get; set; }

        public decimal TotalConcluidas { get; set; }
    }
}