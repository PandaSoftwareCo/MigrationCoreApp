using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MigrationCoreApp.Interfaces;
using MigrationCoreApp.Models;
using MigrationCoreApp.ViewModels;

namespace MigrationCoreApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public IConfigurationRoot Configuration { get; }
        public IServiceProvider ServiceProvider { get; }

        public App()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<App>();
            Configuration = builder.Build();

            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            services.AddLogging(builder =>
            {
                builder.AddConfiguration(Configuration);
                builder.AddDebug();
            });

            services.AddSingleton<IConfigurationRoot>(provider => Configuration);
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
            services.AddSingleton<IAppSettings>(sp =>
                sp.GetRequiredService<IOptions<AppSettings>>().Value);

            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            ServiceProvider.GetService<MainWindow>()?.Show();
        }
    }
}
