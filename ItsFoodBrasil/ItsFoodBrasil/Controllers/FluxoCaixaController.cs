using Microsoft.AspNetCore.Mvc;

namespace ItsFoodBrasil.Controllers
{
    // Mantido por compatibilidade: a tela vive em FinanceiroController.
    public class FluxoCaixaController : Controller
    {
        public IActionResult FluxoCaixa(DateTime? dataInicial, DateTime? dataFinal)
        {
            return RedirectToAction("FluxoCaixa", "Financeiro", new { dataInicial, dataFinal });
        }
    }
}
