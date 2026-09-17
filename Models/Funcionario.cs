namespace ItsFoodBrasil.Models
{
    public class Funcionario
    {
        public int IdFuncionario { get; set; }

        public int IdPessoa { get; set; }

        public int IdPerfil { get; set; }

        public string Status { get; set; } = string.Empty;


        // FK -> Pessoa
        public Pessoa Pessoa { get; set; } = null!;


        // FK -> Perfil
        public Perfil Perfil { get; set; } = null!;


        // Relacionamento com Venda
        public ICollection<Venda> Vendas { get; set; }
            = new List<Venda>();


        // Relacionamento com Relatorio
        public ICollection<Relatorio> Relatorios { get; set; }
            = new List<Relatorio>();
    }
}
