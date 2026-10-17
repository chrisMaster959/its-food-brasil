using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ItsFoodBrasil.Data;
using ItsFoodBrasil.Helpers;
using ItsFoodBrasil.ViewModels;

namespace ItsFoodBrasil.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        //Joga para a tela de login
        [HttpGet]
        public IActionResult Login()
        {
            // quem já está logado vai direto para o Dashboard
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            //Se o email/senha nao forem validos, permanece na view de login
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var email = model.Email.Trim().ToLowerInvariant();
                var funcionario = CadastroStore.BuscarPorEmail(email);

                // mesma mensagem para e-mail inexistente e senha errada (não revela qual dos dois falhou)
                if (funcionario == null || !Senha.Conferir(funcionario.Pessoa.Senha, model.Senha))
                {
                    ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
                    return View(model);
                }

                if (funcionario.Status != "Ativo")
                {
                    ModelState.AddModelError(string.Empty, "Este usuário está inativo. Procure um administrador.");
                    return View(model);
                }

                // dados que ficam no cookie de login
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, funcionario.IdFuncionario.ToString()),
                    new(ClaimTypes.Name, funcionario.Pessoa.Nome),
                    new(ClaimTypes.Email, funcionario.Pessoa.Email),
                    new(ClaimTypes.Role, funcionario.Perfil)
                };

                var identidade = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identidade),
                    new AuthenticationProperties { IsPersistent = model.Lembrar });
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError(string.Empty, Db.Mensagem(ex));
                return View(model);
            }

            //Se o email tiver valido, lança para a tela principal
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
