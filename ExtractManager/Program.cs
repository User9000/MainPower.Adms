using CommandLine;
using log4net;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MainPower.Adms.ExtractManager
{
    class Program
    {
        private static readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private enum ExitCodes
        {
            Success = 0,
            NoJob = 1,
            ExtractError = 2,
            EnricherError = 3,
            TimedOut = 4,
            OtherError = 5,
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
               .WithParsed<Options>(o => RunOptionsAndReturnExitCode(o))
               .WithNotParsed<Options>((errs) => HandleParseError(errs));
        }

        static int RunOptionsAndReturnExitCode(Options o)
        {

            try
            {
                Level loglevel = Level.Info;
                switch (o.Debug)
                {
                    case 1:
                        loglevel = Level.Error;
                        break;
                    case 2:
                        loglevel = Level.Warn;
                        break;
                    case 3:
                        loglevel = Level.Info;
                        break;
                    case 4:
                        loglevel = Level.Debug;
                        break;
                }
                Logger.Setup(loglevel);
                var settings = Util.DeserializeNewtonsoft<Settings>("settings.json") ?? new Settings();

                //this is a global timeout - anything still going when this expires will be cancelled
                var timeout = Task.Delay(o.Timeout * 1000 * 60);//timeout option is in minutes
                var extractor = new Extractor(settings);
                var source = new CancellationTokenSource();
                var token = source.Token;
                string id = DateTime.Now.ToString("yyyyMMdd_HHmm");
                var extractorOutputPath = "";
                var enricherOutputPath = "";
                ExtractType etype = ExtractType.Full;

                Task extract = null;
                int cTask;
                if (o.SuccessfulImport)
                {
                    extract = extractor.Complete();
                    cTask = Task.WaitAny(timeout, extract);
                    if (cTask == 0)
                    {
                        source.Cancel();
                        return (int)ExitCodes.TimedOut;
                    }
                    else
                    {
                        settings.EnricherReferenceModel = settings.EnricherLatestModel;
                        settings.EnricherLatestModel = "";
                        Util.SerializeNewtonsoft("settings.json", settings);
                        return (int)ExitCodes.Success;
                    }
                }
                else if (o.CompleteExtract)
                {
                    etype = ExtractType.Full;
                    extract = extractor.RequestExtract(token, id, etype);
                }
                else if (o.IncrementalExtract)
                {
                    etype = ExtractType.Incremental;
                    extract = extractor.RequestExtract(token, id, etype);
                }
                else
                {
                    _log.Warn("Nothing to do... exiting");
                    return (int)ExitCodes.NoJob;
                }
                //if we get this far then there should be an extract running, and we are waiting for the job to complete

                cTask = Task.WaitAny(timeout, extract);
                if (cTask == 0)
                {
                    source.Cancel();
                    return (int)ExitCodes.TimedOut;
                }
                else
                {
                    //time to invoke the enricher
                    Enricher enricher = new Enricher(settings);
                    var eTask = enricher.RunEnricher(token, extractorOutputPath, enricherOutputPath, etype, settings.EnricherReferenceModel);
                    cTask = Task.WaitAny(timeout, eTask);
                    if (cTask == 0)
                    {
                        source.Cancel();
                        return (int)ExitCodes.TimedOut;
                    }
                    else
                    {
                        if (eTask.Result.Result == 0)
                        {
                            settings.EnricherLatestModel = Path.Combine(enricherOutputPath, "model");
                            Util.SerializeNewtonsoft("settings.json", settings);

                            return (int)ExitCodes.Success;
                        }
                        else
                        {
                            return (int)(ExitCodes.EnricherError);
                        }
                    }
                }
            }
            catch (EnricherException ex)
            {
                _log.Error(ex.Message);
                return (int)ExitCodes.EnricherError;
            }
            catch (ExtractorException ex)
            {
                _log.Error(ex.Message);
                return (int)ExitCodes.ExtractError;
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                return (int)ExitCodes.OtherError;
            }
        }

        static void HandleParseError(IEnumerable<Error> errs)
        {

        }
    }
}
