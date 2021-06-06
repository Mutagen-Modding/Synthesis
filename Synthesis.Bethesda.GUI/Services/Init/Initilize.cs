using System.Threading.Tasks;
using SimpleInjector;

namespace Synthesis.Bethesda.GUI.Services
{
    public interface IInitilize
    {
        Task Initialize();
    }

    public class Initilize : IInitilize
    {
        private readonly IClearLoading _Loading;

        public Initilize(
            IClearLoading loading)
        {
            _Loading = loading;
        }
        
        public async Task Initialize()
        {
            await Task.Run(() =>
            {
                _Loading.Do();
            }).ConfigureAwait(false);
        }
    }
}