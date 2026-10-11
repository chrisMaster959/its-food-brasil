using ItsFoodBrasil.Models;
using ItsFoodBrasil.ViewModels;

namespace ItsFoodBrasil.Data
{
    // Armazenamento TEMPORÁRIO em memória. Troque por consultas ao banco quando existir.
    public static class CadastroStore
    {
        public const int IdAdministrador = 1;

        // Só existem dois perfis no sistema
        public static readonly Dictionary<int, string> Perfis = new()
        {
            [IdAdministrador] = "Administrador",
            [2] = "Funcionário"
        };

        public static readonly List<Funcionario> Funcionarios = new()
        {
            Novo(1, "Carlos Silva",   "carlos.silva@itsfood.com.br",   "(11) 98888-0001", 1, "Ativo"),
            Novo(2, "Ana Souza",      "ana.souza@itsfood.com.br",      "(11) 98888-0002", 2, "Ativo"),
            Novo(3, "Marcos Lima",    "marcos.lima@itsfood.com.br",    "(11) 98888-0003", 2, "Ativo"),
            Novo(4, "Maria Oliveira", "maria.oliveira@itsfood.com.br", "(11) 98888-0004", 2, "Inativo"),
        };

        private static Funcionario Novo(int id, string nome, string email, string telefone,
                                        int idPerfil, string status) => new()
        {
            IdFuncionario = id,
            IdPessoa = id,
            IdPerfil = idPerfil,
            Status = status,
            Pessoa = new Pessoa
            {
                IdPessoa = id,
                Nome = nome,
                Email = email,
                Telefone = telefone,
                Senha = string.Empty // exemplos, sem senha
            }
        };

        public static string NomePerfil(int idPerfil) =>
            Perfis.TryGetValue(idPerfil, out var nome) ? nome : "-";

        public static List<PerfilOpcaoViewModel> OpcoesPerfil() =>
            Perfis.Select(p => new PerfilOpcaoViewModel { Id = p.Key, Nome = p.Value }).ToList();

        public static int ProximoId() =>
            Funcionarios.Count == 0 ? 1 : Funcionarios.Max(f => f.IdFuncionario) + 1;

        public static bool EmailExiste(string email, int? ignorarId = null) =>
            Funcionarios.Any(f =>
                f.IdFuncionario != ignorarId &&
                f.Pessoa.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

        // true se este funcionário é o único administrador ativo (não pode sair de cena)
        public static bool UltimoAdminAtivo(Funcionario f) =>
            f.IdPerfil == IdAdministrador &&
            f.Status == "Ativo" &&
            !Funcionarios.Any(o =>
                o.IdFuncionario != f.IdFuncionario &&
                o.IdPerfil == IdAdministrador &&
                o.Status == "Ativo");

        // vendas feitas pelo funcionário (hoje a venda guarda o nome; no banco será a FK)
        public static int QtdVendas(Funcionario f) =>
            VendaStore.Vendas.Count(v =>
                v.Funcionario.Equals(f.Pessoa.Nome, StringComparison.OrdinalIgnoreCase));
    }
}