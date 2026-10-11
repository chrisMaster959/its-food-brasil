using System.ComponentModel.DataAnnotations;

namespace ItsFoodBrasil.ViewModels
{
    public class FuncionarioEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe o nome.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 100 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o e-mail.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        [StringLength(150, ErrorMessage = "O e-mail pode ter no máximo 150 caracteres.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o telefone.")]
        public string Telefone { get; set; } = string.Empty;

        // opcional: em branco = mantém a senha atual
        [StringLength(50, MinimumLength = 6, ErrorMessage = "A nova senha deve ter pelo menos 6 caracteres.")]
        [DataType(DataType.Password)]
        public string? NovaSenha { get; set; }

        [Compare(nameof(NovaSenha), ErrorMessage = "As senhas não conferem.")]
        [DataType(DataType.Password)]
        public string? ConfirmarNovaSenha { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Escolha o perfil.")]
        public int IdPerfil { get; set; }

        public bool Ativo { get; set; } = true;

        public List<PerfilOpcaoViewModel> Perfis { get; set; } = new();
    }

    public class FuncionarioDeleteViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Perfil { get; set; } = string.Empty;
        public bool Ativo { get; set; }
        public int QtdVendas { get; set; }
        public bool UltimoAdmin { get; set; }

        public bool PodeExcluir => QtdVendas == 0 && !UltimoAdmin;
    }
}