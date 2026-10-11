using Microsoft.AspNetCore.Mvc;
using ItsFoodBrasil.Data;
using ItsFoodBrasil.Models;
using ItsFoodBrasil.ViewModels;

public class FuncionarioController : Controller{
    private static FuncionarioCreateViewModel PreencherPerfis(FuncionarioCreateViewModel vm)
    {
        vm.Perfis = CadastroStore.Perfis
            .Select(p => new PerfilOpcaoViewModel { Id = p.Key, Nome = p.Value })
            .ToList();
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
        if (!string.IsNullOrEmpty(email) && CadastroStore.EmailExiste(email))
            ModelState.AddModelError(nameof(model.Email), "Já existe um cadastro com esse e-mail.");

        var digitos = new string((model.Telefone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (!string.IsNullOrEmpty(model.Telefone) && digitos.Length is not (10 or 11))
            ModelState.AddModelError(nameof(model.Telefone), "Informe o telefone com DDD.");

        if (!ModelState.IsValid)
            return View(model);

        var id = CadastroStore.ProximoId();

        // TODO: ao ligar o banco, gravar Pessoa + Funcionario numa transação e
        // guardar a senha como HASH (ex.: PasswordHasher), nunca em texto puro.
        CadastroStore.Funcionarios.Add(new Funcionario
        {
            IdFuncionario = id,
            IdPessoa = id,
            IdPerfil = model.IdPerfil,
            Status = model.Ativo ? "Ativo" : "Inativo",
            Pessoa = new Pessoa
            {
                IdPessoa = id,
                Nome = model.Nome.Trim(),
                Email = email,
                Telefone = FormatarTelefone(digitos),
                Senha = model.Senha
            }
        });

        TempData["Sucesso"] = $"{model.Nome.Trim()} foi cadastrado(a) com sucesso.";
        return RedirectToAction("Index", "Cadastro");
    }
    // ---------- EDITAR ----------

[HttpGet]
public IActionResult Edit(int id)
{
    var f = CadastroStore.Funcionarios.FirstOrDefault(x => x.IdFuncionario == id);
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
        var f = CadastroStore.Funcionarios.FirstOrDefault(x => x.IdFuncionario == id);
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

        var nomeAntigo = f.Pessoa.Nome;
        var nomeNovo = model.Nome.Trim();

        f.IdPerfil = model.IdPerfil;
        f.Status = model.Ativo ? "Ativo" : "Inativo";
        f.Pessoa.Nome = nomeNovo;
        f.Pessoa.Email = email;
        f.Pessoa.Telefone = FormatarTelefone(digitos);

        // TODO: ao ligar o banco, a senha vira HASH (ex.: PasswordHasher)
        if (!string.IsNullOrEmpty(model.NovaSenha))
            f.Pessoa.Senha = model.NovaSenha;

        // enquanto a venda guarda o nome (e não a FK), acompanha a troca de nome
        if (!nomeAntigo.Equals(nomeNovo, StringComparison.Ordinal))
        {
            foreach (var v in VendaStore.Vendas.Where(v => v.Funcionario == nomeAntigo))
                v.Funcionario = nomeNovo;
        }

        TempData["Sucesso"] = $"Cadastro de {nomeNovo} atualizado.";
        return RedirectToAction("Index", "Cadastro");
    }

    // ---------- DESATIVAR / REATIVAR (olho) ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AlternarStatus(int id)
    {
        var f = CadastroStore.Funcionarios.FirstOrDefault(x => x.IdFuncionario == id);
        if (f == null) return NotFound();

        if (f.Status == "Ativo")
        {
            if (CadastroStore.UltimoAdminAtivo(f))
            {
                TempData["Erro"] = "Não dá para desativar o único administrador ativo.";
                return RedirectToAction("Index", "Cadastro");
            }

            f.Status = "Inativo";
            TempData["Sucesso"] = $"{f.Pessoa.Nome} foi desativado(a).";
        }
        else
        {
            f.Status = "Ativo";
            TempData["Sucesso"] = $"{f.Pessoa.Nome} foi reativado(a).";
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
        var f = CadastroStore.Funcionarios.FirstOrDefault(x => x.IdFuncionario == id);
        if (f == null) return NotFound();

        return View(MontarDelete(f));
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        var f = CadastroStore.Funcionarios.FirstOrDefault(x => x.IdFuncionario == id);
        if (f == null) return NotFound();

        var vm = MontarDelete(f);

        // revalida no servidor: a tela pode estar desatualizada
        if (!vm.PodeExcluir)
            return View("Delete", vm);

        CadastroStore.Funcionarios.Remove(f);

        TempData["Sucesso"] = $"{vm.Nome} foi excluído(a).";
        return RedirectToAction("Index", "Cadastro");
    }
}