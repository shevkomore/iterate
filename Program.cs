using ElectronNET.API;
using iterate;
using ElectronNET.API.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseElectron(args);
builder.Services.AddElectron();

var app = builder.Build();

/*app.UseStaticFiles();
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});*/
app.Start();
IterateApplicationContext context = new IterateApplicationContext();
await context.Start();

app.WaitForShutdown();