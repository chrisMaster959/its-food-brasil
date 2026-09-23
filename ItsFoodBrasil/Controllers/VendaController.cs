using Microsoft.AspNetCore.Mvc;
using ItsFoodBrasil.Models;

namespace ItsFoodBrasil.Controllers
{
    public class VendaController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Venda model)
        {
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult AdicionarItem(ItemVenda model)
        {
            return RedirectToAction("Create");
        }

        [HttpPost]
        public IActionResult RemoverItem(int id)
        {
            return RedirectToAction("Create");
        }

        public IActionResult Details(int id)
        {
            return View();
        }
    }
}