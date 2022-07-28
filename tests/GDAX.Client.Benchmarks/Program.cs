using System.Reflection;
using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(Assembly.GetEntryAssembly()).Run();
Console.ReadLine();
