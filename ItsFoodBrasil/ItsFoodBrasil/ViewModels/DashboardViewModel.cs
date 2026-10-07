namespace ItsFoodBrasil.ViewModels
{
    public class DashboardViewModel
    {
        public decimal VendasHoje { get; set; }
        public decimal VendasMes { get; set; }
        public decimal SaldoCaixa { get; set; }
        public decimal ContasPagarProximos7Dias { get; set; }
        public int QtdContasPagar7Dias { get; set; }

        public List<VendaDiaItem> VendasUltimos30Dias { get; set; } = new();
        public List<AlertaProdutoItem> ProdutosValidadeProxima { get; set; } = new();
        public List<AlertaProdutoItem> ProdutosEstoqueBaixo { get; set; } = new();
        public List<UltimaVendaItem> UltimasVendas { get; set; } = new();
    }

    public class VendaDiaItem
    {
        public DateTime Data { get; set; }
        public decimal Total { get; set; }
    }

    public class AlertaProdutoItem
    {
        public string Nome { get; set; } = string.Empty;
        public string? Lote { get; set; }
        public DateTime? Validade { get; set; }
        public int Quantidade { get; set; }
    }

    public class UltimaVendaItem
    {
        public int IdVenda { get; set; }
        public DateTime Data { get; set; }
        public string Funcionario { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}