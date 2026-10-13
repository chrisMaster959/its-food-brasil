namespace ItsFoodBrasil.ViewModels
{
    // Dados de um cabeçalho de coluna clicável (usado pelo partial _ThSort).
    public class SortHeaderModel
    {
        public string Titulo { get; set; } = string.Empty;

        // nome da coluna enviado na URL (?ordenar=...)
        public string Campo { get; set; } = string.Empty;

        // ordenação atual da tela
        public string? Ordenar { get; set; }
        public string? Direcao { get; set; }

        public bool AlinharDireita { get; set; }
    }
}
