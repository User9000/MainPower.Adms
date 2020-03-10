using System;

namespace MainPower.Adms.ExtractManager
{
    public class EnricherResult
    {
        public DateTime Time { get; set; }

        public bool HasOutput
        {
            get
            {
                return Result >= 0;
            }
        }
        public bool Success
        {
            get
            {
                return Result < 3 && Result >= 0;
            }
        }

        public int Result { get; set; } = -1;
        public int Fatals { get; set; }
        public int Errors { get; set; }
        public int Warnings { get; set; }

        public string ResultMessage
        {
            get
            {
                return Result switch
                {
                    0 => "Enricher enriched successfully.",
                    1 => "Enricher enriched with warnings.",
                    2 => "Enricher enriched with errors.",
                    3 => "Enricher failed.",
                    _ => "Idf has not been enriched yet.",
                };
            }
        }

        public string StatsMessage
        {
            get
            {
                return $"Fatals: {Fatals}, Errors: {Errors}, Warnings: {Warnings}";
            }
        }

    }
}
