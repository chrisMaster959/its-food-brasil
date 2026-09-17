namespace ItsFoodBrasil.Models
{
    public class Pessoa
    {
        public int IdPessoa { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Telefone { get; set; } = string.Empty;

        public string Senha { get; set; } = string.Empty;


        // Relacionamento 1:N com Funcionario
        public ICollection<Funcionario> Funcionarios { get; set; }
            = new List<Funcionario>();
    }
}
