using Noggog;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Mutagen.Bethesda.Synthesis
{
    class ConsoleThrottler : IAsyncDisposable
    {
        private StringBuilder _existingBuilder = new StringBuilder();
        private TextWriter _stdOut;
        private bool _stop;
        private TaskCompletionSource _stopped = new TaskCompletionSource();

        public ConsoleThrottler()
        {
            _stdOut = Console.Out;
            Console.SetOut(new StringWriter(_existingBuilder));
            Task.Run(async () =>
            {
                while (!_stop)
                {
                    await Task.Delay(100);
                    if (_stop) return;
                    Flush(final: false);
                }
            }).FireAndForget((ex) =>
            {
                _stdOut.WriteLine($"Error throttling console output: {ex}");
            });
        }

        public async ValueTask DisposeAsync()
        {
            _stop = true;
            Flush(final: true);
            await _stopped.Task;
            Console.SetOut(_stdOut);
        }

        private void Flush(bool final)
        {
            var curStrings = _existingBuilder;
            _existingBuilder = new StringBuilder();
            Console.SetOut(new StringWriter(_existingBuilder));
            _stdOut.Write(curStrings);
            if (final)
            {
                _stopped.SetResult();
            }
        }
    }
}
