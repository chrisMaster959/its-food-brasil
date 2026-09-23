using Microsoft.AspNetCore.Mvc;
using ItsFoodBrasil.Models;

namespace ItsFoodBrasil.Controllers
{
    public class ContaController : Controller
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
        public IActionResult Create(Conta model)
        {
            return RedirectToAction("Index");
        }

        public IActionResult Details(int id)
        {
            return View();
        }
    }
}