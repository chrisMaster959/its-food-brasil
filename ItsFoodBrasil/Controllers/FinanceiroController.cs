using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ItsFoodBrasil.Data;
using ItsFoodBrasil.Helpers;
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

        private static readonly string[] CamposOrdenacao =
            { "data", "descricao", "categoria", "tipo", "valor" };

        private static FluxoCaixaViewModel Montar(
            DateTime? dataInicial, DateTime? dataFinal,
            string? tipo, string? categoria,
            string? ordenar, string? direcao)
        {
            string? erro = null;
            DateTime fim = (dataFinal ?? DateTime.Today).Date;
            DateTime inicio = (dataInicial ?? DateTime.Today.AddDays(-30)).Date;

            // data inicial maior que a final não é permitido (validado também no servidor):
            // volta para o período padrão e avisa
            if (inicio > fim)
            {
                erro = "A data inicial não pode ser maior que a data final.";
                inicio = DateTime.Today.AddDays(-30);
                fim = DateTime.Today;
            }

            if (tipo != "Entrada" && tipo != "Saída") tipo = null;
            if (string.IsNullOrWhiteSpace(categoria)) categoria = null;

            // período completo (base dos cards de resumo), direto do banco
            var periodo = MovimentacaoStore.Listar(inicio, fim);

            // filtros da tabela
            IEnumerable<Movimentacao> filtradas = periodo;
            if (tipo != null) filtradas = filtradas.Where(m => m.Tipo == tipo);
            if (categoria != null) filtradas = filtradas.Where(m => m.Categoria == categoria);

            // ordenação (padrão: data crescente)
            bool ordemValida = ordenar != null && CamposOrdenacao.Contains(ordenar);
            string campo = ordemValida ? ordenar! : "data";
            bool desc = ordemValida && Ordenacao.Desc(direcao);

            IOrderedEnumerable<Movimentacao> ordenadas = campo switch
            {
                "descricao" => filtradas.Por(m => m.Descricao, desc),
                "categoria" => filtradas.Por(m => m.Categoria, desc),
                "tipo" => filtradas.Por(m => m.Tipo, desc),
                "valor" => filtradas.Por(m => m.Valor, desc),
                _ => filtradas.Por(m => m.Data, desc)
            };

            return new FluxoCaixaViewModel
            {
                DataInicial = inicio,
                DataFinal = fim,
                // saldo no fim do dia anterior ao início do período (calculado pelo banco)
                SaldoAnterior = MovimentacaoStore.SaldoAte(inicio.AddDays(-1)),
                MovimentacoesPeriodo = periodo,
                Movimentacoes = ordenadas.ThenBy(m => m.Id).ToList(),
                Tipo = tipo,
                Categoria = categoria,
                CategoriasEntrada = new[] { "Vendas" }.Concat(CategoriasEntrada).ToArray(),
                CategoriasSaida = CategoriasSaida,
                Ordenar = campo,
                Direcao = desc ? "desc" : "asc",
                Erro = erro
            };
        }

        public IActionResult FluxoCaixa(
            DateTime? dataInicial, DateTime? dataFinal,
            string? tipo, string? categoria,
            string? ordenar, string? direcao)
        {
            return View(Montar(dataInicial, dataFinal, tipo, categoria, ordenar, direcao));
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

            // grava no banco (procedure sp_RegistrarMovimentacao), em nome do funcionário logado
            try
            {
                int? idFuncionario = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var idLogado)
                    ? idLogado
                    : null;

                MovimentacaoStore.Registrar(new Movimentacao
                {
                    Data = data,
                    Descricao = model.Descricao.Trim(),
                    Tipo = model.Tipo,
                    Categoria = model.Categoria,
                    Valor = valor
                }, idFuncionario);
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError(string.Empty, Db.Mensagem(ex));
                return View(model);
            }

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

        public IActionResult ExportarCsv(DateTime? dataInicial, DateTime? dataFinal, string? tipo, string? categoria)
        {
            var vm = Montar(dataInicial, dataFinal, tipo, categoria, null, null);
            var sb = new StringBuilder("Data;Descricao;Tipo;Categoria;Valor\r\n");
            foreach (var m in vm.Movimentacoes.OrderBy(m => m.Data).ThenBy(m => m.Id))
                sb.Append($"{m.Data:dd/MM/yyyy};{m.Descricao.Replace(";", ",")};{m.Tipo};{m.Categoria};{m.Valor:F2}\r\n");

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", "fluxo-de-caixa.csv");
        }
    }
}