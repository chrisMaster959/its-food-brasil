using ItsFoodBrasil.Models;

namespace ItsFoodBrasil.ViewModels
{
    public class ProdutoListViewModel
    {
        // Regras dos alertas: ajuste aqui se quiser outros limites.
        public const int LimiteEstoqueBaixo = 20;
        public const int DiasValidadeProxima = 15;

        // Filtros aplicados
        public string? Busca { get; set; }
        public int? IdCategoria { get; set; }
        public string? Situacao { get; set; }

        public List<Categoria> Categorias { get; set; } = new();
        public List<ProdutoLinhaViewModel> Produtos { get; set; } = new();

        // Indicadores (calculados sobre todos os produtos ativos, não só os filtrados)
        public int ProdutosAtivos { get; set; }
        public int UnidadesEmEstoque { get; set; }
        public decimal ValorEmEstoque { get; set; }
        public int ProdutosEmAlerta { get; set; }
    }

    public class ProdutoLinhaViewModel
    {
        public int IdProduto { get; set; }
        public int IdCategoria { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public int Estoque { get; set; }
        public string? Lote { get; set; }
        public DateTime? Validade { get; set; }
        public bool Ativo { get; set; }

        public bool SemEstoque => Estoque <= 0;
        public bool EstoqueBaixo => Estoque > 0 && Estoque < ProdutoListViewModel.LimiteEstoqueBaixo;

        public int? DiasParaVencer => Validade.HasValue ? (Validade.Value.Date - DateTime.Today).Days : null;
        public bool Vencido => DiasParaVencer < 0;
        public bool ValidadeProxima => DiasParaVencer >= 0 && DiasParaVencer <= ProdutoListViewModel.DiasValidadeProxima;

        public bool EmAlerta => Ativo && (SemEstoque || EstoqueBaixo || Vencido || ValidadeProxima);
    }
}