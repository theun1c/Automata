using System;
using Automata.Desktop.ViewModels;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Automata.Desktop
{
    /// <summary>
    /// Простейший локатор View по соглашению имён:
    /// *ViewModel -> *View.
    /// Нужен, чтобы MainWindow мог подставлять экран через ContentControl.
    /// </summary>
    public class ViewLocator : IDataTemplate
    {

        public Control? Build(object? param)
        {
            if (param is null)
            {
                return null;
            }

            var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
            var type = Type.GetType(name);

            if (type != null)
            {
                return (Control)Activator.CreateInstance(type)!;
            }

            return new TextBlock { Text = "Not Found: " + name };
        }

        public bool Match(object? data)
        {
            return data is ViewModelBase;
        }
    }
}
