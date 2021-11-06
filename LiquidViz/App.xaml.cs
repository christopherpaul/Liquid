using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace LiquidViz
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application
  {
    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);

      var serviceCollection = new ServiceCollection();
      ConfigureServices(serviceCollection);
      var serviceProvider = serviceCollection.BuildServiceProvider();

      MainWindow = new MainWindow();
      MainWindow.DataContext = serviceProvider.GetRequiredService<IMainViewModel>();
      MainWindow.Show();
    }

    private void ConfigureServices(ServiceCollection serviceCollection)
    {
      AddViewModelsByConvention(serviceCollection);
    }

    private void AddViewModelsByConvention(ServiceCollection serviceCollection)
    {
      var assemblies = new[] { Assembly.GetExecutingAssembly() };
      var viewModelsToAdd = assemblies
        .SelectMany(a => a.GetTypes())
        .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("ViewModel", StringComparison.Ordinal))
        .Select(t => (Implementation: t, Service: t.GetInterfaces().FirstOrDefault(i => i.Name.Equals("I" + t.Name, StringComparison.Ordinal))))
        .Where(x => x.Service != null);

      foreach (var x in viewModelsToAdd)
      {
        serviceCollection.AddTransient(x.Service, x.Implementation);
      }
    }
  }
}
