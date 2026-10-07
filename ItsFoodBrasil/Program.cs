using System.Globalization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Formato brasileiro (R$ 1.234,56 / dd/MM/yyyy)
var pt = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = pt;
CultureInfo.DefaultThreadCurrentUICulture = pt;

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
