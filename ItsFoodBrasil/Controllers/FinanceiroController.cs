using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using ItsFoodBrasil.Data;
using ItsFoodBrasil.Models;
using ItsFoodBrasil.ViewModels;

namespace ItsFoodBrasil.Controllers
{
    public class FinanceiroController : Controller
    {
        private static readonly CultureInfo PtBr = new("pt-BR");

        private static readonly string[] CategoriasEntrada =
            { "Suprimento de caixa", "Outras entradas" };

        private static readonly string[] CategoriasSaida =
            { "Compras", "Fornecedores", "Despesas", "Salários", "Frete", "Impostos", "Outras saídas" };

        private static FluxoCaixaViewModel Montar(DateTime? dataInicial, DateTime? dataFinal)
        {
            DateTime fim = (dataFinal ?? DateTime.Today).Date;
            DateTime inicio = (dataInicial ?? DateTime.Today.AddDays(-30)).Date;
            if (fim < inicio) (inicio, fim) = (fim, inicio);

            var todas = MovimentacaoStore.Todas();

            return new FluxoCaixaViewModel
            {
                DataInicial = inicio,
                DataFinal = fim,
                SaldoAnterior = MovimentacaoStore.SaldoInicial
                                + MovimentacaoStore.Liquido(todas.Where(m => m.Data.Date < inicio)),
                Movimentacoes = todas.Where(m => m.Data.Date >= inicio && m.Data.Date <= fim).ToList()
            };
        }

        public IActionResult FluxoCaixa(DateTime? dataInicial, DateTime? dataFinal)
        {
            return View(Montar(dataInicial, dataFinal));
        }

        // ---------- NOVA MOVIMENTAÇÃO ----------

        private static NovaMovimentacaoViewModel PreencherListas(NovaMovimentacaoViewModel vm)
        {
            vm.CategoriasEntrada = CategoriasEntrada;
            vm.CategoriasSaida = CategoriasSaida;
            return vm;
        }

        [HttpGet]
        public IActionResult Nova()
        {
            return View(PreencherListas(new NovaMovimentacaoViewModel()));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Nova(NovaMovimentacaoViewModel model)
        {
            PreencherListas(model);

            // tipo e categoria precisam ser coerentes (validado no servidor, não só no navegador)
            if (model.Tipo != "Entrada" && model.Tipo != "Saída")
            {
                ModelState.AddModelError(nameof(model.Tipo), "Tipo inválido.");
            }
            else
            {
                var permitidas = model.Tipo == "Entrada" ? CategoriasEntrada : CategoriasSaida;
                if (!string.IsNullOrEmpty(model.Categoria) && !permitidas.Contains(model.Categoria))
                    ModelState.AddModelError(nameof(model.Categoria), "Categoria inválida para esse tipo.");
            }

            decimal valor = 0;
            if (!string.IsNullOrWhiteSpace(model.Valor) &&
                (!decimal.TryParse(model.Valor, NumberStyles.Number, PtBr, out valor) || valor <= 0))
            {
                ModelState.AddModelError(nameof(model.Valor), "Informe um valor maior que zero.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var data = model.Data!.Value.Date;

            // TODO: gravar no banco (DbContext) quando existir.
            MovimentacaoStore.Dados.Add(new Movimentacao
            {
                Id = MovimentacaoStore.ProximoId(),
                Data = data,
                Descricao = model.Descricao.Trim(),
                Tipo = model.Tipo,
                Categoria = model.Categoria,
                Valor = valor
            });

            TempData["Sucesso"] = $"{model.Tipo} de {valor.ToString("C2", PtBr)} registrada.";

            // se a data estiver fora do período padrão, abre um período que a inclua
            var inicio = DateTime.Today.AddDays(-30);
            var fim = DateTime.Today;
            if (data < inicio) inicio = data;
            if (data > fim) fim = data;

            return RedirectToAction(nameof(FluxoCaixa), new
            {
                dataInicial = inicio.ToString("yyyy-MM-dd"),
                dataFinal = fim.ToString("yyyy-MM-dd")
            });
        }

        // ---------- EXPORTAR ----------

        public IActionResult ExportarCsv(DateTime? dataInicial, DateTime? dataFinal)
        {
            var vm = Montar(dataInicial, dataFinal);
            var sb = new StringBuilder("Data;Descricao;Tipo;Categoria;Valor\r\n");
            foreach (var m in vm.Movimentacoes.OrderBy(m => m.Data).ThenBy(m => m.Id))
                sb.Append($"{m.Data:dd/MM/yyyy};{m.Descricao.Replace(";", ",")};{m.Tipo};{m.Categoria};{m.Valor:F2}\r\n");

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", "fluxo-de-caixa.csv");
        }
    }
}