namespace ItsFoodBrasil.Models
{
    public class Pessoa
    {
        public int IdPessoa { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Telefone { get; set; } = string.Empty;

        public string Senha { get; set; } = string.Empty;

        // Colunas da tabela Pessoa que não aparecem nas telas, mas precisam ser preservadas na edição
        public string? Cpf { get; set; }

        public DateTime? DataNascimento { get; set; }


        // Relacionamento 1:N com Funcionario
        public ICollection<Funcionario> Funcionarios { get; set; }
            = new List<Funcionario>();
    }
}
