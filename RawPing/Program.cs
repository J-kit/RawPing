using RawPing.Utils;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace RawPing
{
	class Program
	{
		static void Main(string[] args)
		{
			//if (args.Length < 1)
			//{
			//	throw new ArgumentException("Parameters: [<Uri>]");
			//}

			MyPing mp = new MyPing();

			var erg = mp.GetPingTime("8.8.8.8");

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
	}
}
