using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml;
using Frends.Hl7v2.CreateFromXml.Definitions;
using NHapi.Base.Parser;
using NUnit.Framework;

namespace Frends.Hl7v2.CreateFromXml.Tests;

[TestFixture]
[NonParallelizable]
[Explicit("Performance benchmark; run manually.")]
public class LargeMessageBenchmarks
{
    [Test]
    public void Measure_Large_Message_Shapes()
    {
        var iterations = int.Parse(
            Environment.GetEnvironmentVariable("HL7_BENCHMARK_ITERATIONS") ?? "3",
            CultureInfo.InvariantCulture);
        Assert.That(iterations, Is.InRange(1, 20));

        var reportPath = Environment.GetEnvironmentVariable("HL7_BENCHMARK_RESULTS")
                         ?? Path.Combine(FindSolutionDirectory(), "TestResultFile.md");

        if (!File.Exists(reportPath))
        {
            var introduction =
                "# XML to HL7 performance results\n\n" +
                "Synthetic data only. Times are machine-specific, not CI pass/fail thresholds.\n" +
                "Task time and managed allocations are medians of repeated end-to-end calls, after warm-up. " +
                "Input generation, output verification and report writing are outside the timed region. " +
                "Allocated MiB is cumulative allocation on the calling thread, not peak memory.\n" +
                "The separate stock-NHapi timings are single diagnostic samples on the same input. " +
                "Stock parse includes DOM loading and validation; do not add the DOM column to it. " +
                "The stock parser remains unchanged in every stage. Task output is checked against both " +
                "stock NHapi and an independently constructed expected message using SHA-256.\n\n";
            File.AppendAllText(reportPath, introduction);
        }

        var heading =
            $"## Benchmark tests - {DateTimeOffset.UtcNow:O}\n\n" +
            $"- Iterations: {iterations}\n" +
            $"- Runtime: {RuntimeInformation.FrameworkDescription}; {RuntimeInformation.OSDescription}; " +
            $"{RuntimeInformation.ProcessArchitecture}; logical CPUs: {Environment.ProcessorCount}; server GC: {GCSettings.IsServerGC}\n" +
            $"- CPU: {Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Not reported"}\n" +
            $"- NHapi assembly version: {typeof(DefaultXMLParser).Assembly.GetName().Version}\n\n" +
            "| Case | XML KiB (UTF-8) | Task median ms | Task allocated MiB | DOM ms | Stock parse ms | Stock encode ms | Stock newlines ms |\n" +
            "|---|---:|---:|---:|---:|---:|---:|---:|\n";
        File.AppendAllText(reportPath, heading);

        WarmUp();
        foreach (var (name, create) in Cases())
        {
            var message = create();
            var expectedHash = Hash(message.ExpectedEr7);
            var domMs = Measure(
                () =>
                {
                    var document = new XmlDocument();
                    document.LoadXml(message.Xml);
                    return document;
                },
                out _);
            var parseMs = Measure(() => new DefaultXMLParser().Parse(message.Xml), out var parsed);
            var encodeMs = Measure(() => new PipeParser().Encode(parsed), out var encoded);
            var newlineMs = Measure(() => encoded.ReplaceLineEndings("\n"), out var referenceOutput);
            Assert.That(Hash(referenceOutput), Is.EqualTo(expectedHash), $"Stock parser: {name}");

            var (elapsed, allocated) = MeasureTask(message, iterations, name);

            var row = FormattableString.Invariant(
                $"| {name} | {Encoding.UTF8.GetByteCount(message.Xml) / 1024.0:F1} | {elapsed:F2} | {allocated:F2} | {domMs:F2} | {parseMs:F2} | {encodeMs:F2} | {newlineMs:F2} |");
            File.AppendAllText(reportPath, row + "\n");
            TestContext.Progress.WriteLine(row);
        }

