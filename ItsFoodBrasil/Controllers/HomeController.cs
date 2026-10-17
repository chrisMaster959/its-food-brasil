using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ItsFoodBrasil.Data;
using ItsFoodBrasil.Models;
using ItsFoodBrasil.ViewModels;

namespace ItsFoodBrasil.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        var hoje = DateTime.Today;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);

        // vendas dos últimos 30 dias (cobre o gráfico e o mês atual); só as concluídas contam como faturamento
        var concluidas = VendaStore.Listar(de: hoje.AddDays(-30), status: "Concluída");

        // 30 dias, incluindo os dias sem venda (total 0)
        var vendasPorDia = Enumerable.Range(0, 30)
            .Select(i => hoje.AddDays(i - 29))
            .Select(dia => new VendaDiaItem
            {
                Data = dia,
                Total = concluidas.Where(v => v.Data.Date == dia).Sum(v => v.Valor)
            })
            .ToList();

        var saidasMes = MovimentacaoStore.Listar(inicioMes, hoje)
            .Where(m => m.Tipo == "Saída")
            .ToList();

        var produtosAtivos = ProdutoStore.Produtos()
            .Where(p => p.Ativo)
            .ToList();

        var vm = new DashboardViewModel
        {
            VendasHoje = concluidas.Where(v => v.Data.Date == hoje).Sum(v => v.Valor),

            VendasMes = concluidas
                .Where(v => v.Data.Year == hoje.Year && v.Data.Month == hoje.Month)
                .Sum(v => v.Valor),

            SaldoCaixa = MovimentacaoStore.SaldoAte(hoje),

            SaidasMes = saidasMes.Sum(m => m.Valor),
            QtdSaidasMes = saidasMes.Count,

            VendasUltimos30Dias = vendasPorDia,

            // mesmos limites da tela de Estoque (15 dias / estoque mínimo do produto)
            ProdutosValidadeProxima = produtosAtivos
                .Where(p => p.QuantidadeEstoque > 0
                         && p.Validade.HasValue
                         && (p.Validade.Value.Date - hoje).Days <= ProdutoListViewModel.DiasValidadeProxima)
                .OrderBy(p => p.Validade)
                .Take(5)
                .Select(p => new AlertaProdutoItem
                {
                    Nome = p.Nome,
                    Validade = p.Validade,
                    Quantidade = p.QuantidadeEstoque
                })
                .ToList(),

            ProdutosEstoqueBaixo = produtosAtivos
                .Where(p => p.QuantidadeEstoque < p.EstoqueMinimo)
                .OrderBy(p => p.QuantidadeEstoque)
                .ThenBy(p => p.Nome)
                .Take(5)
                .Select(p => new AlertaProdutoItem
                {
                    Nome = p.Nome,
                    Quantidade = p.QuantidadeEstoque
                })
                .ToList(),

            UltimasVendas = VendaStore.Ultimas(5)
                .Select(v => new UltimaVendaItem
                {
                    IdVenda = v.IdVenda,
                    Data = v.Data,
                    Funcionario = v.Funcionario,
                    Valor = v.Valor,
                    Status = v.Status
                })
                .ToList()
        };

        return View(vm);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}