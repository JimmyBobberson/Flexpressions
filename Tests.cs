using System.Diagnostics;

namespace Flexpressions.Tests;

/// <summary>
/// AI-generated runtime test
/// </summary>
public static class RuntimeBattery {

	/// <summary>
	/// run runtime tests
	/// </summary>
	public static void Run() {
		Console.WriteLine("=== Flexpressions Performance Battery ===");
		Console.WriteLine("Warming up the JIT compiler...");
		WarmUp();
		Console.WriteLine("Warmup complete. Running benchmarks...\n");

		int iterations = 100_000;

		// 1. Instantiation (Whiteboard API overhead)
		RunBenchmark("Whiteboard Allocation", () => {
			Function f = ( Flex.x ^ 5 ) + 3 * ( Flex.y ^ 2 ) * Flex.x - 7 * Flex.z + 12;
		}, iterations);

		// 2. Instantiation (Mutable Builder API)
		RunBenchmark("Mutable Builder Allocation", () => {
			PolyEx p = new PolyEx();

			// Term 1: x^5
			p.Add(new MonoEx(1, Flex.x, 5));
			// Term 2: 3y^2x (Using your internal array constructor)
			p.Add(new MonoEx(3, [Flex.y, Flex.x], 2, 1));
			// Term 3: -7z
			p.Subtract(new MonoEx(7, Flex.z, 1));
			// Term 4: +12
			p.Add(new MonoEx(12));

			Function f = new Function(p);
		}, iterations);

		// 3. Evaluation (The Math.Pow override)
		Function evalFunc = ( Flex.x ^ 3 ) * ( Flex.y ^ 2 ) - 4 * Flex.x * Flex.z + 10;
		RunBenchmark("Evaluation f[x,y,z]", () => {
			double result = evalFunc[1.5, 2.0, 3.5];
		}, iterations);

		// 4. Calculus (Derivative)
		Function calcFunc = 5 * ( Flex.x ^ 4 ) * ( Flex.y ^ 3 ) - 2 * ( Flex.z ^ 5 );
		RunBenchmark("Partial Derivative (df/dx)", () => {
			Function df = calcFunc.Derivative(Flex.x);
		}, iterations);

		// 5. Calculus (Gradient Vector)
		Function gradFunc = ( Flex.x ^ 2 ) + 3 * Flex.x * Flex.y - ( Flex.y ^ 3 ) + ( Flex.z ^ 2 ) * Flex.t;
		RunBenchmark("Gradient Vector", () => {
			Function[] grad = gradFunc.Gradient();
		}, iterations);

		// 6. Equality Check (The new Span approach)
		PolyEx p1 = ( Flex.x ^ 2 ) + 3 * Flex.x * Flex.y - ( Flex.y ^ 3 );
		PolyEx p2 = ( Flex.x ^ 2 ) + 3 * Flex.x * Flex.y - ( Flex.y ^ 3 );
		RunBenchmark("Equality Check (==)", () => {
			bool isEqual = p1 == p2;
		}, iterations);
	}

	/// <summary>
	/// run benchmark
	/// </summary>
	private static void RunBenchmark(string name, Action test, int iterations) {
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		Stopwatch sw = Stopwatch.StartNew();

		for (int i = 0; i < iterations; i++) {
			test();
		}

		sw.Stop();

		double msPerOp = (double)sw.ElapsedMilliseconds / iterations;
		double nsPerOp = msPerOp * 1_000_000;

		Console.WriteLine($"{name,-30} | {iterations:N0} ops | Total: {sw.ElapsedMilliseconds,-4} ms | Per Op: {nsPerOp:F2} ns");
	}

	private static void WarmUp() {
		Function f = ( Flex.x ^ 2 ) + Flex.y;
		double val = f[2, 3];
		Function df = f.Derivative(Flex.x);
		Function[] g = f.Gradient();
		bool eq = f.Expression == df.Expression;
	}
}

/// <summary>
/// AI-generated memory test
/// </summary>
public static class MemoryBattery {

	/// <summary>
	/// run memory tests
	/// </summary>
	public static void Run() {
		Console.WriteLine("\n=== Flexpressions Memory Battery ===");
		Console.WriteLine("Warming up memory profiler...");
		WarmUp();
		Console.WriteLine("Warmup complete. Measuring heap allocations...\n");

		// We use fewer iterations here because memory is deterministic, 
		// unlike CPU speed which fluctuates. 10,000 is plenty.
		int iterations = 10_000;

		// 1. Instantiation (Whiteboard API)
		RunBenchmark("Whiteboard Allocation", () => {
			Function f = ( Flex.x ^ 5 ) + 3 * ( Flex.y ^ 2 ) * Flex.x - 7 * Flex.z + 12;
		}, iterations);

		// 2. Instantiation (Mutable Builder API)
		RunBenchmark("Mutable Builder Allocation", () => {
			PolyEx p = new PolyEx();
			p.Add(new MonoEx(1, Flex.x, 5));
			p.Add(new MonoEx(3, [Flex.y, Flex.x], 2, 1));
			p.Subtract(new MonoEx(7, Flex.z, 1));
			p.Add(new MonoEx(12));
			Function f = new Function(p);
		}, iterations);

		// 3. Evaluation
		Function evalFunc = ( Flex.x ^ 3 ) * ( Flex.y ^ 2 ) - 4 * Flex.x * Flex.z + 10;
		RunBenchmark("Evaluation f[x,y,z]", () => {
			double result = evalFunc[1.5, 2.0, 3.5];
		}, iterations);

		// 4. Calculus (Derivative)
		Function calcFunc = 5 * ( Flex.x ^ 4 ) * ( Flex.y ^ 3 ) - 2 * ( Flex.z ^ 5 );
		RunBenchmark("Partial Derivative (df/dx)", () => {
			Function df = calcFunc.Derivative(Flex.x);
		}, iterations);

		// 5. Calculus (Gradient Vector)
		Function gradFunc = ( Flex.x ^ 2 ) + 3 * Flex.x * Flex.y - ( Flex.y ^ 3 ) + ( Flex.z ^ 2 ) * Flex.t;
		RunBenchmark("Gradient Vector", () => {
			Function[] grad = gradFunc.Gradient();
		}, iterations);
	}

	/// <summary>
	/// run benchmark
	/// </summary>
	private static void RunBenchmark(string name, Action test, int iterations) {
		// Run once to ensure any static constructors or JIT caching is handled
		test();

		// Force a total GC cleanup so we have a completely clean slate
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		// Weigh the thread's memory BEFORE the test
		long startBytes = GC.GetAllocatedBytesForCurrentThread();

		for (int i = 0; i < iterations; i++) {
			test();
		}

		// Weigh the thread's memory AFTER the test
		long endBytes = GC.GetAllocatedBytesForCurrentThread();

		long totalAllocated = endBytes - startBytes;
		double bytesPerOp = (double)totalAllocated / iterations;

		Console.WriteLine($"{name,-30} | {iterations:N0} ops | Per Op: {bytesPerOp:F2} Bytes");
	}

	private static void WarmUp() {
		Function f = ( Flex.x ^ 2 ) + Flex.y;
		double val = f[2, 3];
		Function df = f.Derivative(Flex.x);
		Function[] g = f.Gradient();
	}
}