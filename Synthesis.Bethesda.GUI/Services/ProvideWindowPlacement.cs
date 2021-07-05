using System.Drawing;
using System.Windows;
using Synthesis.Bethesda.GUI.Views;

namespace Synthesis.Bethesda.GUI.Services
{
    public interface IProvideWindowPlacement
    {
        Rectangle Rectangle { get; }
    }

    public class ProvideWindowPlacement : IProvideWindowPlacement
    {
        private readonly IMainWindow _Window;
        
        public Rectangle Rectangle  => new(
            x: (int)_Window.Left,
            y: (int)_Window.Top,
            width: (int)_Window.Width,
            height: (int)_Window.Height);

        public ProvideWindowPlacement(IMainWindow window)
        {
            _Window = window;
        }
    }
}