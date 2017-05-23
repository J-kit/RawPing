using RawPing.Utils;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;

namespace RawPing
{
	internal class Program
	{
		private static ConcurrentQueue<IPAddress> Queue = new ConcurrentQueue<IPAddress>();

		private static void WriteIt()
		{
			StreamWriter sw = new StreamWriter("output.txt");
			while (true)
			{
				while (Queue.TryDequeue(out var rip))
				{
					sw.WriteLine(rip.ToString());
				}
				if (Queue.Any())
				{
					Thread.SpinWait(1);
				}
				else
				{
					Thread.Sleep(1);
				}
			}
		}

		private static void Main(string[] args)
		{
			//	var erg = mp.GetPingTime("8.8.8.8");
			string line = " ";
			StreamReader file = new StreamReader(@"D:\Work\remotedesktop\Filterwork\All.txt");

			MyPing mp = new MyPing(x =>
			{
				Queue.Enqueue(x);

				//Console.WriteLine($"Online: {x.ToString()}");
			});
			new Thread(WriteIt) { IsBackground = true, Name = "WriteThread" }.Start();
			while (!string.IsNullOrEmpty((line = file.ReadLine())))
			{
				var ipInfo = ParseIpString(line);
				if (ipInfo.Item1)
				{
					mp.EnqueueIP(ipInfo.Item2.Item1);
				}
				//
			}
			Console.WriteLine("Enqueued");
			Console.ReadLine();
			Ping pingSender = new Ping();
			PingOptions options = new PingOptions();

			// Benutze den Standard TTL Wert (Time To Live) der bei 128ms liegt,
			// aber ändere das Fragmentationsverhalten.
			options.DontFragment = true;

			// Erzeuge einen Puffer mit der Länge von 32 Bytes
			// die versendet werden sollen.
			string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
			byte[] buffer = Encoding.ASCII.GetBytes(data);
			int timeout = 120;

			PingReply reply = pingSender.Send(args[0], timeout, buffer, options);
			if (reply.Status == IPStatus.Success)
			{
				Console.WriteLine("Address: {0}", reply.Address.ToString());
				Console.WriteLine("RoundTrip time: {0}", reply.RoundtripTime);
				Console.WriteLine("Time to live: {0}", reply.Options.Ttl);
				Console.WriteLine("Don't fragment: {0}", reply.Options.DontFragment);
				Console.WriteLine("Buffer size: {0}", reply.Buffer.Length);
			}
		}

		public static Tuple<bool, Tuple<IPAddress, int>> ParseIpString(string input, int defaultport = 3389)
		{
			var tmp = input.Split(new[] { ':' });

			if (tmp.Length == 1 && IPAddress.TryParse(tmp[0], out var prIp))
			{
				return Tuple.Create(true, Tuple.Create(prIp, defaultport));
			}
			else if (tmp.Length == 2 && IPAddress.TryParse(tmp[0], out prIp) && Int32.TryParse(tmp[1], out defaultport))
			{
				return Tuple.Create(true, Tuple.Create(prIp, defaultport));
			}
			else
			{
				return Tuple.Create(false, default(Tuple<IPAddress, int>));
			}
		}
	}
}