using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ItsFoodBrasil.Data;
using ItsFoodBrasil.ViewModels;

namespace ItsFoodBrasil.Controllers
{
    public class FinanceiroController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FinanceiroController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> FluxoCaixa(
            DateTime? dataInicial,
            DateTime? dataFinal)
        {
            DateTime inicio = dataInicial ?? new DateTime(2024, 5, 1);
            DateTime fim = dataFinal ?? new DateTime(2024, 5, 31);

            var movimentacoes = await _context.Movimentacoes
                .Where(m => m.Data >= inicio && m.Data <= fim)
                .OrderByDescending(m => m.Data)
                .ToListAsync();

            decimal entradasAntes = await _context.Movimentacoes
                .Where(m => m.Data < inicio && m.Tipo == "Entrada")
                .SumAsync(m => (decimal?)m.Valor) ?? 0;

            decimal saidasAntes = await _context.Movimentacoes
                .Where(m => m.Data < inicio && m.Tipo == "Saída")
                .SumAsync(m => (decimal?)m.Valor) ?? 0;

            var viewModel = new FluxoCaixaViewModel
            {
                DataInicial = inicio,
                DataFinal = fim,
                SaldoAnterior = entradasAntes - saidasAntes,
                Movimentacoes = movimentacoes
            };

            return View(viewModel);
        }
    }
}