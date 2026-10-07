using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using ItsFoodBrasil.ViewModels;

namespace ItsFoodBrasil.Controllers
{
    public class VendaController : Controller
    {
        private static readonly CultureInfo PtBr = new("pt-BR");

        private static readonly string[] FormasPagamento =
            { "Dinheiro", "Cartão de débito", "Cartão de crédito", "Pix" };

        // DADOS DE EXEMPLO em memória. Troque por consulta ao banco quando existir.
        private static readonly List<PdvProdutoViewModel> Catalogo = new()
        {
            new() { IdProduto = 1,  Nome = "Presunto fatiado 200g",      Preco = 12.90m, Estoque = 140 },
            new() { IdProduto = 2,  Nome = "Mortadela defumada 1kg",     Preco = 24.50m, Estoque = 62 },
            new() { IdProduto = 3,  Nome = "Salsicha hot dog 500g",      Preco = 9.80m,  Estoque = 220 },
            new() { IdProduto = 4,  Nome = "Linguiça toscana 1kg",       Preco = 21.90m, Estoque = 8 },
            new() { IdProduto = 5,  Nome = "Salame italiano 300g",       Preco = 32.00m, Estoque = 45 },
            new() { IdProduto = 6,  Nome = "Bacon em manta 1kg",         Preco = 38.90m, Estoque = 30 },
            new() { IdProduto = 7,  Nome = "Peito de peru fatiado 200g", Preco = 15.90m, Estoque = 95 },
            new() { IdProduto = 8,  Nome = "Apresuntado 1kg",            Preco = 17.50m, Estoque = 25 },
            new() { IdProduto = 9,  Nome = "Linguiça calabresa 500g",    Preco = 14.90m, Estoque = 80 },
            new() { IdProduto = 10, Nome = "Queijo mussarela 500g",      Preco = 27.00m, Estoque = 50 },
        };

        private static int _ultimaVenda = 1048;

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new PdvViewModel { Catalogo = Catalogo });
        }

        [HttpPost]
        public IActionResult Create(PdvViewModel model)
        {
            model.Catalogo = Catalogo;

            IActionResult Erro(string mensagem)
            {
                ModelState.AddModelError(string.Empty, mensagem);
                return View(model);
            }

            // junta itens repetidos e descarta quantidades inválidas
            model.Itens = model.Itens
                .Where(i => i.Quantidade > 0)
                .GroupBy(i => i.IdProduto)
                .Select(g => new PdvItemViewModel { IdProduto = g.Key, Quantidade = g.Sum(x => x.Quantidade) })
                .ToList();

            if (model.Itens.Count == 0)
                return Erro("Adicione ao menos um produto.");

            // os preços vêm SEMPRE do servidor, nunca do navegador
            decimal subtotal = 0;
            foreach (var item in model.Itens)
            {
                var produto = Catalogo.FirstOrDefault(p => p.IdProduto == item.IdProduto);
                if (produto == null)
                    return Erro("Produto não encontrado.");
                if (item.Quantidade > produto.Estoque)
                    return Erro($"Estoque insuficiente para {produto.Nome} (disponível: {produto.Estoque}).");

                subtotal += produto.Preco * item.Quantidade;
            }

            decimal desconto = model.DescontoCentavos / 100m;
            if (desconto > subtotal)
                return Erro("O desconto não pode ser maior que o subtotal.");

            decimal total = subtotal - desconto;

            if (!FormasPagamento.Contains(model.FormaPagamento))
                return Erro("Forma de pagamento inválida.");

            decimal troco = 0;
            if (model.FormaPagamento == "Dinheiro")
            {
                decimal recebido = (model.ValorRecebidoCentavos ?? 0) / 100m;
                if (recebido < total)
                    return Erro("O valor recebido é menor que o total da venda.");
                troco = recebido - total;
            }

            // TODO: gravar Venda + ItemVenda no banco (com o funcionário logado) dentro de uma transação.
            // Por enquanto, só baixa o estoque da lista em memória.
            foreach (var item in model.Itens)
                Catalogo.First(p => p.IdProduto == item.IdProduto).Estoque -= item.Quantidade;

            var numero = ++_ultimaVenda;
            var msg = $"Venda #{numero} finalizada: {total.ToString("C2", PtBr)}";
            if (troco > 0) msg += $" · Troco: {troco.ToString("C2", PtBr)}";
            TempData["Sucesso"] = msg;

            return RedirectToAction("Create");
        }

        public IActionResult Details(int id)
        {
            return View();
        }
    }
}