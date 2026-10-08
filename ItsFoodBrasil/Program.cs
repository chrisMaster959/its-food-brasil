using System.Globalization;
using ItsFoodBrasil.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
// Entity Framework Core 8 + SQL Server
// ======================================================

var connectionString =
builder.Configuration.GetConnectionString("DefaultConnection")
?? throw new InvalidOperationException(
"A conexão 'DefaultConnection' não foi configurada no appsettings.json.");

builder.Services.AddDbContext<AppDbContext>(options =>
options.UseSqlServer(connectionString));

// ======================================================
// 3. AUTENTICAÇÃO E AUTORIZAÇÃO
// ASP.NET Core Identity
// ======================================================

builder.Services
.AddIdentity<IdentityUser, IdentityRole>(options =>
{
// Cada usuário deve possuir um e-mail único.
options.User.RequireUniqueEmail = true;


    // Regras para criação de senhas.
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;

    // Bloqueio temporário após tentativas inválidas.
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan =
        TimeSpan.FromMinutes(5);
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();


// ======================================================
// 4. CONFIGURAÇÃO DO COOKIE DE AUTENTICAÇÃO
// ======================================================

builder.Services.ConfigureApplicationCookie(options =>
{
options.LoginPath = "/Account/Login";
options.AccessDeniedPath = "/Account/AccessDenied";

options.Cookie.Name = "ItsFoodBrasil.Auth";
options.Cookie.HttpOnly = true;
options.Cookie.IsEssential = true;

options.ExpireTimeSpan = TimeSpan.FromHours(8);
options.SlidingExpiration = true;


});

// ======================================================
// 5. MVC
// ======================================================

builder.Services.AddControllersWithViews();

// ======================================================
// 6. CONFIGURAÇÃO DA APLICAÇÃO
// ======================================================

var app = builder.Build();

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
