using ItsFoodBrasil.ViewModels;

namespace ItsFoodBrasil.Data
{
    // Armazenamento TEMPORÁRIO em memória. Troque por consultas ao banco quando existir.
    public static class VendaStore
    {
        public static readonly List<VendaListaItem> Vendas = new()
        {
            Novo(1048, DateTime.Now.AddMinutes(-25), "Carlos Silva", 0m, "Concluída",
                I("Presunto fatiado 200g", 12.90m, 20),
                I("Mortadela defumada 1kg", 24.50m, 10),
                I("Salsicha hot dog 500g", 9.80m, 30)),

            Novo(1047, DateTime.Now.AddHours(-2), "Ana Souza", 5m, "Concluída",
                I("Linguiça toscana 1kg", 21.90m, 10),
                I("Bacon em manta 1kg", 38.90m, 5)),

            Novo(1046, DateTime.Now.AddHours(-4), "Carlos Silva", 0m, "Pendente",
                I("Salame italiano 300g", 32.00m, 20),
                I("Queijo mussarela 500g", 27.00m, 30),
                I("Peito de peru fatiado 200g", 15.90m, 20)),

            Novo(1045, DateTime.Now.AddHours(-26), "Ana Souza", 0m, "Concluída",
                I("Apresuntado 1kg", 17.50m, 12),
                I("Linguiça calabresa 500g", 14.90m, 10)),

            Novo(1044, DateTime.Now.AddHours(-30), "Marcos Lima", 0m, "Cancelada",
                I("Bacon em manta 1kg", 38.90m, 15),
                I("Mortadela defumada 1kg", 24.50m, 10)),

            Novo(1043, DateTime.Now.AddDays(-2), "Ana Souza", 10m, "Concluída",
                I("Presunto fatiado 200g", 12.90m, 25),
                I("Peito de peru fatiado 200g", 15.90m, 15),
                I("Queijo mussarela 500g", 27.00m, 10)),

            Novo(1042, DateTime.Now.AddDays(-3), "Marcos Lima", 0m, "Concluída",
                I("Salsicha hot dog 500g", 9.80m, 20),
                I("Linguiça calabresa 500g", 14.90m, 15)),

            Novo(1041, DateTime.Now.AddDays(-4), "Carlos Silva", 20m, "Concluída",
                I("Salame italiano 300g", 32.00m, 30),
                I("Bacon em manta 1kg", 38.90m, 20),
                I("Queijo mussarela 500g", 27.00m, 25),
                I("Mortadela defumada 1kg", 24.50m, 15)),

            Novo(1040, DateTime.Now.AddDays(-5), "Ana Souza", 0m, "Concluída",
                I("Linguiça toscana 1kg", 21.90m, 15),
                I("Apresuntado 1kg", 17.50m, 10)),

            Novo(1039, DateTime.Now.AddDays(-6), "Marcos Lima", 0m, "Cancelada",
                I("Presunto fatiado 200g", 12.90m, 30),
                I("Salsicha hot dog 500g", 9.80m, 25)),

            Novo(1038, DateTime.Now.AddDays(-7), "Carlos Silva", 0m, "Concluída",
                I("Peito de peru fatiado 200g", 15.90m, 10),
                I("Linguiça calabresa 500g", 14.90m, 12)),

            Novo(1037, DateTime.Now.AddDays(-8), "Ana Souza", 15m, "Concluída",
                I("Queijo mussarela 500g", 27.00m, 20),
                I("Salame italiano 300g", 32.00m, 15),
                I("Linguiça toscana 1kg", 21.90m, 20)),
        };

        private static VendaItemDetalhe I(string produto, decimal preco, int quantidade) => new()
        {
            Produto = produto,
            PrecoUnitario = preco,
            Quantidade = quantidade
        };

        private static VendaListaItem Novo(int id, DateTime data, string funcionario, decimal desconto,
                                           string status, params VendaItemDetalhe[] itens) => new()
        {
            IdVenda = id,
            Data = data,
            Funcionario = funcionario,
            QtdItens = itens.Sum(i => i.Quantidade),
            Desconto = desconto,
            Valor = itens.Sum(i => i.Subtotal) - desconto,
            Status = status,
            Itens = itens.ToList()
        };
    }
}