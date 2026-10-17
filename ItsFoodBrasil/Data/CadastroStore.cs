using Microsoft.Data.SqlClient;
using ItsFoodBrasil.Helpers;
using ItsFoodBrasil.Models;
using ItsFoodBrasil.ViewModels;

namespace ItsFoodBrasil.Data
{
    // Funcionários e perfis, lidos/gravados no SQL Server (tabelas Pessoa e Funcionario).
    public static class CadastroStore
    {
        public const int IdAdministrador = 1;

        // Senha dos usuários de exemplo do script SQL (que vêm sem senha). Troque depois de entrar.
        public const string SenhaInicial = "Senha@123";

        // Só existem dois perfis no sistema (no banco: 'Admin' e 'Funcionario')
        public static readonly Dictionary<int, string> Perfis = new()
        {
            [IdAdministrador] = "Administrador",
            [2] = "Funcionário"
        };

        public static string NomePerfil(int idPerfil) =>
            Perfis.TryGetValue(idPerfil, out var nome) ? nome : "-";

        public static List<PerfilOpcaoViewModel> OpcoesPerfil() =>
            Perfis.Select(p => new PerfilOpcaoViewModel { Id = p.Key, Nome = p.Value }).ToList();

        // tradução entre o id usado nas telas e o texto gravado no banco
        private static string PerfilNoBanco(int idPerfil) =>
            idPerfil == IdAdministrador ? "Admin" : "Funcionario";

        private static int IdDoPerfil(string perfilNoBanco) =>
            perfilNoBanco == "Admin" ? IdAdministrador : 2;

        // ---------- CONSULTAS ----------

        private const string SelectFuncionario = @"
            SELECT p.IdPessoa, p.Nome, p.Email, p.Telefone, p.CPF, p.DataNascimento, p.Senha, f.Perfil, f.Ativo
            FROM dbo.Funcionario f
            JOIN dbo.Pessoa p ON p.IdPessoa = f.IdPessoa";

        private static Funcionario Ler(SqlDataReader r)
        {
            var id = r.GetInt32(0);
            var perfil = r.GetString(7);

            return new Funcionario
            {
                IdFuncionario = id,
                IdPessoa = id,
                IdPerfil = IdDoPerfil(perfil),
                Perfil = perfil,
                Status = r.GetBoolean(8) ? "Ativo" : "Inativo",
                Pessoa = new Pessoa
                {
                    IdPessoa = id,
                    Nome = r.GetString(1),
                    Email = r.IsDBNull(2) ? string.Empty : r.GetString(2),
                    Telefone = r.IsDBNull(3) ? string.Empty : r.GetString(3),
                    Cpf = r.IsDBNull(4) ? null : r.GetString(4),
                    DataNascimento = r.IsDBNull(5) ? null : r.GetDateTime(5),
                    Senha = r.IsDBNull(6) ? string.Empty : r.GetString(6)
                }
            };
        }

        public static List<Funcionario> Listar() =>
            Db.Consultar(SelectFuncionario + " ORDER BY p.Nome", Ler);

        public static Funcionario? Buscar(int id) =>
            Db.Consultar(SelectFuncionario + " WHERE f.IdPessoa = @Id", Ler, Db.P("@Id", id))
              .FirstOrDefault();

        // usado no login
        public static Funcionario? BuscarPorEmail(string email) =>
            Db.Consultar(SelectFuncionario + " WHERE p.Email = @Email", Ler, Db.P("@Email", email))
              .FirstOrDefault();

        public static bool EmailExiste(string email, int? ignorarId = null) =>
            Db.Contar("SELECT COUNT(*) FROM dbo.Pessoa WHERE Email = @Email AND (@Ignorar IS NULL OR IdPessoa <> @Ignorar)",
                      Db.P("@Email", email), Db.P("@Ignorar", ignorarId)) > 0;

