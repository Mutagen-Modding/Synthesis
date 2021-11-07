using System.Threading.Channels;

namespace Synthesis.Bethesda.Execution.WorkEngine
{
    public interface IWorkQueue
    {
        ChannelReader<IToDo> Reader { get; }
    }
}