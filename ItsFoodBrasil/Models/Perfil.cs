namespace ItsFoodBrasil.Models
{
    public class Perfil
    {
        public int IdPerfil { get; set; }

        //Administrador / Funcionário
        public string Nome { get; set; } = string.Empty;

        // Relacionamento 1:N com Funcionario
        public ICollection<Funcionario> Funcionarios { get; set; }
            = new List<Funcionario>();
    }
}
