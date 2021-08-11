using System;
using Noggog;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline.V1;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline.V2;

namespace Synthesis.Bethesda.Execution.Settings.Json.Pipeline
{
    public interface IPipelineSettingsImporter
    {
        IPipelineSettings Import(FilePath path);
    }

    public class PipelineSettingsImporter : IPipelineSettingsImporter
    {
        private readonly IPipelineSettingsV1Reader _v1Reader;
        private readonly IPipelineSettingsV2Reader _v2Reader;
        private readonly IPipelineSettingsUpgrader _upgrader;
        private readonly IPipelineSettingsVersionRetriever _versionRetriever;

        public PipelineSettingsImporter(
            IPipelineSettingsV1Reader v1Reader,
            IPipelineSettingsV2Reader v2Reader,
            IPipelineSettingsUpgrader upgrader,
            IPipelineSettingsVersionRetriever versionRetriever)
        {
            _v1Reader = v1Reader;
            _v2Reader = v2Reader;
            _upgrader = upgrader;
            _versionRetriever = versionRetriever;
        }

        public IPipelineSettings Import(FilePath path)
        {
            object o;
            switch (_versionRetriever.GetVersion(path))
            {
                case 1:
                    o = _v1Reader.Read(path);
                    break;
                case 2:
                    o = _v2Reader.Read(path);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return _upgrader.Upgrade(o);
        }
    }
}