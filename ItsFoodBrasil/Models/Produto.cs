namespace ItsFoodBrasil.Models
{
    public class Produto
    {
        public int IdProduto { get; set; }

        public int IdCategoria { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string? Descricao { get; set; }

        public decimal Preco { get; set; }

        public int QuantidadeEstoque { get; set; }

        // Limite do alerta de "estoque baixo" (coluna EstoqueMinimo do banco)
        public int EstoqueMinimo { get; set; } = 20;

        public DateTime? Validade { get; set; }

        public bool Ativo { get; set; }

        // FK -> Categoria
        public Categoria Categoria { get; set; } = null!;

        // Relacionamento com ItemVenda
        public ICollection<ItemVenda> ItensVenda { get; set; }
            = new List<ItemVenda>();
    }
}