namespace ItsFoodBrasil.Models
{
    public class Categoria
    {
        public int IdCategoria { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string? Descricao { get; set; }

        public bool Ativo { get; set; }


        // Relacionamento 1:N com Produto
        public ICollection<Produto> Produtos { get; set; }
            = new List<Produto>();
    }
}
