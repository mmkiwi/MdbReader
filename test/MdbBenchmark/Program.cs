using BenchmarkDotNet.Running;

var summary = BenchmarkRunner.Run(typeof(Program).Assembly);
//await new MdbBenchmarks().RunJet3();