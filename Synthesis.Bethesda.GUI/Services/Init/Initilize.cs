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
        private readonly DependencyMetadata<ISettings> _Settings;

        public Initilize(
            IClearLoading loading,
            DependencyMetadata<ISettings> settings)
        {
            _Loading = loading;
            _Settings = settings;
        }
        
        public async Task Initialize()
        {
            await Task.WhenAll(
                Task.Run(() =>
                {
                    _Loading.Do();
                }),
                Task.Run(() =>
                {
                    _Settings.GetInstance();
                })).ConfigureAwait(false);
        }
    }
}