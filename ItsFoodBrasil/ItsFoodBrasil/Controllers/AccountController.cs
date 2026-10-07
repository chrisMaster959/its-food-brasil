using Microsoft.AspNetCore.Mvc;
using ItsFoodBrasil.ViewModels;

namespace ItsFoodBrasil.Controllers
{
    public class AccountController : Controller
    {
        //Joga para a tela de login
        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            //Se o email/senha nao forem validos, permanece na view de login
            if (!ModelState.IsValid)
                return View(model);

            //Se o email tiver valido, lança para a tela principal
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult Logout()
        {
            // TODO: encerrar a sessão/cookie de autenticação.
            return RedirectToAction("Login");
        }
    }
}