namespace ItsFoodBrasil.ViewModels
{
    public class CadastroListViewModel
    {
        public string? Busca { get; set; }

        public string? Tipo { get; set; }

        public string? Situacao { get; set; }

        public int TotalFuncionarios { get; set; }

        public int FuncionariosAtivos { get; set; }

        public int FuncionariosInativos { get; set; }

        public int TotalPerfis { get; set; }

        public List<CadastroItemViewModel> Cadastros { get; set; }
            = new List<CadastroItemViewModel>();

        public bool Filtrando =>
            !string.IsNullOrWhiteSpace(Busca) ||
            !string.IsNullOrWhiteSpace(Tipo) ||
            !string.IsNullOrWhiteSpace(Situacao);
    }

    public class CadastroItemViewModel
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string? Documento { get; set; }

        public string Tipo { get; set; } = string.Empty;

        public string? Usuario { get; set; }

        public string? Perfil { get; set; }

        public bool Ativo { get; set; }

        public string DetalhesUrl { get; set; } = "#";

        public string EditarUrl { get; set; } = "#";

        public string ExcluirUrl { get; set; } = "#";
    }
}