using System.Text;
using Microsoft.AspNetCore.Mvc;
using ItsFoodBrasil.ViewModels;
using ItsFoodBrasil.Models;

namespace ItsFoodBrasil.Controllers
{
    public class FinanceiroController : Controller
    {
        // Saldo de caixa antes da primeira movimentação abaixo.
        private const decimal SaldoInicial = 12450m;

        // DADOS DE EXEMPLO (em ordem cronológica). Troque por consulta ao banco (DbContext) quando existir.
        private static readonly List<Movimentacao> Dados = new()
        {
            Mov(1,  8,  "Recebimento de vendas",       "Entrada", "Vendas",       12000m),
            Mov(2,  10, "Pagamento de aluguel",        "Saída",   "Despesas",      6500m),
            Mov(3,  15, "Recebimento de vendas",       "Entrada", "Vendas",       13250m),
            Mov(4,  18, "Pagamento fornecedor Frimesa","Saída",   "Fornecedores", 10800m),
            Mov(5,  20, "Recebimento de vendas",       "Entrada", "Vendas",       15400m),
            Mov(6,  22, "Pagamento de energia",        "Saída",   "Despesas",      4230m),
            Mov(7,  24, "Recebimento de vendas",       "Entrada", "Vendas",       12880m),
            Mov(8,  25, "Compra de mercadorias",       "Saída",   "Compras",       9500m),
            Mov(9,  26, "Recebimento de vendas",       "Entrada", "Vendas",       14300m),
            Mov(10, 27, "Recebimento de vendas",       "Entrada", "Vendas",        6190m),
            Mov(11, 29, "Compra de mercadorias",       "Saída",   "Compras",       8800m),
            Mov(12, 30, "Recebimento de vendas",       "Entrada", "Vendas",        9760m),
            Mov(13, 30, "Pagamento de frete",          "Saída",   "Despesas",      1250m),
            Mov(14, 30, "Recebimento de vendas",       "Entrada", "Vendas",        6230m),
            Mov(15, 31, "Pagamento fornecedor Frimesa","Saída",   "Fornecedores", 12450m),
            Mov(16, 31, "Recebimento de vendas",       "Entrada", "Vendas",        8750m),
        };

        private static Movimentacao Mov(int id, int dia, string desc, string tipo, string cat, decimal valor) =>
            new() { Id = id, Data = new DateTime(2024, 5, dia), Descricao = desc, Tipo = tipo, Categoria = cat, Valor = valor };

        private static decimal Liquido(IEnumerable<Movimentacao> lista) =>
            lista.Sum(m => m.Tipo == "Entrada" ? m.Valor : -m.Valor);

        private static FluxoCaixaViewModel Montar(DateTime? dataInicial, DateTime? dataFinal)
        {
            DateTime inicio = (dataInicial ?? new DateTime(2024, 5, 1)).Date;
            DateTime fim = (dataFinal ?? new DateTime(2024, 5, 31)).Date;
            if (fim < inicio) (inicio, fim) = (fim, inicio);

            return new FluxoCaixaViewModel
            {
                DataInicial = inicio,
                DataFinal = fim,
                SaldoAnterior = SaldoInicial + Liquido(Dados.Where(m => m.Data.Date < inicio)),
                Movimentacoes = Dados.Where(m => m.Data.Date >= inicio && m.Data.Date <= fim).ToList()
            };
        }

        public IActionResult FluxoCaixa(DateTime? dataInicial, DateTime? dataFinal)
        {
            return View(Montar(dataInicial, dataFinal));
        }

        public IActionResult ExportarCsv(DateTime? dataInicial, DateTime? dataFinal)
        {
            var vm = Montar(dataInicial, dataFinal);
            var sb = new StringBuilder("Data;Descricao;Tipo;Categoria;Valor\r\n");
            foreach (var m in vm.Movimentacoes.OrderBy(m => m.Data).ThenBy(m => m.Id))
                sb.Append($"{m.Data:dd/MM/yyyy};{m.Descricao};{m.Tipo};{m.Categoria};{m.Valor:F2}\r\n");

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", "fluxo-de-caixa.csv");
        }
    }
}
