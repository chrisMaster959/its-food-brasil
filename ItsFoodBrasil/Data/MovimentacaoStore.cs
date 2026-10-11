using ItsFoodBrasil.Models;

namespace ItsFoodBrasil.Data
{
    // Armazenamento TEMPORÁRIO em memória. Troque por consultas ao banco quando existir.
    public static class MovimentacaoStore
    {
        // Saldo de caixa antes da primeira movimentação.
        public const decimal SaldoInicial = 12450m;

        // Lançamentos manuais (as vendas entram sozinhas, via Todas()).
        public static readonly List<Movimentacao> Dados = new()
        {
            Mov(1, 28, "Pagamento de aluguel",         "Saída",   "Despesas",            6500m),
            Mov(2, 24, "Pagamento fornecedor Frimesa", "Saída",   "Fornecedores",       10800m),
            Mov(3, 20, "Pagamento de energia",         "Saída",   "Despesas",            4230m),
            Mov(4, 15, "Compra de mercadorias",        "Saída",   "Compras",             9500m),
            Mov(5, 12, "Suprimento de caixa",          "Entrada", "Suprimento de caixa", 3000m),
            Mov(6, 9,  "Compra de mercadorias",        "Saída",   "Compras",             8800m),
            Mov(7, 5,  "Pagamento de frete",           "Saída",   "Frete",               1250m),
            Mov(8, 2,  "Pagamento fornecedor Frimesa", "Saída",   "Fornecedores",       12450m),
        };

        private static Movimentacao Mov(int id, int diasAtras, string desc, string tipo, string cat, decimal valor) =>
            new()
            {
                Id = id,
                Data = DateTime.Today.AddDays(-diasAtras),
                Descricao = desc,
                Tipo = tipo,
                Categoria = cat,
                Valor = valor
            };

        public static int ProximoId() =>
            Dados.Count == 0 ? 1 : Dados.Max(m => m.Id) + 1;

        // Manuais + vendas concluídas (entradas automáticas)
        public static List<Movimentacao> Todas()
        {
            var vendas = VendaStore.Vendas
                .Where(v => v.Status == "Concluída")
                .Select(v => new Movimentacao
                {
                    Id = 100000 + v.IdVenda,
                    Data = v.Data,
                    Descricao = $"Venda #{v.IdVenda}",
                    Tipo = "Entrada",
                    Categoria = "Vendas",
                    Valor = v.Valor
                });

            return Dados.Concat(vendas).ToList();
        }

        public static decimal Liquido(IEnumerable<Movimentacao> lista) =>
            lista.Sum(m => m.Tipo == "Entrada" ? m.Valor : -m.Valor);

        // saldo de caixa ao final do dia informado
        public static decimal SaldoAte(DateTime data) =>
            SaldoInicial + Liquido(Todas().Where(m => m.Data.Date <= data.Date));
    }
}