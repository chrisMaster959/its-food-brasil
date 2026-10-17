using Microsoft.AspNetCore.Identity;
using ItsFoodBrasil.Models;

namespace ItsFoodBrasil.Helpers
{
    // A senha nunca é gravada em texto puro: vai para o banco como HASH.
    public static class Senha
    {
        private static readonly PasswordHasher<Pessoa> Hasher = new();

        public static string Gerar(string senha) =>
            Hasher.HashPassword(new Pessoa(), senha);

        public static bool Conferir(string hash, string senha)
        {
            if (string.IsNullOrEmpty(hash)) return false;

            return Hasher.VerifyHashedPassword(new Pessoa(), hash, senha)
                != PasswordVerificationResult.Failed;
        }
    }
}
