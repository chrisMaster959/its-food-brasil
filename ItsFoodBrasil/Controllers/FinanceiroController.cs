using Microsoft.AspNetCore.Mvc;
using ItsFoodBrasil.ViewModels;
using ItsFoodBrasil.Models;

namespace ItsFoodBrasil.Controllers
{
    public class FinanceiroController : Controller
    {
        public IActionResult FluxoCaixa(
            DateTime? dataInicial,
            DateTime? dataFinal)
        {
            DateTime inicio = dataInicial ?? new DateTime(2024, 5, 1);
            DateTime fim = dataFinal ?? new DateTime(2024, 5, 31);

            var movimentacoes = new List<Movimentacao>
            {
                new Movimentacao
                {
                    Descricao = "Venda de produto",
                    Tipo = "Entrada",
                    Valor = 150,
                    Data = DateTime.Now
                },

                new Movimentacao
                {
                    Descricao = "Compra de ingredientes",
                    Tipo = "Saída",
                    Valor = 50,
                    Data = DateTime.Now
                }
            };

            var viewModel = new FluxoCaixaViewModel
            {
                DataInicial = inicio,
                DataFinal = fim,
                SaldoAnterior = 0,
                Movimentacoes = movimentacoes
            };

            return View(viewModel);
        }
    }
}