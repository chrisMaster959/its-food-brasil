using ItsFoodBrasil.Models;

namespace ItsFoodBrasil.Data
{
    // Armazenamento TEMPORÁRIO em memória, compartilhado pelo Estoque e pelo Caixa (PDV).
    // Troque por consultas ao banco quando existir.
    public static class ProdutoStore
    {
        private static readonly Categoria Frios =
            new() { IdCategoria = 1, Nome = "Frios", Ativo = true };

        private static readonly Categoria Linguicas =
            new() { IdCategoria = 2, Nome = "Linguiças e Salsichas", Ativo = true };

        private static readonly Categoria Defumados =
            new() { IdCategoria = 3, Nome = "Defumados", Ativo = true };

        private static readonly Categoria Aves =
            new() { IdCategoria = 4, Nome = "Aves", Ativo = true };

        private static readonly Categoria Laticinios =
            new() { IdCategoria = 5, Nome = "Laticínios", Ativo = true };

        public static readonly List<Categoria> Categorias = new()
        {
            Frios, Linguicas, Defumados, Aves, Laticinios
        };

        public static readonly List<Produto> Produtos = new()
        {
            //  id  categoria   nome                          preço   estoque  validade(dias)  ativo
            P(1,  Frios,      "Presunto fatiado 200g",       12.90m, 140, 120,  true),
            P(2,  Frios,      "Mortadela defumada 1kg",      24.50m, 62,  45,   true),
            P(3,  Linguicas,  "Salsicha hot dog 500g",       9.80m,  220, 200,  true),
            P(4,  Linguicas,  "Linguiça toscana 1kg",        21.90m, 8,   4,    true),
            P(5,  Defumados,  "Salame italiano 300g",        32.00m, 45,  150,  true),
            P(6,  Defumados,  "Bacon em manta 1kg",          38.90m, 30,  -3,   true),
            P(7,  Aves,       "Peito de peru fatiado 200g",  15.90m, 95,  70,   true),
            P(8,  Frios,      "Apresuntado 1kg",             17.50m, 0,   null, false),
            P(9,  Linguicas,  "Linguiça calabresa 500g",     14.90m, 80,  9,    true),
            P(10, Laticinios, "Queijo mussarela 500g",       27.00m, 15,  12,   true),
        };

        private static Produto P(int id, Categoria categoria, string nome, decimal preco,
                                 int estoque, int? diasValidade, bool ativo) => new()
        {
            IdProduto = id,
            IdCategoria = categoria.IdCategoria,
            Categoria = categoria,
            Nome = nome,
            Preco = preco,
            QuantidadeEstoque = estoque,
            Validade = diasValidade.HasValue ? DateTime.Today.AddDays(diasValidade.Value) : null,
            Ativo = ativo
        };
    }
}