using System.Windows.Input;

namespace Boxer.Core
{
    public interface ISmartCommand : ICommand
    {
        void RaiseCanExecuteChanged();
    }
}