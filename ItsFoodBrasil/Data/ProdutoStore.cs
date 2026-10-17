using Microsoft.Data.SqlClient;
using ItsFoodBrasil.Models;

namespace ItsFoodBrasil.Data
{
    // Categorias e produtos (estoque), lidos/gravados no SQL Server.
    // Compartilhado pelo Estoque, pelo Caixa (PDV) e pelo Dashboard.
    public static class ProdutoStore
    {
        // ---------- CATEGORIAS ----------

        public static List<Categoria> Categorias() =>
            Db.Consultar(
                "SELECT IdCategoria, Nome, Descricao, Ativa FROM dbo.Categoria ORDER BY Nome",
                r => new Categoria
                {
                    IdCategoria = r.GetInt32(0),
                    Nome = r.GetString(1),
                    Descricao = r.IsDBNull(2) ? null : r.GetString(2),
                    Ativo = r.GetBoolean(3)
                });

        // ---------- PRODUTOS ----------

        // vw_Estoque já traz o produto junto com o nome da categoria
        private const string SelectProduto = @"
            SELECT IdProduto, IdCategoria, Categoria, Nome, Descricao, Preco, Estoque, EstoqueMinimo, DataValidade, Ativo
            FROM dbo.vw_Estoque";

        private static Produto Ler(SqlDataReader r)
        {
            var idCategoria = r.GetInt32(1);

            return new Produto
            {
                IdProduto = r.GetInt32(0),
                IdCategoria = idCategoria,
                Categoria = new Categoria { IdCategoria = idCategoria, Nome = r.GetString(2), Ativo = true },
                Nome = r.GetString(3),
                Descricao = r.IsDBNull(4) ? null : r.GetString(4),
                Preco = r.GetDecimal(5),
                QuantidadeEstoque = r.GetInt32(6),
                EstoqueMinimo = r.GetInt32(7),
                Validade = r.IsDBNull(8) ? null : r.GetDateTime(8),
                Ativo = r.GetBoolean(9)
            };
        }

        public static List<Produto> Produtos() =>
            Db.Consultar(SelectProduto + " ORDER BY Nome", Ler);

        public static Produto? Buscar(int id) =>
            Db.Consultar(SelectProduto + " WHERE IdProduto = @Id", Ler, Db.P("@Id", id))
              .FirstOrDefault();

        // quantas vendas já incluíram o produto (produto vendido não pode ser excluído)
        public static int QtdVendas(int idProduto) =>
            Db.Contar("SELECT COUNT(DISTINCT IdVenda) FROM dbo.ItemVenda WHERE IdProduto = @Id",
                      Db.P("@Id", idProduto));

        // ---------- GRAVAÇÕES (usam as procedures do banco) ----------

        public static int Criar(Produto p)
        {
            var idProduto = new SqlParameter("@IdProduto", System.Data.SqlDbType.Int)
            {
                Direction = System.Data.ParameterDirection.Output
            };

            Db.Procedimento("dbo.sp_CadastrarProduto",
                Db.P("@IdCategoria", p.IdCategoria),
                Db.P("@Nome", p.Nome.Trim()),
                Db.P("@Descricao", string.IsNullOrWhiteSpace(p.Descricao) ? null : p.Descricao.Trim()),
                Db.P("@Preco", p.Preco),
                Db.P("@QuantidadeEstoque", p.QuantidadeEstoque),
                Db.PData("@DataValidade", p.Validade?.Date, System.Data.SqlDbType.Date),
                idProduto);

            var id = (int)idProduto.Value;

            // a procedure cadastra o produto como ativo; respeita o checkbox da tela
            if (!p.Ativo)
                Db.ProcedimentoEscalar("dbo.sp_AlternarAtivoProduto", Db.P("@IdProduto", id));

            return id;
        }

        public static void Atualizar(Produto p)
        {
            // o estoque mínimo não está no formulário: mantém o valor que já existe
            var minimo = Db.Contar("SELECT EstoqueMinimo FROM dbo.Produto WHERE IdProduto = @Id",
                                   Db.P("@Id", p.IdProduto));

            Db.Procedimento("dbo.sp_AtualizarProduto",
                Db.P("@IdProduto", p.IdProduto),
                Db.P("@IdCategoria", p.IdCategoria),
                Db.P("@Nome", p.Nome.Trim()),
                Db.P("@Descricao", string.IsNullOrWhiteSpace(p.Descricao) ? null : p.Descricao.Trim()),
                Db.P("@Preco", p.Preco),
                Db.P("@QuantidadeEstoque", p.QuantidadeEstoque),
                Db.P("@EstoqueMinimo", minimo),
                Db.PData("@DataValidade", p.Validade?.Date, System.Data.SqlDbType.Date),
                Db.P("@Ativo", p.Ativo));
        }

        // devolve true se o produto ficou ativo
        public static bool AlternarAtivo(int id) =>
            Convert.ToBoolean(Db.ProcedimentoEscalar("dbo.sp_AlternarAtivoProduto", Db.P("@IdProduto", id)));

        public static void Excluir(int id) =>
            Db.Procedimento("dbo.sp_ExcluirProduto", Db.P("@IdProduto", id));
    }
}
