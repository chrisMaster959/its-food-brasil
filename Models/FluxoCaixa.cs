namespace ItsFoodBrasil.Models
{
    public class FluxoCaixa
    {
        public int IdFluxoCaixa { get; set; }

        public int IdConta { get; set; }

        public string Tipo { get; set; } = string.Empty;

        public decimal Valor { get; set; }

        public string? Descricao { get; set; }

        public DateTime Data { get; set; }


        // FK -> Conta
        public Conta Conta { get; set; } = null!;
    }
}
