using Microsoft.AspNetCore.Mvc;
using ItsFoodBrasil.Models;
using ItsFoodBrasil.ViewModels;

namespace ItsFoodBrasil.Controllers
{
    public class ProdutoController : Controller
    {
        // DADOS DE EXEMPLO em memória. Troque por consulta ao banco (com Include de Categoria) quando existir.
        private static readonly Categoria Frios = new() { IdCategoria = 1, Nome = "Frios", Ativo = true };
        private static readonly Categoria Linguicas = new() { IdCategoria = 2, Nome = "Linguiças e Salsichas", Ativo = true };
        private static readonly Categoria Defumados = new() { IdCategoria = 3, Nome = "Defumados", Ativo = true };
        private static readonly Categoria Aves = new() { IdCategoria = 4, Nome = "Aves", Ativo = true };
        private static readonly Categoria Laticinios = new() { IdCategoria = 5, Nome = "Laticínios", Ativo = true };

        private static readonly List<Categoria> Categorias = new() { Frios, Linguicas, Defumados, Aves, Laticinios };

        private static readonly DateTime Hoje = DateTime.Today;

        private static readonly List<Produto> Produtos = new()
        {
            new() { IdProduto = 1,  IdCategoria = 1, Categoria = Frios,      Nome = "Presunto fatiado 200g",      Preco = 12.90m, QuantidadeEstoque = 140, Lote = "L2410A", Validade = Hoje.AddDays(120), Ativo = true },
            new() { IdProduto = 2,  IdCategoria = 1, Categoria = Frios,      Nome = "Mortadela defumada 1kg",     Preco = 24.50m, QuantidadeEstoque = 62,  Lote = "L2409C", Validade = Hoje.AddDays(45),  Ativo = true },
            new() { IdProduto = 3,  IdCategoria = 2, Categoria = Linguicas,  Nome = "Salsicha hot dog 500g",      Preco = 9.80m,  QuantidadeEstoque = 220, Lote = "L2410B", Validade = Hoje.AddDays(200), Ativo = true },
            new() { IdProduto = 4,  IdCategoria = 2, Categoria = Linguicas,  Nome = "Linguiça toscana 1kg",       Preco = 21.90m, QuantidadeEstoque = 8,   Lote = "L2408F", Validade = Hoje.AddDays(4),   Ativo = true },
            new() { IdProduto = 5,  IdCategoria = 3, Categoria = Defumados,  Nome = "Salame italiano 300g",       Preco = 32.00m, QuantidadeEstoque = 45,  Lote = "L2407D", Validade = Hoje.AddDays(150), Ativo = true },
            new() { IdProduto = 6,  IdCategoria = 3, Categoria = Defumados,  Nome = "Bacon em manta 1kg",         Preco = 38.90m, QuantidadeEstoque = 30,  Lote = "L2409E", Validade = Hoje.AddDays(-3),  Ativo = true },
            new() { IdProduto = 7,  IdCategoria = 4, Categoria = Aves,       Nome = "Peito de peru fatiado 200g", Preco = 15.90m, QuantidadeEstoque = 95,  Lote = "L2410C", Validade = Hoje.AddDays(70),  Ativo = true },
            new() { IdProduto = 8,  IdCategoria = 1, Categoria = Frios,      Nome = "Apresuntado 1kg",            Preco = 17.50m, QuantidadeEstoque = 0,   Lote = null,     Validade = null,                  Ativo = false },
            new() { IdProduto = 9,  IdCategoria = 2, Categoria = Linguicas,  Nome = "Linguiça calabresa 500g",    Preco = 14.90m, QuantidadeEstoque = 80,  Lote = "L2410D", Validade = Hoje.AddDays(9),   Ativo = true },
            new() { IdProduto = 10, IdCategoria = 5, Categoria = Laticinios, Nome = "Queijo mussarela 500g",      Preco = 27.00m, QuantidadeEstoque = 15,  Lote = "L2410E", Validade = Hoje.AddDays(12),  Ativo = true },
        };

        public ActionResult Index(string? busca, int? idCategoria, string? situacao)
        {
            var todos = Produtos.Select(p => new ProdutoLinhaViewModel
            {
                IdProduto = p.IdProduto,
                IdCategoria = p.IdCategoria,
                Nome = p.Nome,
                Categoria = p.Categoria.Nome,
                Preco = p.Preco,
                Estoque = p.QuantidadeEstoque,
                Lote = p.Lote,
                Validade = p.Validade,
                Ativo = p.Ativo
            }).ToList();

            IEnumerable<ProdutoLinhaViewModel> lista = todos;

            if (!string.IsNullOrWhiteSpace(busca))
            {
                var b = busca.Trim();
                lista = lista.Where(p =>
                    p.Nome.Contains(b, StringComparison.OrdinalIgnoreCase) ||
                    (p.Lote ?? string.Empty).Contains(b, StringComparison.OrdinalIgnoreCase));
            }

            if (idCategoria.HasValue)
                lista = lista.Where(p => p.IdCategoria == idCategoria.Value);

            lista = situacao switch
            {
                "baixo" => lista.Where(p => p.EstoqueBaixo),
                "sem" => lista.Where(p => p.SemEstoque),
                "proxima" => lista.Where(p => p.ValidadeProxima),
                "vencido" => lista.Where(p => p.Vencido),
                "inativo" => lista.Where(p => !p.Ativo),
                _ => lista
            };

            var ativos = todos.Where(p => p.Ativo).ToList();

            var vm = new ProdutoListViewModel
            {
                Busca = busca,
                IdCategoria = idCategoria,
                Situacao = situacao,
                Categorias = Categorias,
                Produtos = lista.OrderBy(p => p.Nome).ToList(),
                ProdutosAtivos = ativos.Count,
                UnidadesEmEstoque = ativos.Sum(p => p.Estoque),
                ValorEmEstoque = ativos.Sum(p => p.Preco * p.Estoque),
                ProdutosEmAlerta = ativos.Count(p => p.EmAlerta)
            };

            return View(vm);
        }


        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(Produto model)
        {
            return RedirectToAction("Index");
        }


        [HttpGet]
        public ActionResult Edit(int id)
        {
            return View();
        }

        [HttpPost]
        public ActionResult Edit(Produto model)
        {
            return RedirectToAction("Index");
        }


        public ActionResult Details(int id)
        {
            return View();
        }


        [HttpGet]
        public ActionResult Delete(int id)
        {
            return View();
        }

        [HttpPost]
        public ActionResult DeleteConfirmed(int id)
        {
            return RedirectToAction("Index");
        }
    }
}