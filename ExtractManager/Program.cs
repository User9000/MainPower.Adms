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
        private static readonly string _settingsFileName = "em_settings.json";
        private enum ExitCodes
        {
            Success = 0,
            NoJob = 1,
            ExtractError = 2,
            EnricherError = 3,
            TimedOut = 4,
            OtherError = 5,
            CommandLineParseError = 6,
            SettingsFileError = 7,
        }

        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<Options>(args)
        .MapResult(
          (opts) => RunOptionsAndReturnExitCode(opts),
          errs => HandleParseError(errs));

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
                var settings = Util.DeserializeNewtonsoft<Settings>(_settingsFileName);
                if (settings == null)
                {
                    _log.Error("Problem loading or parsing the settings file");
                    return (int)ExitCodes.SettingsFileError;
                }
                string id = DateTime.Now.ToString("yyyyMMdd_HHmm");

                Logger.Setup(loglevel, settings.LogPath, id);
                
                //this is a global timeout - anything still going when this expires will be cancelled
                var timeout = Task.Delay(settings.Timeout * 1000 * 60);//timeout option is in minutes
                var extractor = new Extractor(settings);
                var source = new CancellationTokenSource();
                var token = source.Token;
                var extractorOutputPath = Path.Combine(settings.IdfFileShare, id);
                var enricherOutputPath = Path.Combine(extractorOutputPath, "enricheroutput");
                ExtractType etype = ExtractType.Full;

                Task extract = null;
                int cTask;
                if (o.SuccessfulImport)
                {
                    extract = extractor.Complete();
                    cTask = Task.WaitAny(timeout, extract);
                    if (cTask == 0)
                    {
                        _log.Error("Timed out waiting for extractor completion");
                        source.Cancel();
                        return (int)ExitCodes.TimedOut;
                    }
                    else
                    {
                        if (extract.Status == TaskStatus.Faulted)
                        {
                            _log.Error(extract.Exception?.Message);
                            _log.Debug(extract.Exception?.ToString());
                            return (int)ExitCodes.ExtractError;
                        }
                        else if (extract.Status == TaskStatus.RanToCompletion)
                        {
                            if (string.IsNullOrWhiteSpace(settings.EnricherLatestModel))
                            {
                                _log.Warn("No newer enricher model available to update, no changes will be made.");
                                return (int)ExitCodes.Success;
                            }
                            else
                            {
                                _log.Info($"Updating enricher reference model from {settings.EnricherReferenceModel} to {settings.EnricherLatestModel}");
                                settings.EnricherReferenceModel = settings.EnricherLatestModel;
                                settings.EnricherLatestModel = "";
                                Util.SerializeNewtonsoft(_settingsFileName, settings);
                                return (int)ExitCodes.Success;
                            }
                        }
                        else
                        {
                            _log.Error($"Extractor task completed in unexpected state {extract.Status}");
                            return (int)ExitCodes.ExtractError;
                        }

                    }
                }
                else if (o.CompleteExtract)
                {
                    etype = ExtractType.Full;
                    if (!string.IsNullOrWhiteSpace(o.Enrich))
                    {
                        id = o.Enrich;
                        extractorOutputPath = Path.Combine(settings.IdfFileShare, id);
                        enricherOutputPath = Path.Combine(extractorOutputPath, "enricheroutput");
                        extract = Task.Delay(1000);
                    }
                    else
                    {
                        extract = extractor.RequestExtract(token, id, etype, extractorOutputPath);
                    }
                }
                else if (o.IncrementalExtract)
                {
                    etype = ExtractType.Incremental;
                    if (!string.IsNullOrWhiteSpace(o.Enrich))
                    {
                        id = o.Enrich;
                        extractorOutputPath = Path.Combine(settings.IdfFileShare, id);
                        enricherOutputPath = Path.Combine(extractorOutputPath, "enricheroutput");
                        extract = Task.Delay(1000);
                    }
                    else
                    {
                        extract = extractor.RequestExtract(token, id, etype, extractorOutputPath);
                    }
                }
                else
                {
                    _log.Warn("Nothing to do, quitting");
                    return (int)ExitCodes.NoJob;
                }
                //if we get this far then there should be an extract running, and we are waiting for the job to complete

                cTask = Task.WaitAny(timeout, extract);
                if (cTask == 0)
                {
                    _log.Info("Timed out waiting for extract, quitting");
                    source.Cancel();
                    return (int)ExitCodes.TimedOut;
                }
                else
                {
                    if (extract.Status == TaskStatus.Faulted)
                    {
                        _log.Error(extract.Exception?.Message);
                        _log.Debug(extract.Exception?.ToString());
                        return (int)ExitCodes.ExtractError;
                    }
                    else if (extract.Status == TaskStatus.RanToCompletion)
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
                            if (eTask.Status == TaskStatus.Faulted)
                            {
                                _log.Error(extract.Exception?.Message);
                                _log.Debug(extract.Exception?.ToString());
                                return (int)ExitCodes.EnricherError;
                            }
                            else if (eTask.Status == TaskStatus.RanToCompletion)
                            {
                                if (eTask.Result.Success)
                                {
                                    _log.Info(eTask.Result.ResultMessage);
                                    settings.EnricherLatestModel = Path.Combine(enricherOutputPath, "model");
                                    Util.SerializeNewtonsoft(_settingsFileName, settings);

                                    //TODO: should we clear out this directory first?
                                    _log.Info($"Copying files from {enricherOutputPath} to {settings.VirtuosoDestinationPath}");
                                    Directory.CreateDirectory(settings.VirtuosoDestinationPath);
                                    foreach (var sourceFilePath in Directory.GetFiles(enricherOutputPath, "*.xml"))
                                    {
                                        string fileName = Path.GetFileName(sourceFilePath);
                                        string destinationFilePath = Path.Combine(settings.VirtuosoDestinationPath, fileName);
                                        File.Copy(sourceFilePath, destinationFilePath, true);
                                    }

                                    _log.Info($"Extract successfully output to {settings.VirtuosoDestinationPath}");
                                    return (int)ExitCodes.Success;
                                }
                                else
                                {
                                    _log.Error("Enricher did not complete successfully, see enricher log for details");
                                    _log.Debug(eTask.Result.ResultMessage);
                                    return (int)(ExitCodes.EnricherError);
                                }
                            }
                            else
                            {
                                _log.Error($"Enricher task completed in unexpected state {eTask.Status}");
                                return (int)ExitCodes.EnricherError;
                            }
                        }
                    }
                    else
                    {
                        _log.Error($"Extractor task completed in unexpected state {extract.Status}");
                        return (int)ExitCodes.ExtractError;
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

        static int HandleParseError(IEnumerable<Error> errs)
        {
            return (int)ExitCodes.CommandLineParseError;
        }
    }
}
