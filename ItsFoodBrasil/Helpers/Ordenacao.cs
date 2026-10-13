namespace ItsFoodBrasil.Helpers
{
    // Apoio à ordenação (crescente/decrescente) das tabelas.
    public static class Ordenacao
    {
        public static bool Desc(string? direcao) =>
            string.Equals(direcao, "desc", StringComparison.OrdinalIgnoreCase);

        public static IOrderedEnumerable<T> Por<T, K>(this IEnumerable<T> origem, Func<T, K> chave, bool desc) =>
            desc ? origem.OrderByDescending(chave) : origem.OrderBy(chave);
    }
}
