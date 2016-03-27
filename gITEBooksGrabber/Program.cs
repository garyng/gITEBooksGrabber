using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace gITEBooksGrabber
{
	class Program
	{
		static void Main(string[] args)
		{
			Books bs = new Books();
			bs.Start().Wait();

			Console.ReadKey();
		}

	}

}
