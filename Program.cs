using System;
using System.Diagnostics;

namespace Flexpressions {

	class Program {

		static void Main(string[] args) {

			MonoEx<float> one = new MonoEx<float>(1);
			MonoEx<int> negtwo = new MonoEx<int>(-2);
			MonoEx<int> quad = new MonoEx<int>('x', 2);
			MonoEx<double> flippy = new MonoEx<double>(7, 't', -2);

			Console.WriteLine(one);
			Console.WriteLine(negtwo);
			Console.WriteLine(quad);
			Console.WriteLine(flippy);
			//Console.WriteLine("\u207B");

		}

	}
}