using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using ItsFoodBrasil.ViewModels;
using ItsFoodBrasil.Data;

namespace ItsFoodBrasil.Controllers
{
    public class VendaController : Controller
    {
        private static readonly CultureInfo PtBr = new("pt-BR");

        private static readonly string[] FormasPagamento =
            { "Dinheiro", "Cartão de débito", "Cartão de crédito", "Pix" };

        // DADOS DE EXEMPLO em memória. Troque por consulta ao banco quando existir.
        // O catálogo do caixa vem do estoque: só produtos ATIVOS aparecem para venda.
private static List<PdvProdutoViewModel> Catalogo => ProdutoStore.Produtos
            .Where(p => p.Ativo)
            .OrderBy(p => p.Nome)
            .Select(p => new PdvProdutoViewModel
            {
                IdProduto = p.IdProduto,
                Nome = p.Nome,
                Preco = p.Preco,
                Estoque = p.QuantidadeEstoque
            })
            .ToList();

        private static int _ultimaVenda = 1048;

        
        
        public IActionResult Index(DateTime? de, DateTime? ate, string? status)
        {
            IEnumerable<VendaListaItem> query = VendaStore.Vendas;

            if (de.HasValue)
                query = query.Where(v => v.Data >= de.Value.Date);

            if (ate.HasValue)
                query = query.Where(v => v.Data < ate.Value.Date.AddDays(1));

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(v => v.Status == status);

            var lista = query.OrderByDescending(v => v.Data).ToList();

            var vm = new VendasIndexViewModel
            {
                Vendas = lista,
                De = de,
                Ate = ate,
                Status = status,
                TotalConcluidas = lista.Where(v => v.Status == "Concluída").Sum(v => v.Valor)
            };

            return View(vm);
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
            ProdutoStore.Produtos.First(p => p.IdProduto == item.IdProduto).QuantidadeEstoque -= item.Quantidade;

            var numero = ++_ultimaVenda;
            VendaStore.Vendas.Add(new VendaListaItem
            {
                IdVenda = numero,
                Data = DateTime.Now,
                Funcionario = User.Identity?.Name ?? "Funcionário",
                QtdItens = model.Itens.Sum(i => i.Quantidade),
                Desconto = desconto,
                Valor = total,
                Status = "Concluída",
                Itens = model.Itens.Select(i =>
                {
                    var p = Catalogo.First(c => c.IdProduto == i.IdProduto);
                    return new VendaItemDetalhe
                    {
                        Produto = p.Nome,
                        PrecoUnitario = p.Preco,
                        Quantidade = i.Quantidade
                    };
                }).ToList()
            });
            var msg = $"Venda #{numero} finalizada: {total.ToString("C2", PtBr)}";
            if (troco > 0) msg += $" · Troco: {troco.ToString("C2", PtBr)}";
            TempData["Sucesso"] = msg;

            return RedirectToAction("Create");
        }

        public IActionResult Details(int id)
        {
            var venda = VendaStore.Vendas.FirstOrDefault(v => v.IdVenda == id);

            if (venda == null)
                return NotFound();

            return View(venda);
        }
    }
}