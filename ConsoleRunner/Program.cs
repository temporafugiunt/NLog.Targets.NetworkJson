using System;
using System.IO;
using System.Threading;
using LumenWorks.Framework.IO.Csv;
using NLog;

namespace NLog.Targets.Gelf.ConsoleRunner
{
    class Program
    {
        private static readonly Random Random = new Random();
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        static void Main()
        {
            int index = 0;
            while (true)
            {
                var comic = GetNextComic();

                var eventInfo = new LogEventInfo
                                    {
                                        Message = comic.Title,
                                        Level = LogLevel.Info,
                                    };
                eventInfo.Properties.Add("Publisher", comic.Publisher);
                eventInfo.Properties.Add("ReleaseDate", comic.ReleaseDate);

                Logger.Log(eventInfo);

                // sometimes throws exception
                if (index%10 == 0)
                {
                    try
                    {
                        ReThrowVerbose(comic.Title);
                    }
                    catch (VerboseException ex)
                    {
                        var eventInfo2 = new LogEventInfo
                        {
                            Level = LogLevel.Warn,
                            Message = "We have outer exception: " + ex.Message,
                            Exception = ex
                        };
                        Logger.Log(eventInfo2);
                    }
                }

                Thread.Sleep(1000);
                index++;
            }
        }

        static void ThrowVerbose(string message)
        {
            throw new VerboseException(message);
        }

        static void ReThrowVerbose(string message)
        {
            try
            {
                ThrowVerbose(message);
            }
            catch (VerboseException ex)
            {
                var eventInfo = new LogEventInfo
                {
                    Level = LogLevel.Error,
                    Message = "We have inner exception: " + ex.Message,
                    Exception = ex
                };
                Logger.Log(eventInfo);

                throw new VerboseException("This is exception wrapper", ex);
            }
        }

        private static Comic GetNextComic()
        {
            var nextComicIndex = Random.Next(1, 400);

            using (var csv = new CsvReader(new StreamReader("comics.csv"), false))
            {
                csv.MoveTo(nextComicIndex);
                return new Comic
                {
                    Title = csv[2],
                    Publisher = csv[1],
                    ReleaseDate = csv[0]
                };
            }
        }
    }

    class Comic
    {
        public string Title { get; set; }
        public string ReleaseDate { get; set; }
        public string Publisher { get; set; }
    }

    class VerboseException : Exception
    {
        public VerboseException(string message) : base(message)
        {
        }

        public VerboseException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
