using Microsoft.EntityFrameworkCore;
using ItsFoodBrasil.Models;

namespace ItsFoodBrasil.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Movimentacao> Movimentacoes { get; set; }
    }
}