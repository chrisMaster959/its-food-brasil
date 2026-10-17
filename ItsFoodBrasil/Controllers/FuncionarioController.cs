using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ItsFoodBrasil.Data;
using ItsFoodBrasil.Models;
using ItsFoodBrasil.ViewModels;

public class FuncionarioController : Controller{
    private static FuncionarioCreateViewModel PreencherPerfis(FuncionarioCreateViewModel vm)
    {
        vm.Perfis = CadastroStore.OpcoesPerfil();
        return vm;
    }

    private static string FormatarTelefone(string digitos) =>
        digitos.Length == 11
            ? $"({digitos[..2]}) {digitos.Substring(2, 5)}-{digitos[7..]}"
            : $"({digitos[..2]}) {digitos.Substring(2, 4)}-{digitos[6..]}";

    [HttpGet]
    public IActionResult Create()
    {
        return View(PreencherPerfis(new FuncionarioCreateViewModel()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(FuncionarioCreateViewModel model)
    {
        PreencherPerfis(model);

        if (!CadastroStore.Perfis.ContainsKey(model.IdPerfil))
            ModelState.AddModelError(nameof(model.IdPerfil), "Perfil inválido.");

        var email = (model.Email ?? string.Empty).Trim().ToLowerInvariant();

        var digitos = new string((model.Telefone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (!string.IsNullOrEmpty(model.Telefone) && digitos.Length is not (10 or 11))
            ModelState.AddModelError(nameof(model.Telefone), "Informe o telefone com DDD.");

        try
        {
            if (!string.IsNullOrEmpty(email) && CadastroStore.EmailExiste(email))
                ModelState.AddModelError(nameof(model.Email), "Já existe um cadastro com esse e-mail.");

            if (!ModelState.IsValid)
                return View(model);

            // grava Pessoa + Funcionario no banco (a senha vai como HASH, nunca em texto puro)
            CadastroStore.Criar(model.Nome.Trim(), email, FormatarTelefone(digitos),
                                model.Senha, model.IdPerfil, model.Ativo);
        }
        catch (SqlException ex)
        {
            ModelState.AddModelError(string.Empty, Db.Mensagem(ex));
            return View(model);
        }

        TempData["Sucesso"] = $"{model.Nome.Trim()} foi cadastrado(a) com sucesso.";
        return RedirectToAction("Index", "Cadastro");
    }
    // ---------- EDITAR ----------

[HttpGet]
public IActionResult Edit(int id)
{
    var f = CadastroStore.Buscar(id);
    if (f == null) return NotFound();

    return View(new FuncionarioEditViewModel
    {
        Id = f.IdFuncionario,
        Nome = f.Pessoa.Nome,
        Email = f.Pessoa.Email,
        Telefone = f.Pessoa.Telefone,
        IdPerfil = f.IdPerfil,
        Ativo = f.Status == "Ativo",
        Perfis = CadastroStore.OpcoesPerfil()
    });
}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, FuncionarioEditViewModel model)
    {
        var f = CadastroStore.Buscar(id);
        if (f == null) return NotFound();

        model.Id = id;
        model.Perfis = CadastroStore.OpcoesPerfil();

        if (!CadastroStore.Perfis.ContainsKey(model.IdPerfil))
            ModelState.AddModelError(nameof(model.IdPerfil), "Perfil inválido.");

        var email = (model.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(email) && CadastroStore.EmailExiste(email, id))
            ModelState.AddModelError(nameof(model.Email), "Já existe outro cadastro com esse e-mail.");

        var digitos = new string((model.Telefone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (!string.IsNullOrEmpty(model.Telefone) && digitos.Length is not (10 or 11))
            ModelState.AddModelError(nameof(model.Telefone), "Informe o telefone com DDD.");

        // o sistema não pode ficar sem administrador ativo
        if (CadastroStore.UltimoAdminAtivo(f) &&
            (model.IdPerfil != CadastroStore.IdAdministrador || !model.Ativo))
        {
            ModelState.AddModelError(string.Empty,
                "Este é o único administrador ativo: ele precisa continuar como Administrador e ativo.");
        }

        if (!ModelState.IsValid)
            return View(model);

        var nomeNovo = model.Nome.Trim();

        try
        {
            // atualiza Pessoa + Funcionario; a nova senha (se informada) é gravada como HASH
            CadastroStore.Atualizar(f, nomeNovo, email, FormatarTelefone(digitos),
                                    model.IdPerfil, model.Ativo, model.NovaSenha);
        }
        catch (SqlException ex)
        {
            ModelState.AddModelError(string.Empty, Db.Mensagem(ex));
            return View(model);
        }

        TempData["Sucesso"] = $"Cadastro de {nomeNovo} atualizado.";
        return RedirectToAction("Index", "Cadastro");
    }

    // ---------- DESATIVAR / REATIVAR (olho) ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AlternarStatus(int id)
    {
        var f = CadastroStore.Buscar(id);
        if (f == null) return NotFound();

        if (f.Status == "Ativo" && CadastroStore.UltimoAdminAtivo(f))
        {
            TempData["Erro"] = "Não dá para desativar o único administrador ativo.";
            return RedirectToAction("Index", "Cadastro");
        }

        try
        {
            var ficouAtivo = CadastroStore.AlternarStatus(id);

            TempData["Sucesso"] = ficouAtivo
                ? $"{f.Pessoa.Nome} foi reativado(a)."
                : $"{f.Pessoa.Nome} foi desativado(a).";
        }
        catch (SqlException ex)
        {
            TempData["Erro"] = Db.Mensagem(ex);
        }

        return RedirectToAction("Index", "Cadastro");
    }

    // ---------- EXCLUIR ----------

    private static FuncionarioDeleteViewModel MontarDelete(Funcionario f) => new()
    {
        Id = f.IdFuncionario,
        Nome = f.Pessoa.Nome,
        Email = f.Pessoa.Email,
        Perfil = CadastroStore.NomePerfil(f.IdPerfil),
        Ativo = f.Status == "Ativo",
        QtdVendas = CadastroStore.QtdVendas(f),
        UltimoAdmin = CadastroStore.UltimoAdminAtivo(f)
    };

    [HttpGet]
    public IActionResult Delete(int id)
    {
        var f = CadastroStore.Buscar(id);
        if (f == null) return NotFound();

        return View(MontarDelete(f));
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        var f = CadastroStore.Buscar(id);
        if (f == null) return NotFound();

        var vm = MontarDelete(f);

        // revalida no servidor: a tela pode estar desatualizada
        if (!vm.PodeExcluir)
            return View("Delete", vm);

        try
        {
            CadastroStore.Excluir(id);
        }
        catch (SqlException ex)
        {
            TempData["Erro"] = Db.Mensagem(ex);
            return RedirectToAction("Index", "Cadastro");
        }

        TempData["Sucesso"] = $"{vm.Nome} foi excluído(a).";
        return RedirectToAction("Index", "Cadastro");
    }
}
