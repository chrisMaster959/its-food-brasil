using ItsFoodBrasil.Models;

namespace ItsFoodBrasil.ViewModels
{
    public class FluxoCaixaViewModel
    {
        public DateTime DataInicial { get; set; }

        public DateTime DataFinal { get; set; }

        public decimal SaldoAnterior { get; set; }

        // linhas da tabela (já com os filtros de tipo/categoria e a ordenação)
        public List<Movimentacao> Movimentacoes { get; set; } = new();

        // todo o período, sem filtro de tipo/categoria: os cards de resumo usam esta lista
        public List<Movimentacao> MovimentacoesPeriodo { get; set; } = new();

        // filtros
        public string? Tipo { get; set; }
        public string? Categoria { get; set; }
        public string[] CategoriasEntrada { get; set; } = Array.Empty<string>();
        public string[] CategoriasSaida { get; set; } = Array.Empty<string>();

        public bool FiltrandoTipoCategoria =>
            !string.IsNullOrEmpty(Tipo) || !string.IsNullOrEmpty(Categoria);

        // ordenação
        public string Ordenar { get; set; } = "data";
        public string Direcao { get; set; } = "asc";

        // mensagem de validação (ex.: data inicial maior que a final)
        public string? Erro { get; set; }

        public decimal TotalEntradas =>
            MovimentacoesPeriodo
                .Where(m => m.Tipo == "Entrada")
                .Sum(m => m.Valor);

        public decimal TotalSaidas =>
            MovimentacoesPeriodo
                .Where(m => m.Tipo == "Saída")
                .Sum(m => m.Valor);

        public decimal SaldoAtual =>
            SaldoAnterior + TotalEntradas - TotalSaidas;
    }
}