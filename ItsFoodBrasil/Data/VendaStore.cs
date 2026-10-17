using System.Data;
using Microsoft.Data.SqlClient;
using ItsFoodBrasil.ViewModels;

namespace ItsFoodBrasil.Data
{
    // Vendas, lidas/gravadas no SQL Server (tabelas Venda e ItemVenda).
    public static class VendaStore
    {
        // vw_Vendas já traz o nome do funcionário e a quantidade de itens
        private const string SelectVenda = @"
            SELECT IdVenda, DataVenda, Funcionario, QtdItens, Desconto, ValorTotal, Status
            FROM dbo.vw_Vendas";

        private static VendaListaItem Ler(SqlDataReader r) => new()
        {
            IdVenda = r.GetInt32(0),
            Data = r.GetDateTime(1),
            Funcionario = r.GetString(2),
            QtdItens = r.GetInt32(3),
            Desconto = r.GetDecimal(4),
            Valor = r.GetDecimal(5),
            Status = r.GetString(6)
        };

        // Lista de vendas (sem os itens). Datas inclusivas; null = sem filtro.
        public static List<VendaListaItem> Listar(DateTime? de = null, DateTime? ate = null, string? status = null) =>
            Db.Consultar(
                SelectVenda + @"
                WHERE (@De IS NULL OR DataVenda >= @De)
                  AND (@Ate IS NULL OR DataVenda < DATEADD(DAY, 1, @Ate))
                  AND (@Status IS NULL OR Status = @Status)
                ORDER BY DataVenda DESC, IdVenda DESC",
                Ler,
                Db.PData("@De", de?.Date),
                Db.PData("@Ate", ate?.Date),
                Db.P("@Status", string.IsNullOrWhiteSpace(status) ? null : status));

        public static List<VendaListaItem> Ultimas(int quantidade) =>
            Db.Consultar(
                "SELECT TOP (@Qtd) IdVenda, DataVenda, Funcionario, QtdItens, Desconto, ValorTotal, Status " +
                "FROM dbo.vw_Vendas ORDER BY DataVenda DESC, IdVenda DESC",
                Ler,
                Db.P("@Qtd", quantidade));

        // Venda com os itens (tela de detalhes).
        public static VendaListaItem? Buscar(int id)
        {
            var venda = Db.Consultar(SelectVenda + " WHERE IdVenda = @Id", Ler, Db.P("@Id", id))
                          .FirstOrDefault();

            if (venda == null)
                return null;

            venda.Itens = Db.Consultar(
                "SELECT Produto, Quantidade, PrecoUnitario FROM dbo.vw_ItensVenda WHERE IdVenda = @Id ORDER BY Produto",
                r => new VendaItemDetalhe
                {
                    Produto = r.GetString(0),
                    Quantidade = r.GetInt32(1),
                    PrecoUnitario = r.GetDecimal(2)
                },
                Db.P("@Id", id));

            return venda;
        }

        // Registra a venda inteira numa única transação (feita pela procedure):
        // valida estoque, grava venda e itens, baixa o estoque e lança a entrada no caixa.
        // Os preços vêm do banco; só o produto e a quantidade são enviados.
        public static int Registrar(int idFuncionario, List<PdvItemViewModel> itens, decimal desconto)
        {
            var tabela = new DataTable();
            tabela.Columns.Add("IdProduto", typeof(int));
            tabela.Columns.Add("Quantidade", typeof(int));

            foreach (var item in itens)
                tabela.Rows.Add(item.IdProduto, item.Quantidade);

            var pItens = new SqlParameter("@Itens", SqlDbType.Structured)
            {
                TypeName = "dbo.TipoItemVenda",
                Value = tabela
            };

            var pIdVenda = new SqlParameter("@IdVenda", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            Db.Procedimento("dbo.sp_RegistrarVenda",
                Db.P("@IdFuncionario", idFuncionario),
                pItens,
                new SqlParameter("@Desconto", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = desconto },
                pIdVenda);

            return (int)pIdVenda.Value;
        }

        // Devolve o estoque e remove a entrada que a venda gerou no caixa.
        public static void Cancelar(int idVenda) =>
            Db.Procedimento("dbo.sp_CancelarVenda", Db.P("@IdVenda", idVenda));
    }
}