        File.AppendAllText(reportPath, "\nCompleted: all output checks passed.\n\n");
        TestContext.Progress.WriteLine($"Results appended to {reportPath}");
    }

    [Test]
    public void Measure_Large_Sibling_Stress()
    {
        var iterations = int.Parse(
            Environment.GetEnvironmentVariable("HL7_BENCHMARK_ITERATIONS") ?? "3",
            CultureInfo.InvariantCulture);
        Assert.That(iterations, Is.InRange(1, 20));
        var reportPath = Environment.GetEnvironmentVariable("HL7_BENCHMARK_RESULTS")
                         ?? Path.Combine(FindSolutionDirectory(), "TestResultFile.md");
        WarmUp();
        var message = SyntheticHl7Message.Create(1, 64000);
        var (elapsed, allocated) = MeasureTask(message, iterations, "64,000 sibling groups");
        var section = $"\n## Supplemental stress —  {DateTimeOffset.UtcNow:O}\n\n" +
                      $"Median of {iterations} warmed-up samples.\n\n" +
                      "This additional final-stage case puts all 64,000 observations under one order. " +
                      "Only the task is timed: the stock parser is deliberately not run at this scale " +
                      "to avoid the known quadratic stall. Complete output is checked against the " +
                      "independently generated expected message. There is no baseline measurement for this case.\n\n" +
                      "| Case | XML KiB (UTF-8) | Task median ms | Task allocated MiB |\n" +
                      "|---|---:|---:|---:|\n" +
                      FormattableString.Invariant(
                          $"| Sibling groups: 64000 | {Encoding.UTF8.GetByteCount(message.Xml) / 1024.0:F1} | {elapsed:F2} | {allocated:F2} |\n\n") +
                      "Completed: all output checks passed.\n\n";
        File.AppendAllText(reportPath, section);
        TestContext.Progress.WriteLine(section);
    }

    private static IEnumerable<(string Name, Func<SyntheticHl7Message> Create)> Cases()
    {
        foreach (var count in new[] { 1000, 2000, 4000, 8000 })
            yield return ($"Sibling groups: {count}", () => SyntheticHl7Message.Create(1, count));

        yield return ("Spread groups: 80 x 100", () => SyntheticHl7Message.Create(80, 100));
        yield return ("Spread groups: 640 x 100", () => SyntheticHl7Message.Create(640, 100));
        yield return ("One field: 1 MiB", () => SyntheticHl7Message.Create(1, 1, valueLength: 1024 * 1024));
        yield return ("One field: 10 MiB", () => SyntheticHl7Message.Create(1, 1, valueLength: 10 * 1024 * 1024));

        foreach (var count in new[] { 1000, 4000, 8000 })
            yield return ($"Repeated OBX.5 fields: {count}", () => SyntheticHl7Message.Create(1, 1, count));
    }

    private static void WarmUp()
    {
        var message = SyntheticHl7Message.Create(1, 10, 2);
        for (var i = 0; i < 3; i++)
        {
            _ = new PipeParser().Encode(new DefaultXMLParser().Parse(message.Xml));
            var result = Hl7v2.CreateFromXml(new Input { Xml = message.Xml }, new Options(), CancellationToken.None);
            Assert.That(result.Hl7v2Message, Is.EqualTo(message.ExpectedEr7));
        }
    }

    private static (double Milliseconds, double AllocatedMiB) MeasureTask(
        SyntheticHl7Message message,
        int iterations,
        string name)
    {
        var expectedHash = Hash(message.ExpectedEr7);
        var elapsed = new List<double>();
        var allocated = new List<double>();
        var input = new Input { Xml = message.Xml };
        var options = new Options();
        for (var iteration = 0; iteration < iterations; iteration++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            var milliseconds = Measure(
                () => Hl7v2.CreateFromXml(input, options, CancellationToken.None),
                out var result);
            var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            elapsed.Add(milliseconds);
            allocated.Add(bytes / (1024.0 * 1024.0));
            Assert.That(result.Success, Is.True, name);
            Assert.That(Hash(result.Hl7v2Message), Is.EqualTo(expectedHash), $"{name}, sample {iteration + 1}");
        }

        return (Median(elapsed), Median(allocated));
    }

    private static double Measure<T>(Func<T> action, out T result)
    {
        var start = Stopwatch.GetTimestamp();
        result = action();
        return Stopwatch.GetElapsedTime(start).TotalMilliseconds;
    }

    private static double Median(List<double> values)
    {
        var ordered = values.OrderBy(value => value).ToArray();
        var middle = ordered.Length / 2;
        return ordered.Length % 2 == 0 ? (ordered[middle - 1] + ordered[middle]) / 2 : ordered[middle];
    }

    private static string Hash(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static string FindSolutionDirectory()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             directory != null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Frends.Hl7v2.CreateFromXml.sln")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException("Set HL7_BENCHMARK_RESULTS when running outside the source checkout.");
    }
}
