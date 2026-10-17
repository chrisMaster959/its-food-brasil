using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ItsFoodBrasil.Models;
using ItsFoodBrasil.ViewModels;
using ItsFoodBrasil.Data;
using ItsFoodBrasil.Helpers;
namespace ItsFoodBrasil.Controllers
{
    public class ProdutoController : Controller
    {
        // Lidos do banco a cada uso (assim a tela sempre mostra o que está gravado)
        private static List<Categoria> Categorias => ProdutoStore.Categorias();
        private static List<Produto> Produtos => ProdutoStore.Produtos();


        // ============================================================
        // ESTOQUE
        // ============================================================

        private static readonly string[] CamposOrdenacao =
            { "produto", "categoria", "preco", "estoque", "validade", "status" };

        public ActionResult Index(
            string? busca,
            int? idCategoria,
            string? situacao,
            string? ordenar,
            string? direcao)
        {
            var categorias = Categorias;

            var todos = Produtos
                .Select(p => new ProdutoLinhaViewModel
                {
                    IdProduto = p.IdProduto,
                    IdCategoria = p.IdCategoria,
                    Nome = p.Nome,
                    Categoria = p.Categoria?.Nome ?? "",
                    Preco = p.Preco,
                    Estoque = p.QuantidadeEstoque,
                    EstoqueMinimo = p.EstoqueMinimo,
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

            // ordenação (padrão: nome crescente)
            bool ordemValida = ordenar != null && CamposOrdenacao.Contains(ordenar);
            string campo = ordemValida ? ordenar! : "produto";
            bool desc = ordemValida && Ordenacao.Desc(direcao);

            var ordenados = campo switch
            {
                "categoria" => lista.Por(p => p.Categoria, desc),
                "preco" => lista.Por(p => p.Preco, desc),
                "estoque" => lista.Por(p => p.Estoque, desc),
                // produtos sem validade ficam sempre no final
                "validade" => lista.Por(p => p.Validade ?? (desc ? DateTime.MinValue : DateTime.MaxValue), desc),
                // crescente: ativos primeiro
                "status" => lista.Por(p => p.Ativo ? 0 : 1, desc),
                _ => lista.Por(p => p.Nome, desc)
            };

            var vm = new ProdutoListViewModel
            {
                Busca = busca,
                IdCategoria = idCategoria,
                Situacao = situacao,
                Ordenar = campo,
                Direcao = desc ? "desc" : "asc",

                Categorias = categorias,

                Produtos = ordenados
                    .ThenBy(p => p.Nome)
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

            if (model.Nome?.Trim().Length > 150)
                ModelState.AddModelError("Nome", "O nome pode ter no máximo 150 caracteres.");

            if (model.Descricao?.Trim().Length > 500)
                ModelState.AddModelError("Descricao", "A descrição pode ter no máximo 500 caracteres.");

            var categorias = Categorias;

            if (model.IdCategoria > 0 && !categorias.Any(c => c.IdCategoria == model.IdCategoria))
                ModelState.AddModelError("IdCategoria", "Categoria inválida.");

            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = categorias;

                return View(model);
            }

            try
            {
                // grava no banco (procedure sp_CadastrarProduto)
                ProdutoStore.Criar(model);
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError(string.Empty, Db.Mensagem(ex));
                ViewBag.Categorias = categorias;

                return View(model);
            }

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
            var produto = ProdutoStore.Buscar(id);

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

            if (model.Nome?.Trim().Length > 150)
                ModelState.AddModelError("Nome", "O nome pode ter no máximo 150 caracteres.");

            if (model.Descricao?.Trim().Length > 500)
                ModelState.AddModelError("Descricao", "A descrição pode ter no máximo 500 caracteres.");

            var produto = ProdutoStore.Buscar(model.IdProduto);
            if (produto == null)
                return NotFound();

            var categorias = Categorias;

            if (model.IdCategoria > 0 && !categorias.Any(c => c.IdCategoria == model.IdCategoria))
                ModelState.AddModelError("IdCategoria", "Categoria inválida.");

            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = categorias;
                return View(model);
            }

            try
            {
                // atualiza no banco (procedure sp_AtualizarProduto)
                ProdutoStore.Atualizar(model);
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError(string.Empty, Db.Mensagem(ex));
                ViewBag.Categorias = categorias;
                return View(model);
            }

            TempData["Sucesso"] = $"Produto \"{model.Nome.Trim()}\" atualizado com sucesso!";

            return RedirectToAction(nameof(Index));
        }


        // ---------- DESATIVAR / REATIVAR (olho) ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AlternarAtivo(int id)
        {
            var produto = ProdutoStore.Buscar(id);
            if (produto == null)
                return NotFound();

            try
            {
                var ficouAtivo = ProdutoStore.AlternarAtivo(id);

                TempData["Sucesso"] = ficouAtivo
                    ? $"Produto \"{produto.Nome}\" reativado."
                    : $"Produto \"{produto.Nome}\" desativado: não aparece mais no caixa.";
            }
            catch (SqlException ex)
            {
                TempData["Erro"] = Db.Mensagem(ex);
            }

            return RedirectToAction(nameof(Index));
        }


        // ---------- EXCLUIR ----------

        [HttpGet]
        public ActionResult Delete(int id)
        {
            var produto = ProdutoStore.Buscar(id);
            if (produto == null)
                return NotFound();

            ViewBag.QtdVendas = ProdutoStore.QtdVendas(id);

            return View(produto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var produto = ProdutoStore.Buscar(id);
            if (produto == null)
                return NotFound();

            // revalida no servidor: produto já vendido não pode ser excluído
            var qtdVendas = ProdutoStore.QtdVendas(id);
            if (qtdVendas > 0)
            {
                ViewBag.QtdVendas = qtdVendas;
                return View("Delete", produto);
            }

            try
            {
                ProdutoStore.Excluir(id);
            }
            catch (SqlException ex)
            {
                TempData["Erro"] = Db.Mensagem(ex);
                return RedirectToAction(nameof(Index));
            }

            TempData["Sucesso"] = $"Produto \"{produto.Nome}\" excluído com sucesso!";

            return RedirectToAction(nameof(Index));
        }
    }
}
