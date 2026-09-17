namespace ItsFoodBrasil.Models
{
    public class Conta
    {
        public int IdConta { get; set; }

        public int IdVenda { get; set; }

        public string Tipo { get; set; } = string.Empty;

        public decimal Valor { get; set; }

        public string? Descricao { get; set; }

        public DateTime Data { get; set; }

        public string Status { get; set; } = string.Empty;


        // FK -> Venda
        public Venda Venda { get; set; } = null!;


        // Relacionamento com FluxoCaixa
        public ICollection<FluxoCaixa> FluxosCaixa { get; set; }
            = new List<FluxoCaixa>();
    }
}
