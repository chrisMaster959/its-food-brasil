using System.Globalization;
using ItsFoodBrasil.Data;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);
// ======================================================
// 1. CULTURA BRASILEIRA
// Formatos: R$ 1.234,56 / dd/MM/yyyy
// ======================================================

var pt = new CultureInfo("pt-BR");

CultureInfo.DefaultThreadCurrentCulture = pt;
CultureInfo.DefaultThreadCurrentUICulture = pt;

// ======================================================
// 2. CONEXÃO COM O BANCO DE DADOS
// SQL Server via ADO.NET (classes da pasta Data).
// A connection string fica no appsettings.json.
// ======================================================

Db.ConnectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "A conexão 'DefaultConnection' não foi configurada no appsettings.json.");

// ======================================================
// 3. AUTENTICAÇÃO (cookie)
// O login confere e-mail e senha (hash) na tabela Pessoa.
// ======================================================

builder.Services
    .AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";

        options.Cookie.Name = "ItsFoodBrasil.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;

        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// ======================================================
// 4. MVC
// Todas as telas exigem login (exceto as marcadas com [AllowAnonymous]).
// ======================================================

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter());
});

// ======================================================
// 5. CONFIGURAÇÃO DA APLICAÇÃO
// ======================================================

var app = builder.Build();

// Os usuários de exemplo do script SQL não têm senha: define a senha inicial (veja o README).
try
{
    CadastroStore.DefinirSenhaInicial();
}
catch (Exception ex)
{
    app.Logger.LogError(ex,
        "Não foi possível conectar ao banco. Confira a connection string e se o script SQL foi executado.");
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// A autenticação deve vir antes da autorização.
app.UseAuthentication();
app.UseAuthorization();

// Página inicial: login.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
