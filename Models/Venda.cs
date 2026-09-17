namespace ItsFoodBrasil.Models
{
    public class Venda
    {
        public int IdVenda { get; set; }

        public int IdFuncionario { get; set; }

        public DateTime DataVenda { get; set; }

        public decimal ValorTotal { get; set; }

        public decimal Desconto { get; set; }

        public string Status { get; set; } = string.Empty;


        // FK -> Funcionario
        public Funcionario Funcionario { get; set; } = null!;


        // Venda possui vários itens
        public ICollection<ItemVenda> ItensVenda { get; set; }
            = new List<ItemVenda>();


        // Venda possui contas
        public ICollection<Conta> Contas { get; set; }
            = new List<Conta>();
    }
}
