using System.ComponentModel.DataAnnotations;

namespace ItsFoodBrasil.ViewModels
{
    public class PerfilOpcaoViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
    }

    public class FuncionarioCreateViewModel
    {
        [Required(ErrorMessage = "Informe o nome.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 100 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o e-mail.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        [StringLength(150, ErrorMessage = "O e-mail pode ter no máximo 150 caracteres.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o telefone.")]
        public string Telefone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a senha.")]
        [StringLength(50, MinimumLength = 6, ErrorMessage = "A senha deve ter pelo menos 6 caracteres.")]
        [DataType(DataType.Password)]
        public string Senha { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme a senha.")]
        [Compare(nameof(Senha), ErrorMessage = "As senhas não conferem.")]
        [DataType(DataType.Password)]
        public string ConfirmarSenha { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Escolha o perfil.")]
        public int IdPerfil { get; set; } = 2; // Funcionário

        public bool Ativo { get; set; } = true;

        public List<PerfilOpcaoViewModel> Perfis { get; set; } = new();
    }
}