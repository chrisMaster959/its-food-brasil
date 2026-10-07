using ItsFoodBrasil.Models;

namespace ItsFoodBrasil.ViewModels
{
    public class FluxoCaixaViewModel
    {
        public DateTime DataInicial { get; set; }

        public DateTime DataFinal { get; set; }

        public decimal SaldoAnterior { get; set; }

        public List<Movimentacao> Movimentacoes { get; set; } = new();

        public decimal TotalEntradas =>
            Movimentacoes
                .Where(m => m.Tipo == "Entrada")
                .Sum(m => m.Valor);

        public decimal TotalSaidas =>
            Movimentacoes
                .Where(m => m.Tipo == "Saída")
                .Sum(m => m.Valor);

        public decimal SaldoAtual =>
            SaldoAnterior + TotalEntradas - TotalSaidas;
    }
}