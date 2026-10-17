using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ItsFoodBrasil.ViewModels;
using ItsFoodBrasil.Data;
using ItsFoodBrasil.Helpers;

namespace ItsFoodBrasil.Controllers
{
    public class VendaController : Controller
    {
        private static readonly CultureInfo PtBr = new("pt-BR");

        private static readonly string[] FormasPagamento =
            { "Dinheiro", "Cartão de débito", "Cartão de crédito", "Pix" };

        // O catálogo do caixa vem do banco: só produtos ATIVOS aparecem para venda.
        private static List<PdvProdutoViewModel> Catalogo() => ProdutoStore.Produtos()
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

        // id do funcionário logado (guardado no cookie de login)
        private int IdFuncionarioLogado() =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        private static readonly string[] CamposOrdenacao =
            { "venda", "data", "funcionario", "itens", "desconto", "valor", "status" };

        public IActionResult Index(DateTime? de, DateTime? ate, string? status, string? ordenar, string? direcao)
        {
            // data inicial maior que a final não é permitida (validado também no servidor)
            string? erro = null;
            if (de.HasValue && ate.HasValue && de.Value.Date > ate.Value.Date)
            {
                erro = "A data inicial não pode ser maior que a data final.";
                de = null;
                ate = null;
            }

            // o filtro de datas e de status é feito no banco; a ordenação continua aqui
            IEnumerable<VendaListaItem> query = VendaStore.Listar(de, ate, status);

            // ordenação (padrão: data, mais recentes primeiro)
            bool ordemValida = ordenar != null && CamposOrdenacao.Contains(ordenar);
            string campo = ordemValida ? ordenar! : "data";
            bool desc = !ordemValida || Ordenacao.Desc(direcao);

            var lista = (campo switch
            {
                "venda" => query.Por(v => v.IdVenda, desc),
                "funcionario" => query.Por(v => v.Funcionario, desc),
                "itens" => query.Por(v => v.QtdItens, desc),
                "desconto" => query.Por(v => v.Desconto, desc),
                "valor" => query.Por(v => v.Valor, desc),
                "status" => query.Por(v => v.Status, desc),
                _ => query.Por(v => v.Data, desc)
            }).ToList();

            var vm = new VendasIndexViewModel
            {
                Vendas = lista,
                De = de,
                Ate = ate,
                Status = status,
                TotalConcluidas = lista.Where(v => v.Status == "Concluída").Sum(v => v.Valor),
                Ordenar = campo,
                Direcao = desc ? "desc" : "asc",
                Erro = erro
            };

            return View(vm);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new PdvViewModel { Catalogo = Catalogo() });
        }

        [HttpPost]
        public IActionResult Create(PdvViewModel model)
        {
            var catalogo = Catalogo();
            model.Catalogo = catalogo;

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
                var produto = catalogo.FirstOrDefault(p => p.IdProduto == item.IdProduto);
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

            // Grava no banco (procedure sp_RegistrarVenda): venda + itens + baixa de estoque
            // + entrada no fluxo de caixa, tudo numa única transação.
            int numero;
            try
            {
                numero = VendaStore.Registrar(IdFuncionarioLogado(), model.Itens, desconto);
            }
            catch (SqlException ex)
            {
                return Erro(Db.Mensagem(ex));
            }

            var msg = $"Venda #{numero} finalizada: {total.ToString("C2", PtBr)}";
            if (troco > 0) msg += $" · Troco: {troco.ToString("C2", PtBr)}";
            TempData["Sucesso"] = msg;

            return RedirectToAction("Create");
        }

        public IActionResult Details(int id)
        {
            var venda = VendaStore.Buscar(id);

            if (venda == null)
                return NotFound();

            return View(venda);
        }

        // Cancelar: devolve o estoque e remove a entrada que a venda gerou no caixa.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancelar(int id)
        {
            try
            {
                VendaStore.Cancelar(id);
                TempData["Sucesso"] = $"Venda #{id} cancelada. O estoque foi devolvido e a entrada saiu do caixa.";
            }
            catch (SqlException ex)
            {
                TempData["Erro"] = Db.Mensagem(ex);
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
