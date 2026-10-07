using Microsoft.AspNetCore.Mvc;
using ItsFoodBrasil.Models;

namespace ItsFoodBrasil.Controllers
{
    public class PerfilController : Controller
    {

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(Perfil model)
        {
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            return View();
        }

        [HttpPost]
        public ActionResult Edit(Perfil model)
        {
            return RedirectToAction("Index");
        }

        public ActionResult Details(int id)
        {
            return View();
        }


        [HttpGet]
        public ActionResult Delete(int id)
        {
            return View();
        }

        [HttpPost]
        public ActionResult DeleteConfirmed(int id)
        {
            return RedirectToAction("Index");
        }
    }
}