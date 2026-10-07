using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ItsFoodBrasil.Models;
using ItsFoodBrasil.ViewModels;

namespace ItsFoodBrasil.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        // DADOS DE EXEMPLO. Troque por consultas ao banco quando existir.
        var hoje = DateTime.Today;
        var rnd = new Random(42);

        var vendas = Enumerable.Range(0, 30)
            .Select(i => new VendaDiaItem { Data = hoje.AddDays(i - 29), Total = rnd.Next(3000, 9500) })
            .ToList();

        var vm = new DashboardViewModel
        {
            VendasHoje = vendas.Last().Total,
            VendasMes = vendas
                .Where(v => v.Data.Year == hoje.Year && v.Data.Month == hoje.Month)
                .Sum(v => v.Total),
            SaldoCaixa = 57680m,
            ContasPagarProximos7Dias = 18350m,
            QtdContasPagar7Dias = 3,
            VendasUltimos30Dias = vendas,

            ProdutosValidadeProxima = new()
            {
                new() { Nome = "Bacon em manta 1kg",     Lote = "L2409E", Validade = hoje.AddDays(-3), Quantidade = 30 },
                new() { Nome = "Linguiça toscana 1kg",   Lote = "L2408F", Validade = hoje.AddDays(4),  Quantidade = 8 },
                new() { Nome = "Mortadela defumada 1kg", Lote = "L2409C", Validade = hoje.AddDays(9),  Quantidade = 62 },
            },

            ProdutosEstoqueBaixo = new()
            {
                new() { Nome = "Linguiça toscana 1kg",    Quantidade = 8 },
                new() { Nome = "Apresuntado 1kg",         Quantidade = 0 },
                new() { Nome = "Salame italiano 300g",    Quantidade = 12 },
            },

            UltimasVendas = new()
            {
                new() { IdVenda = 1048, Data = DateTime.Now.AddMinutes(-25),  Funcionario = "Carlos Silva", Valor = 1280.50m, Status = "Concluída" },
                new() { IdVenda = 1047, Data = DateTime.Now.AddHours(-2),     Funcionario = "Ana Souza",    Valor = 640.00m,  Status = "Concluída" },
                new() { IdVenda = 1046, Data = DateTime.Now.AddHours(-4),     Funcionario = "Carlos Silva", Valor = 2350.90m, Status = "Pendente" },
                new() { IdVenda = 1045, Data = DateTime.Now.AddHours(-26),    Funcionario = "Ana Souza",    Valor = 415.30m,  Status = "Concluída" },
                new() { IdVenda = 1044, Data = DateTime.Now.AddHours(-30),    Funcionario = "Marcos Lima",  Valor = 980.00m,  Status = "Cancelada" },
            }
        };

        return View(vm);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}