using System.Data;
using Microsoft.Data.SqlClient;

namespace ItsFoodBrasil.Data
{
    // Acesso ao SQL Server (ADO.NET puro, sem bibliotecas extras).
    // A connection string é preenchida no Program.cs, a partir do appsettings.json.
    public static class Db
    {
        public static string ConnectionString { get; set; } = string.Empty;

        public static SqlConnection Abrir()
        {
            var conexao = new SqlConnection(ConnectionString);
            conexao.Open();
            return conexao;
        }

        // Parâmetro que aceita null (vira DBNull no banco).
        public static SqlParameter P(string nome, object? valor) =>
            new SqlParameter(nome, valor ?? DBNull.Value);

        // Parâmetro de data (aceita null). Use SqlDbType.Date para colunas DATE.
        public static SqlParameter PData(string nome, DateTime? valor, SqlDbType tipo = SqlDbType.DateTime) =>
            new SqlParameter(nome, tipo) { Value = (object?)valor ?? DBNull.Value };

        private static SqlCommand Comando(SqlConnection conexao, string sql, SqlParameter[] ps)
        {
            var cmd = new SqlCommand(sql, conexao);
            cmd.Parameters.AddRange(ps);
            return cmd;
        }

        // SELECT que devolve várias linhas. "ler" transforma cada linha em um objeto.
        public static List<T> Consultar<T>(string sql, Func<SqlDataReader, T> ler, params SqlParameter[] ps)
        {
            var lista = new List<T>();
            using var conexao = Abrir();
            using var cmd = Comando(conexao, sql, ps);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                lista.Add(ler(reader));
            return lista;
        }

        // Devolve a primeira coluna da primeira linha (COUNT, saldo, etc.).
        public static object? Escalar(string sql, params SqlParameter[] ps)
        {
            using var conexao = Abrir();
            using var cmd = Comando(conexao, sql, ps);
            var valor = cmd.ExecuteScalar();
            return valor == DBNull.Value ? null : valor;
        }

        public static int Contar(string sql, params SqlParameter[] ps) =>
            Convert.ToInt32(Escalar(sql, ps) ?? 0);

        // INSERT / UPDATE / DELETE.
        public static void Executar(string sql, params SqlParameter[] ps)
        {
            using var conexao = Abrir();
            using var cmd = Comando(conexao, sql, ps);
            cmd.ExecuteNonQuery();
        }

        // Executa uma stored procedure. Parâmetros OUTPUT podem ser lidos depois
        // pelo próprio objeto SqlParameter (propriedade Value).
        public static void Procedimento(string nome, params SqlParameter[] ps)
        {
            using var conexao = Abrir();
            using var cmd = Comando(conexao, nome, ps);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.ExecuteNonQuery();
        }

        // Procedure que termina com um SELECT de uma coluna (ex.: devolve o novo "Ativo").
        public static object? ProcedimentoEscalar(string nome, params SqlParameter[] ps)
        {
            using var conexao = Abrir();
            using var cmd = Comando(conexao, nome, ps);
            cmd.CommandType = CommandType.StoredProcedure;
            var valor = cmd.ExecuteScalar();
            return valor == DBNull.Value ? null : valor;
        }

        // As procedures do banco usam THROW (erro 50000+) com mensagem em português
        // (ex.: "Estoque insuficiente..."). Essas mensagens podem ir direto para a tela.
        public static string Mensagem(SqlException ex) =>
            ex.Number >= 50000
                ? ex.Message
                : "Não foi possível acessar o banco de dados. Verifique a conexão e tente novamente.";
    }
}
