using Voxta.Abstractions.Management;
using Voxta.Host.AspNetCore.WebSockets;
using Voxta.Server.BackgroundServices;
using Voxta.Server.Filters;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Voxta.Server;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllersWithViews().PartManager.ApplicationParts.Add(new AssemblyPart(typeof(WebSocketsController).Assembly));
        services.AddVoxtaServer();

        #warning Cleanup to allow using outside
        services.AddSingleton<TemporaryFileCleanupService>();
        services.AddSingleton<ITemporaryFileCleanup>(sp => sp.GetRequiredService<TemporaryFileCleanupService>());
        services.AddHostedService(sp => sp.GetRequiredService<TemporaryFileCleanupService>());
        
        services.AddTransient<IStartupFilter, AutoRequestServicesStartupFilter>();
    }

    public void Configure(IApplicationBuilder  app, IWebHostEnvironment env)
    {
        if (!env.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseWebSockets();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}