using System.Threading.Tasks;
using System.Windows.Input;

namespace MigrationCoreApp.Framework
{
    public interface IAsyncCommand : ICommand
    {
        Task ExecuteAsync(object parameter);
    }
}
