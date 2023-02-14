using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace ResizableSpanWriter.Benchmarks;
class Program
{
    private static IConfig BenchConfig => DefaultConfig.Instance.AddJob(Job.Default.AsDefault()
        .WithRuntime(CoreRuntime.Core70)
        .WithJit(Jit.RyuJit)
        .WithArguments(new[] { new MsBuildArgument("/p:Optimize=true") }));
    static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, BenchConfig);
    }
}