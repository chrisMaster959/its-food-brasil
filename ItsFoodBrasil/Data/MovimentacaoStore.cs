using Microsoft.Data.SqlClient;
using ItsFoodBrasil.Models;

namespace ItsFoodBrasil.Data
{
    // Fluxo de caixa, lido/gravado no SQL Server (tabela Movimentacao).
    // As vendas concluídas já entram aqui sozinhas (a procedure sp_RegistrarVenda lança a entrada).
    public static class MovimentacaoStore
    {
        // Movimentações do período (datas inclusivas), com a categoria já resolvida pela view.
        public static List<Movimentacao> Listar(DateTime inicio, DateTime fim) =>
            Db.Consultar(
                @"SELECT IdMovimentacao, DataMovimentacao, Descricao, Tipo, Categoria, Valor
                  FROM dbo.vw_FluxoCaixa
                  WHERE DataMovimentacao >= @Inicio
                    AND DataMovimentacao <  DATEADD(DAY, 1, @Fim)",
                r => new Movimentacao
                {
                    Id = r.GetInt32(0),
                    Data = r.GetDateTime(1),
                    Descricao = r.GetString(2),
                    Tipo = r.GetString(3),
                    Categoria = r.GetString(4),
                    Valor = r.GetDecimal(5)
                },
                Db.PData("@Inicio", inicio.Date),
                Db.PData("@Fim", fim.Date));

        // saldo de caixa ao final do dia informado (saldo inicial + entradas - saídas)
        public static decimal SaldoAte(DateTime data) =>
            Convert.ToDecimal(Db.Escalar("SELECT dbo.fn_SaldoCaixaAte(@Data)", Db.PData("@Data", data.Date, System.Data.SqlDbType.Date)) ?? 0m);

        public static decimal Liquido(IEnumerable<Movimentacao> lista) =>
            lista.Sum(m => m.Tipo == "Entrada" ? m.Valor : -m.Valor);

        // Lançamento manual ("Nova movimentação").
        public static int Registrar(Movimentacao m, int? idFuncionario)
        {
            var id = new SqlParameter("@IdMovimentacao", System.Data.SqlDbType.Int)
            {
                Direction = System.Data.ParameterDirection.Output
            };

            Db.Procedimento("dbo.sp_RegistrarMovimentacao",
                Db.P("@Tipo", m.Tipo),
                new SqlParameter("@Valor", System.Data.SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = m.Valor },
                Db.P("@Descricao", m.Descricao),
                Db.P("@Categoria", m.Categoria),
                Db.PData("@Data", m.Data),
                Db.P("@IdFuncionario", idFuncionario),
                id);

            return (int)id.Value;
        }
    }
}
