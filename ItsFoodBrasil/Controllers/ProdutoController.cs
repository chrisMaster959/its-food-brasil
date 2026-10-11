using Microsoft.AspNetCore.Mvc;
using ItsFoodBrasil.Models;
using ItsFoodBrasil.ViewModels;
using ItsFoodBrasil.Data;
namespace ItsFoodBrasil.Controllers
{
    public class ProdutoController : Controller
    {
        private static List<Categoria> Categorias => ProdutoStore.Categorias;
        private static List<Produto> Produtos => ProdutoStore.Produtos;


        // ============================================================
        // ESTOQUE
        // ============================================================

        public ActionResult Index(
            string? busca,
            int? idCategoria,
            string? situacao)
        {
            var todos = Produtos
                .Select(p => new ProdutoLinhaViewModel
                {
                    IdProduto = p.IdProduto,
                    IdCategoria = p.IdCategoria,
                    Nome = p.Nome,
                    Categoria = p.Categoria?.Nome ?? "",
                    Preco = p.Preco,
                    Estoque = p.QuantidadeEstoque,
                    Validade = p.Validade,
                    Ativo = p.Ativo
                })
                .ToList();

            IEnumerable<ProdutoLinhaViewModel> lista = todos;

            if (!string.IsNullOrWhiteSpace(busca))
            {
                var b = busca.Trim();

                lista = lista.Where(p =>
                    p.Nome.Contains(
                        b,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    (p.Lote ?? string.Empty).Contains(
                        b,
                        StringComparison.OrdinalIgnoreCase));
            }

            if (idCategoria.HasValue)
            {
                lista = lista.Where(
                    p => p.IdCategoria == idCategoria.Value);
            }

            lista = situacao switch
            {
                "baixo" =>
                    lista.Where(p => p.EstoqueBaixo),

                "sem" =>
                    lista.Where(p => p.SemEstoque),

                "proxima" =>
                    lista.Where(p => p.ValidadeProxima),

                "vencido" =>
                    lista.Where(p => p.Vencido),

                "inativo" =>
                    lista.Where(p => !p.Ativo),

                _ => lista
            };

            var ativos = todos
                .Where(p => p.Ativo)
                .ToList();

            var vm = new ProdutoListViewModel
            {
                Busca = busca,
                IdCategoria = idCategoria,
                Situacao = situacao,

                Categorias = Categorias,

                Produtos = lista
                    .OrderBy(p => p.Nome)
                    .ToList(),

                ProdutosAtivos = ativos.Count,

                UnidadesEmEstoque =
                    ativos.Sum(p => p.Estoque),

                ValorEmEstoque =
                    ativos.Sum(p => p.Preco * p.Estoque),

                ProdutosEmAlerta =
                    ativos.Count(p => p.EmAlerta)
            };

            return View(vm);
        }


        // ============================================================
        // NOVO PRODUTO
        // ============================================================

        [HttpGet]
        public ActionResult Create()
        {
            ViewBag.Categorias = Categorias;

            return View(new Produto
            {
                Ativo = true,
                QuantidadeEstoque = 0
            });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Produto model)
        {
            // Remove a validação da propriedade de navegação.
            ModelState.Remove(nameof(Produto.Categoria));

            if (string.IsNullOrWhiteSpace(model.Nome))
            {
                ModelState.AddModelError(
                    "Nome",
                    "Informe o nome do produto.");
            }

            if (model.IdCategoria <= 0)
            {
                ModelState.AddModelError(
                    "IdCategoria",
                    "Selecione uma categoria.");
            }

            if (model.Preco < 0)
            {
                ModelState.AddModelError(
                    "Preco",
                    "O preço não pode ser negativo.");
            }

            if (model.QuantidadeEstoque < 0)
            {
                ModelState.AddModelError(
                    "QuantidadeEstoque",
                    "A quantidade não pode ser negativa.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = Categorias;

                return View(model);
            }

            var categoria = Categorias.FirstOrDefault(
                c => c.IdCategoria == model.IdCategoria);

            if (categoria == null)
            {
                ModelState.AddModelError(
                    "IdCategoria",
                    "Categoria inválida.");

                ViewBag.Categorias = Categorias;

                return View(model);
            }

            // Gera o próximo código.
            model.IdProduto = Produtos.Any()
                ? Produtos.Max(p => p.IdProduto) + 1
                : 1;

            model.Categoria = categoria;

            Produtos.Add(model);

            TempData["Sucesso"] =
                $"Produto \"{model.Nome}\" cadastrado com sucesso!";

            return RedirectToAction(nameof(Index));
        }


        // ============================================================
        // EDITAR
        // ============================================================

        [HttpGet]
        public ActionResult Edit(int id)
        {
            var produto = Produtos.FirstOrDefault(
                p => p.IdProduto == id);

            if (produto == null)
                return NotFound();

            ViewBag.Categorias = Categorias;

            return View(produto);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Produto model)
        {
            ModelState.Remove(nameof(Produto.Categoria));

            if (string.IsNullOrWhiteSpace(model.Nome))
                ModelState.AddModelError("Nome", "Informe o nome do produto.");

            if (model.IdCategoria <= 0)
                ModelState.AddModelError("IdCategoria", "Selecione uma categoria.");

            if (model.Preco < 0)
                ModelState.AddModelError("Preco", "O preço não pode ser negativo.");

            if (model.QuantidadeEstoque < 0)
                ModelState.AddModelError("QuantidadeEstoque", "A quantidade não pode ser negativa.");

            var produto = Produtos.FirstOrDefault(p => p.IdProduto == model.IdProduto);
            if (produto == null)
                return NotFound();

            var categoria = Categorias.FirstOrDefault(c => c.IdCategoria == model.IdCategoria);
            if (model.IdCategoria > 0 && categoria == null)
                ModelState.AddModelError("IdCategoria", "Categoria inválida.");

            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = Categorias;
                return View(model);
            }

            var nomeAntigo = produto.Nome;
            var nomeNovo = model.Nome.Trim();

            produto.IdCategoria = model.IdCategoria;
            produto.Categoria = categoria!;
            produto.Nome = nomeNovo;
            produto.Descricao = model.Descricao;
            produto.Preco = model.Preco;
            produto.QuantidadeEstoque = model.QuantidadeEstoque;
            produto.Validade = model.Validade;
            produto.Ativo = model.Ativo;

            // enquanto a venda guarda o nome do produto (e não a FK), acompanha a troca de nome
            if (!nomeAntigo.Equals(nomeNovo, StringComparison.Ordinal))
            {
                foreach (var venda in VendaStore.Vendas)
                    foreach (var item in venda.Itens.Where(i => i.Produto == nomeAntigo))
                        item.Produto = nomeNovo;
            }

            TempData["Sucesso"] = $"Produto \"{produto.Nome}\" atualizado com sucesso!";

            return RedirectToAction(nameof(Index));
        }

        // vendas que incluem o produto (hoje a venda guarda o nome; no banco será a FK de ItemVenda)
        private static int QtdVendas(Produto p) =>
            VendaStore.Vendas.Count(v => v.Itens.Any(i => i.Produto == p.Nome));


        // ---------- DESATIVAR / REATIVAR (olho) ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AlternarAtivo(int id)
        {
            var produto = Produtos.FirstOrDefault(p => p.IdProduto == id);
            if (produto == null)
                return NotFound();

            produto.Ativo = !produto.Ativo;

            TempData["Sucesso"] = produto.Ativo
                ? $"Produto \"{produto.Nome}\" reativado."
                : $"Produto \"{produto.Nome}\" desativado: não aparece mais no caixa.";

            return RedirectToAction(nameof(Index));
        }


        // ---------- EXCLUIR ----------

        [HttpGet]
        public ActionResult Delete(int id)
        {
            var produto = Produtos.FirstOrDefault(p => p.IdProduto == id);
            if (produto == null)
                return NotFound();

            ViewBag.QtdVendas = QtdVendas(produto);

            return View(produto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var produto = Produtos.FirstOrDefault(p => p.IdProduto == id);
            if (produto == null)
                return NotFound();

            // revalida no servidor: produto já vendido não pode ser excluído
            var qtdVendas = QtdVendas(produto);
            if (qtdVendas > 0)
            {
                ViewBag.QtdVendas = qtdVendas;
                return View("Delete", produto);
            }

            Produtos.Remove(produto);

            TempData["Sucesso"] = $"Produto \"{produto.Nome}\" excluído com sucesso!";

            return RedirectToAction(nameof(Index));
        }
    }
}