namespace ItsFoodBrasil.Models
{
    public class Relatorio
    {
        public int IdRelatorio { get; set; }

        public int IdFuncionario { get; set; }

        public DateTime PeriodoInicio { get; set; }

        public DateTime PeriodoFim { get; set; }

        public DateTime GeradoEm { get; set; }


        // FK -> Funcionario
        public Funcionario Funcionario { get; set; } = null!;
    }
}
