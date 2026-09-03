using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ItsFoodBrasil.Models
{
    public class Movimentacao
    {
        public int Id { get; set; }

        [Required]
        public DateTime Data { get; set; }

        [Required]
        [StringLength(200)]
        public string Descricao { get; set; } = string.Empty;

        [Required]
        public string Tipo { get; set; } = string.Empty;
        // Entrada ou Saída

        [Required]
        public string Categoria { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Valor { get; set; }
    }
}