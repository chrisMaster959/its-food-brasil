using Microsoft.AspNetCore.Mvc;
using ItsFoodBrasil.Data;
using ItsFoodBrasil.Helpers;
using ItsFoodBrasil.ViewModels;

public class CadastroController : Controller
{
    private static readonly string[] CamposOrdenacao =
        { "nome", "tipo", "usuario", "perfil", "status" };

    [HttpGet]
    public IActionResult Index(string? busca, string? tipo, string? situacao, string? ordenar, string? direcao)
    {
        // funcionários
        var todos = CadastroStore.Listar()
            .Select(f => new CadastroItemViewModel
            {
                Id = f.IdFuncionario,
                Nome = f.Pessoa.Nome,
                Documento = null,
                Tipo = "Funcionário",
                Usuario = f.Pessoa.Email,
                Perfil = CadastroStore.NomePerfil(f.IdPerfil),
                Ativo = f.Status == "Ativo",
                DetalhesUrl = Url.Action("Details", "Funcionario", new { id = f.IdFuncionario }) ?? "#",
                EditarUrl = Url.Action("Edit", "Funcionario", new { id = f.IdFuncionario }) ?? "#",
                ExcluirUrl = Url.Action("Delete", "Funcionario", new { id = f.IdFuncionario }) ?? "#"
            })
            .ToList();

        // perfis
        todos.AddRange(CadastroStore.Perfis.Select(p => new CadastroItemViewModel
        {
            Id = p.Key,
            Nome = p.Value,
            Documento = null,
            Tipo = "Perfil",
            Usuario = null,
            Perfil = p.Value,
            Ativo = true,
            DetalhesUrl = Url.Action("Details", "Perfil", new { id = p.Key }) ?? "#",
            EditarUrl = Url.Action("Edit", "Perfil", new { id = p.Key }) ?? "#",
            ExcluirUrl = Url.Action("Delete", "Perfil", new { id = p.Key }) ?? "#"
        }));

        // estatísticas (sempre da lista completa, antes dos filtros)
        var totalFuncionarios = todos.Count(x => x.Tipo == "Funcionário");
        var funcionariosAtivos = todos.Count(x => x.Tipo == "Funcionário" && x.Ativo);
        var funcionariosInativos = todos.Count(x => x.Tipo == "Funcionário" && !x.Ativo);
        var totalPerfis = todos.Count(x => x.Tipo == "Perfil");

        // filtros
        IEnumerable<CadastroItemViewModel> cadastros = todos;

        if (!string.IsNullOrWhiteSpace(busca))
        {
            busca = busca.Trim();
            cadastros = cadastros.Where(x =>
                x.Nome.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                (x.Documento ?? "").Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                (x.Usuario ?? "").Contains(busca, StringComparison.OrdinalIgnoreCase));
        }

        if (string.Equals(tipo, "funcionario", StringComparison.OrdinalIgnoreCase))
            cadastros = cadastros.Where(x => x.Tipo == "Funcionário");
        else if (string.Equals(tipo, "perfil", StringComparison.OrdinalIgnoreCase))
            cadastros = cadastros.Where(x => x.Tipo == "Perfil");

        if (string.Equals(situacao, "ativo", StringComparison.OrdinalIgnoreCase))
            cadastros = cadastros.Where(x => x.Ativo);
        else if (string.Equals(situacao, "inativo", StringComparison.OrdinalIgnoreCase))
            cadastros = cadastros.Where(x => !x.Ativo);

        // ordenação (sem escolha: mantém a ordem original)
        bool ordemValida = ordenar != null && CamposOrdenacao.Contains(ordenar);
        bool desc = ordemValida && Ordenacao.Desc(direcao);

        if (ordemValida)
        {
            cadastros = (ordenar switch
            {
                "tipo" => cadastros.Por(x => x.Tipo, desc),
                "usuario" => cadastros.Por(x => x.Usuario ?? string.Empty, desc),
                "perfil" => cadastros.Por(x => x.Perfil ?? string.Empty, desc),
                // crescente: ativos primeiro
                "status" => cadastros.Por(x => x.Ativo ? 0 : 1, desc),
                _ => cadastros.Por(x => x.Nome, desc)
            }).ThenBy(x => x.Nome);
        }

        var model = new CadastroListViewModel
        {
            Busca = busca,
            Tipo = tipo,
            Situacao = situacao,
            Ordenar = ordemValida ? ordenar : null,
            Direcao = ordemValida ? (desc ? "desc" : "asc") : null,
            TotalFuncionarios = totalFuncionarios,
            FuncionariosAtivos = funcionariosAtivos,
            FuncionariosInativos = funcionariosInativos,
            TotalPerfis = totalPerfis,
            Cadastros = cadastros.ToList()
        };

        return View(model);
    }
}