        // true se este funcionário é o único administrador ativo (não pode sair de cena)
        public static bool UltimoAdminAtivo(Funcionario f) =>
            f.IdPerfil == IdAdministrador &&
            f.Status == "Ativo" &&
            Db.Contar("SELECT COUNT(*) FROM dbo.Funcionario WHERE Perfil = 'Admin' AND Ativo = 1 AND IdPessoa <> @Id",
                      Db.P("@Id", f.IdFuncionario)) == 0;

        // vendas feitas pelo funcionário (quem já vendeu não pode ser excluído, só desativado)
        public static int QtdVendas(Funcionario f) =>
            Db.Contar("SELECT COUNT(*) FROM dbo.Venda WHERE IdFuncionario = @Id",
                      Db.P("@Id", f.IdFuncionario));

        // ---------- GRAVAÇÕES (usam as procedures do banco) ----------

        // Cadastra Pessoa + Funcionario e devolve o id novo.
        public static int Criar(string nome, string email, string telefone, string senha, int idPerfil, bool ativo)
        {
            var idPessoa = new SqlParameter("@IdPessoa", System.Data.SqlDbType.Int)
            {
                Direction = System.Data.ParameterDirection.Output
            };

            Db.Procedimento("dbo.sp_CadastrarFuncionario",
                Db.P("@Nome", nome),
                Db.P("@Email", email),
                Db.P("@Telefone", telefone),
                Db.P("@Perfil", PerfilNoBanco(idPerfil)),
                idPessoa);

            var id = (int)idPessoa.Value;

            Db.Executar("UPDATE dbo.Pessoa SET Senha = @Senha WHERE IdPessoa = @Id",
                        Db.P("@Senha", Senha.Gerar(senha)), Db.P("@Id", id));

            if (!ativo)
                Db.ProcedimentoEscalar("dbo.sp_AlternarStatusFuncionario", Db.P("@IdFuncionario", id));

            return id;
        }

        public static void Atualizar(Funcionario atual, string nome, string email, string telefone,
                                     int idPerfil, bool ativo, string? novaSenha)
        {
            // CPF e data de nascimento não estão na tela: reenvia os valores que já existem
            Db.Procedimento("dbo.sp_AtualizarFuncionario",
                Db.P("@IdPessoa", atual.IdFuncionario),
                Db.P("@Nome", nome),
                Db.P("@CPF", atual.Pessoa.Cpf),
                Db.P("@Email", email),
                Db.P("@Telefone", telefone),
                Db.PData("@DataNascimento", atual.Pessoa.DataNascimento, System.Data.SqlDbType.Date),
                Db.P("@Perfil", PerfilNoBanco(idPerfil)));

            if (!string.IsNullOrEmpty(novaSenha))
                Db.Executar("UPDATE dbo.Pessoa SET Senha = @Senha WHERE IdPessoa = @Id",
                            Db.P("@Senha", Senha.Gerar(novaSenha)), Db.P("@Id", atual.IdFuncionario));

            if ((atual.Status == "Ativo") != ativo)
                Db.ProcedimentoEscalar("dbo.sp_AlternarStatusFuncionario", Db.P("@IdFuncionario", atual.IdFuncionario));
        }

        // devolve true se o funcionário ficou ativo
        public static bool AlternarStatus(int id) =>
            Convert.ToBoolean(Db.ProcedimentoEscalar("dbo.sp_AlternarStatusFuncionario", Db.P("@IdFuncionario", id)));

        public static void Excluir(int id) =>
            Db.Procedimento("dbo.sp_ExcluirFuncionario", Db.P("@IdFuncionario", id));

        // Usuários que ainda não têm senha (os de exemplo do script SQL) recebem a SenhaInicial.
        public static void DefinirSenhaInicial()
        {
            Db.Executar("UPDATE dbo.Pessoa SET Senha = @Senha " +
                        "WHERE Senha IS NULL AND IdPessoa IN (SELECT IdPessoa FROM dbo.Funcionario)",
                        Db.P("@Senha", Senha.Gerar(SenhaInicial)));
        }
    }
}
