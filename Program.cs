using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using LifeHacks;
using LifeHacks.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Serviços de persistência, lógica de negócio e notificações
builder.Services.AddScoped<IStorageService, LocalStorageService>();
builder.Services.AddScoped<RoutineManagerService>();
builder.Services.AddScoped<NotificationService>();

await builder.Build().RunAsync();
