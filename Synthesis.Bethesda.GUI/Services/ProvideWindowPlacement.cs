using System.Drawing;
using System.Windows;

namespace Synthesis.Bethesda.GUI.Services
{
    public interface IProvideWindowPlacement
    {
        Rectangle Rectangle { get; }
    }

    public class ProvideWindowPlacement : IProvideWindowPlacement
    {
        private readonly Window _Window;
        
        public Rectangle Rectangle  => new(
            x: (int)_Window.Left,
            y: (int)_Window.Top,
            width: (int)_Window.Width,
            height: (int)_Window.Height);

        public ProvideWindowPlacement(Window window)
        {
            _Window = window;
        }
    }
}