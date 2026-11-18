using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using ExpertPdf.Anywhere.HtmlToPdf;

namespace MultithreadedPerformance
{
    /// <summary>
    /// Runs many conversions on several threads at the same time and measures the throughput, the way a web
    /// application or a service converts pages for many users. Run it without arguments for a short test on the
    /// bundled sample.html.
    /// </summary>
    internal static class Program
    {
        private static string s_input;
        private static string s_outputFolder;
        private static int s_conversionsPerThread;
        private static int s_completed;
        private static int s_failed;
        private static Stopwatch s_clock;

        private static int Main(string[] args)
        {
            if (args.Length > 3 || (args.Length > 0 && (args[0] == "--help" || args[0] == "/?")))
            {
                Console.WriteLine("Usage: MultithreadedPerformance [threads] [conversions per thread] [url or html file]");
                Console.WriteLine("  defaults: 4 threads, 3 conversions each, sample.html");
                return 0;
            }

            string appFolder = AppDomain.CurrentDomain.BaseDirectory;
            int threads = args.Length > 0 ? int.Parse(args[0]) : 4;
            s_conversionsPerThread = args.Length > 1 ? int.Parse(args[1]) : 3;

            s_input = args.Length > 2 ? args[2] : Path.Combine(appFolder, "sample.html");
            s_outputFolder = Path.Combine(appFolder, "out");
            Directory.CreateDirectory(s_outputFolder);

            Console.WriteLine("{0} threads x {1} conversions of {2}", threads, s_conversionsPerThread, s_input);
            Console.WriteLine("Output folder: " + s_outputFolder);
            Console.WriteLine();

            s_clock = Stopwatch.StartNew();
            Thread[] workers = new Thread[threads];
            for (int i = 0; i < threads; i++)
            {
                workers[i] = new Thread(Worker);
                workers[i].Start(i);
            }
            foreach (Thread worker in workers)
                worker.Join();

            double seconds = s_clock.Elapsed.TotalSeconds;
            Console.WriteLine();
            Console.WriteLine("{0} conversions in {1:F1} s: {2:F2} PDF/s. Failed: {3}.", s_completed, seconds, s_completed / seconds, s_failed);
            return s_failed == 0 ? 0 : 1;
        }

        private static void Worker(object state)
        {
            int threadIndex = (int)state;
            for (int i = 0; i < s_conversionsPerThread; i++)
            {
                string outputFile = Path.Combine(s_outputFolder, string.Format("thread{0}_conversion{1}.pdf", threadIndex, i));
                Stopwatch watch = Stopwatch.StartNew();
                try
                {
                    // PdfConverter is not shared between threads: every conversion has its own instance
                    PdfConverter converter = new PdfConverter();
                    // converter.LicenseKey = "your license key";

                    if (File.Exists(s_input))
                        converter.SavePdfFromHtmlFileToFile(s_input, outputFile);
                    else
                        converter.SavePdfFromUrlToFile(s_input, outputFile);

                    int completed = Interlocked.Increment(ref s_completed);
                    Console.WriteLine("Thread {0}, conversion {1}: {2:F2} s. Completed: {3}, average {4:F2} PDF/s",
                        threadIndex, i, watch.Elapsed.TotalSeconds, completed, completed / s_clock.Elapsed.TotalSeconds);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref s_failed);
                    Console.WriteLine("Thread {0}, conversion {1} failed: {2}", threadIndex, i, ex.Message);
                }
            }
        }
    }
}
