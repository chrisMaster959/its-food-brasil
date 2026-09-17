namespace ItsFoodBrasil.Models
{
    public class Perfil
    {
        public int IdPerfil { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string? Descricao { get; set; }


        // Relacionamento 1:N com Funcionario
        public ICollection<Funcionario> Funcionarios { get; set; }
            = new List<Funcionario>();
    }
}
