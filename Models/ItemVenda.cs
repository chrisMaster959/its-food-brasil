namespace ItsFoodBrasil.Models
{
    public class ItemVenda
    {
        public int IdItemVenda { get; set; }

        public int IdVenda { get; set; }

        public int IdProduto { get; set; }

        public int Quantidade { get; set; }

        public decimal ValorUnitario { get; set; }

        public decimal Desconto { get; set; }

        public decimal Subtotal { get; set; }


        // FK -> Venda
        public Venda Venda { get; set; } = null!;


        // FK -> Produto
        public Produto Produto { get; set; } = null!;
    }
}
