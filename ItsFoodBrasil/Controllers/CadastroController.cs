using Microsoft.AspNetCore.Mvc;
using ItsFoodBrasil.ViewModels;
public class CadastroController : Controller
{
    [HttpGet]
    public IActionResult Index(
        string? busca,
        string? tipo,
        string? situacao)
    {
        // ============================================================
        // DADOS FICTÍCIOS
        // ============================================================

        var cadastros = new List<CadastroItemViewModel>
        {
            new CadastroItemViewModel
            {
                Id = 1,
                Nome = "João da Silva",
                Documento = "123.456.789-00",
                Tipo = "Funcionário",
                Usuario = "joao.silva",
                Perfil = "Administrador",
                Ativo = true,

                DetalhesUrl = Url.Action(
                    "Details",
                    "Funcionario",
                    new { id = 1 }) ?? "#",

                EditarUrl = Url.Action(
                    "Edit",
                    "Funcionario",
                    new { id = 1 }) ?? "#",

                ExcluirUrl = Url.Action(
                    "Delete",
                    "Funcionario",
                    new { id = 1 }) ?? "#"
            },

            new CadastroItemViewModel
            {
                Id = 2,
                Nome = "Maria Oliveira",
                Documento = "987.654.321-00",
                Tipo = "Funcionário",
                Usuario = "maria.oliveira",
                Perfil = "Vendedor",
                Ativo = true,

                DetalhesUrl = Url.Action(
                    "Details",
                    "Funcionario",
                    new { id = 2 }) ?? "#",

                EditarUrl = Url.Action(
                    "Edit",
                    "Funcionario",
                    new { id = 2 }) ?? "#",

                ExcluirUrl = Url.Action(
                    "Delete",
                    "Funcionario",
                    new { id = 2 }) ?? "#"
            },

            new CadastroItemViewModel
            {
                Id = 3,
                Nome = "Carlos Souza",
                Documento = "456.789.123-00",
                Tipo = "Funcionário",
                Usuario = "carlos.souza",
                Perfil = "Financeiro",
                Ativo = true,

                DetalhesUrl = Url.Action(
                    "Details",
                    "Funcionario",
                    new { id = 3 }) ?? "#",

                EditarUrl = Url.Action(
                    "Edit",
                    "Funcionario",
                    new { id = 3 }) ?? "#",

                ExcluirUrl = Url.Action(
                    "Delete",
                    "Funcionario",
                    new { id = 3 }) ?? "#"
            },

            new CadastroItemViewModel
            {
                Id = 4,
                Nome = "Ana Pereira",
                Documento = "321.654.987-00",
                Tipo = "Funcionário",
                Usuario = "ana.pereira",
                Perfil = "Vendedor",
                Ativo = false,

                DetalhesUrl = Url.Action(
                    "Details",
                    "Funcionario",
                    new { id = 4 }) ?? "#",

                EditarUrl = Url.Action(
                    "Edit",
                    "Funcionario",
                    new { id = 4 }) ?? "#",

                ExcluirUrl = Url.Action(
                    "Delete",
                    "Funcionario",
                    new { id = 4 }) ?? "#"
            },

            new CadastroItemViewModel
            {
                Id = 5,
                Nome = "Administrador",
                Documento = null,
                Tipo = "Perfil",
                Usuario = null,
                Perfil = "Administrador",
                Ativo = true,

                DetalhesUrl = Url.Action(
                    "Details",
                    "Perfil",
                    new { id = 1 }) ?? "#",

                EditarUrl = Url.Action(
                    "Edit",
                    "Perfil",
                    new { id = 1 }) ?? "#",

                ExcluirUrl = Url.Action(
                    "Delete",
                    "Perfil",
                    new { id = 1 }) ?? "#"
            },

            new CadastroItemViewModel
            {
                Id = 6,
                Nome = "Vendedor",
                Documento = null,
                Tipo = "Perfil",
                Usuario = null,
                Perfil = "Vendedor",
                Ativo = true,

                DetalhesUrl = Url.Action(
                    "Details",
                    "Perfil",
                    new { id = 2 }) ?? "#",

                EditarUrl = Url.Action(
                    "Edit",
                    "Perfil",
                    new { id = 2 }) ?? "#",

                ExcluirUrl = Url.Action(
                    "Delete",
                    "Perfil",
                    new { id = 2 }) ?? "#"
            },

            new CadastroItemViewModel
            {
                Id = 7,
                Nome = "Financeiro",
                Documento = null,
                Tipo = "Perfil",
                Usuario = null,
                Perfil = "Financeiro",
                Ativo = true,

                DetalhesUrl = Url.Action(
                    "Details",
                    "Perfil",
                    new { id = 3 }) ?? "#",

                EditarUrl = Url.Action(
                    "Edit",
                    "Perfil",
                    new { id = 3 }) ?? "#",

                ExcluirUrl = Url.Action(
                    "Delete",
                    "Perfil",
                    new { id = 3 }) ?? "#"
            },

            new CadastroItemViewModel
            {
                Id = 8,
                Nome = "Perfil Antigo",
                Documento = null,
                Tipo = "Perfil",
                Usuario = null,
                Perfil = "Perfil Antigo",
                Ativo = false,

                DetalhesUrl = Url.Action(
                    "Details",
                    "Perfil",
                    new { id = 4 }) ?? "#",

                EditarUrl = Url.Action(
                    "Edit",
                    "Perfil",
                    new { id = 4 }) ?? "#",

                ExcluirUrl = Url.Action(
                    "Delete",
                    "Perfil",
                    new { id = 4 }) ?? "#"
            }
        };

        // ============================================================
        // FILTRO DE BUSCA
        // ============================================================

        if (!string.IsNullOrWhiteSpace(busca))
        {
            busca = busca.Trim();

            cadastros = cadastros
                .Where(x =>
                    x.Nome.Contains(
                        busca,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    (!string.IsNullOrWhiteSpace(x.Documento) &&
                     x.Documento.Contains(
                        busca,
                        StringComparison.OrdinalIgnoreCase))
                    ||
                    (!string.IsNullOrWhiteSpace(x.Usuario) &&
                     x.Usuario.Contains(
                        busca,
                        StringComparison.OrdinalIgnoreCase))
                )
                .ToList();
        }

        // ============================================================
        // FILTRO POR TIPO
        // ============================================================

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            if (tipo.Equals(
                "funcionario",
                StringComparison.OrdinalIgnoreCase))
            {
                cadastros = cadastros
                    .Where(x => x.Tipo == "Funcionário")
                    .ToList();
            }

            if (tipo.Equals(
                "perfil",
                StringComparison.OrdinalIgnoreCase))
            {
                cadastros = cadastros
                    .Where(x => x.Tipo == "Perfil")
                    .ToList();
            }
        }

        // ============================================================
        // FILTRO POR SITUAÇÃO
        // ============================================================

        if (!string.IsNullOrWhiteSpace(situacao))
        {
            if (situacao.Equals(
                "ativo",
                StringComparison.OrdinalIgnoreCase))
            {
                cadastros = cadastros
                    .Where(x => x.Ativo)
                    .ToList();
            }

            if (situacao.Equals(
                "inativo",
                StringComparison.OrdinalIgnoreCase))
            {
                cadastros = cadastros
                    .Where(x => !x.Ativo)
                    .ToList();
            }
        }

        // ============================================================
        // ESTATÍSTICAS
        // ============================================================

        // Como os filtros alteram a tabela, as estatísticas são
        // calculadas a partir da lista original.

        var todos = new List<CadastroItemViewModel>
        {
            new CadastroItemViewModel
            {
                Tipo = "Funcionário",
                Ativo = true
            },
            new CadastroItemViewModel
            {
                Tipo = "Funcionário",
                Ativo = true
            },
            new CadastroItemViewModel
            {
                Tipo = "Funcionário",
                Ativo = true
            },
            new CadastroItemViewModel
            {
                Tipo = "Funcionário",
                Ativo = false
            },
            new CadastroItemViewModel
            {
                Tipo = "Perfil",
                Ativo = true
            },
            new CadastroItemViewModel
            {
                Tipo = "Perfil",
                Ativo = true
            },
            new CadastroItemViewModel
            {
                Tipo = "Perfil",
                Ativo = true
            },
            new CadastroItemViewModel
            {
                Tipo = "Perfil",
                Ativo = false
            }
        };

        var totalFuncionarios = todos
            .Count(x => x.Tipo == "Funcionário");

        var funcionariosAtivos = todos
            .Count(x =>
                x.Tipo == "Funcionário" &&
                x.Ativo);

        var funcionariosInativos = todos
            .Count(x =>
                x.Tipo == "Funcionário" &&
                !x.Ativo);

        var totalPerfis = todos
            .Count(x => x.Tipo == "Perfil");

        // ============================================================
        // VIEW MODEL
        // ============================================================

        var model = new CadastroListViewModel
        {
            Busca = busca,

            Tipo = tipo,

            Situacao = situacao,

            TotalFuncionarios = totalFuncionarios,

            FuncionariosAtivos = funcionariosAtivos,

            FuncionariosInativos = funcionariosInativos,

            TotalPerfis = totalPerfis,

            Cadastros = cadastros
        };

        return View(model);
    }
}