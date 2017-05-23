using System;
using System.Collections.Generic;
using System.Linq;

using System;

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

/// <summary>
/// Source http://www.codeplanet.eu/tutorials/csharp/4-tcp-ip-socket-programmierung-in-csharp.html?start=4
/// </summary>
namespace RawPing.Utils
{
	/// <summary>
	/// Implementierung eines Pings in C#.
	/// </summary>
	internal class MyPing
	{
		private const int SOCKET_ERROR = -1;
		private const int ICMP_ECHO = 8;

		private Action<IPAddress> OnPingReceive;
		private IcmpPacket packet;// = new IcmpPacket();
		private Socket socket;//= new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
		private int intCount = 0;

		public MyPing(Action<IPAddress> onPingReceive)
		{
			OnPingReceive = onPingReceive;
			packet = new IcmpPacket();
			socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
		}

		public void EnqueueIP(IPAddress input)
		{
			EndPoint epServer = (new IPEndPoint(input, 0));
			socket.BeginSendTo(packet.Buf, 0, packet.Size, SocketFlags.None, epServer, null, null);
			Thread.Sleep(1);
			if (intCount < 20)
			{
				byte[] ReceiveBuffer = new byte[256];
				socket.BeginReceive(ReceiveBuffer, 0, ReceiveBuffer.Length, SocketFlags.None, ReceiveCallback, ReceiveBuffer);
				intCount++;
			}
		}

		private void ReceiveCallback(IAsyncResult x)
		{
			var ReceiveBuffer = x.AsyncState as byte[];
			if (OnPingReceive != null)
			{
				var ipP = new byte[4];
				Array.Copy(ReceiveBuffer, 12, ipP, 0, 4);
				var dstip = new IPAddress(ipP);
				OnPingReceive(dstip);
			}
			socket.BeginReceive(ReceiveBuffer, 0, ReceiveBuffer.Length, SocketFlags.None, ReceiveCallback, ReceiveBuffer);
		}

		public int GetPingTime(string host)
		{
			if (host == null) return -1;

			IPHostEntry serverHE = Dns.GetHostEntry(host);
			if (serverHE == null) return -1; // Fehler

			// Den IPEndPoint des Servers in einen EndPoint konvertieren.
			EndPoint epServer = (new IPEndPoint(serverHE.AddressList[0], 0));

			socket.BeginSendTo(packet.Buf, 0, packet.Size, SocketFlags.None, epServer, null, null);

			// Initialisiere den Buffer. Der Empfänger-Buffer ist die Größe des
			// ICMP Header plus den IP Header (20 bytes)
			byte[] ReceiveBuffer = new byte[256];
			socket.BeginReceive(ReceiveBuffer, 0, ReceiveBuffer.Length, SocketFlags.None, x =>
			{
				var buf = x.AsyncState as byte[];
				var ipP = new byte[4];
				Array.Copy(buf, 12, ipP, 0, 4);
				var dstip = new IPAddress(ipP);
				Debugger.Break();
			}, ReceiveBuffer);

			//IPAddress.Parse(ReceiveBuffer);
			Console.ReadLine();

			return 0;
		}
	}

	public class Utils
	{
		public static int Serialize(IcmpPacket packet, byte[] Buffer, int PacketSize, int PingData)
		{
			int cbReturn = 0;

			// Serialisiere den struct in ein Array
			int Index = 0;

			byte[] b_type = new byte[1];
			b_type[0] = (packet.Type);

			byte[] b_code = new byte[1];
			b_code[0] = (packet.SubCode);

			byte[] b_cksum = BitConverter.GetBytes(packet.CheckSum);
			byte[] b_id = BitConverter.GetBytes(packet.Identifier);
			byte[] b_seq = BitConverter.GetBytes(packet.SequenceNumber);

			// Console.WriteLine("Serialize type ");
			Array.Copy(b_type, 0, Buffer, Index, b_type.Length);
			Index += b_type.Length;

			// Console.WriteLine("Serialize code ");
			Array.Copy(b_code, 0, Buffer, Index, b_code.Length);
			Index += b_code.Length;

			// Console.WriteLine("Serialize cksum ");
			Array.Copy(b_cksum, 0, Buffer, Index, b_cksum.Length);
			Index += b_cksum.Length;

			// Console.WriteLine("Serialize id ");
			Array.Copy(b_id, 0, Buffer, Index, b_id.Length);
			Index += b_id.Length;

			Array.Copy(b_seq, 0, Buffer, Index, b_seq.Length);
			Index += b_seq.Length;

			// Kopiere die Daten

			Array.Copy(packet.Data, 0, Buffer, Index, PingData);

			Index += PingData;

			if (Index != PacketSize /* sizeof(IcmpPacket) */)
			{
				cbReturn = -1;
				return cbReturn;
			}

			cbReturn = Index;
			return cbReturn;
		}

		public static UInt16 checksum(UInt16[] buffer, int size)
		{
			int cksum = 0;
			int counter;

			counter = 0;

			while (size > 0)
			{
				UInt16 val = buffer[counter];

				cksum += Convert.ToInt32(buffer[counter]);
				counter += 1;
				size -= 1;
			}

			cksum = (cksum >> 16) + (cksum & 0xffff);
			cksum += (cksum >> 16);
			return (UInt16)(~cksum);
		}
	}

	public class IcmpPacketWrapper
	{
		public byte[] Buf;
		public int Size;

		private const int SOCKET_ERROR = -1;
		private const int ICMP_ECHO = 8;

		public IcmpPacketWrapper()
		{
			int PingData = 32;
			int PacketSize = PingData + 8;
			int icmp_header_buffer_index = 0;

			// Das zu sendende Paket erstellen.
			IcmpPacket packet = new IcmpPacket()
			{
				Type = ICMP_ECHO,
				SubCode = 0,
				CheckSum = UInt16.Parse("0"),
				Identifier = UInt16.Parse("45"),
				SequenceNumber = UInt16.Parse("0"),
				Data = new byte[PingData].Select(m => (byte)'#').ToArray()
			};

			// Stelle sicher dass das icmp_pkt_buffer Byte Array
			// eine gerade Zahl ist.
			if (PacketSize % 2 == 1) ++PacketSize;

			byte[] icmp_pkt_buffer = new byte[PacketSize];
			int index = Utils.Serialize(packet, icmp_pkt_buffer, PacketSize, PingData);
			if (index == -1) return;

			// Die Prüfsumme für das Paket berechnen.
			int cksum_buffer_length = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(index) / 2));
			UInt16[] cksum_buffer = new UInt16[cksum_buffer_length];

			for (int i = 0; i < cksum_buffer_length; i++)
			{
				cksum_buffer[i] = BitConverter.ToUInt16(icmp_pkt_buffer, icmp_header_buffer_index);
				icmp_header_buffer_index += 2;
			}

			packet.CheckSum = Utils.checksum(cksum_buffer, cksum_buffer_length);

			// Nachdem nun die Prüfsumme vorhanden ist, das Paket erneut serialisieren.
			byte[] sendbuf = new byte[PacketSize];
			if (Utils.Serialize(packet, sendbuf, PacketSize, PingData) == -1) return;
			Buf = sendbuf;
			Size = PacketSize;
		}
	}

	public class IcmpPacket
	{
		public byte Type;               // Message Typ
		public byte SubCode;            // Subcode Typ
		public byte[] Data;             // Byte Array
		public UInt16 CheckSum;         // Checksumme
		public UInt16 Identifier;       // Identifizierer
		public UInt16 SequenceNumber;   // Sequenznummer

		public byte[] Buf;
		public int Size;
		private const int SOCKET_ERROR = -1;
		private const int ICMP_ECHO = 8;

		public IcmpPacket(bool doInit = true)
		{
			// Das zu sendende Paket erstellen.

			int PingData = 32;
			int PacketSize = PingData + 8;
			int icmp_header_buffer_index = 0;

			if (doInit)
			{
				Type = ICMP_ECHO;
				SubCode = 0;
				CheckSum = UInt16.Parse("0");
				Identifier = UInt16.Parse("45");
				SequenceNumber = UInt16.Parse("0");
				Data = new byte[PingData].Select(m => (byte)'#').ToArray();
			}

			// Stelle sicher dass das icmp_pkt_buffer Byte Array
			// eine gerade Zahl ist.
			if (PacketSize % 2 == 1) ++PacketSize;

			byte[] icmp_pkt_buffer = new byte[PacketSize];
			int index = Utils.Serialize(this, icmp_pkt_buffer, PacketSize, PingData);
			if (index == -1) return;

			// Die Prüfsumme für das Paket berechnen.
			int cksum_buffer_length = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(index) / 2));
			UInt16[] cksum_buffer = new UInt16[cksum_buffer_length];

			for (int i = 0; i < cksum_buffer_length; i++)
			{
				cksum_buffer[i] = BitConverter.ToUInt16(icmp_pkt_buffer, icmp_header_buffer_index);
				icmp_header_buffer_index += 2;
			}

			this.CheckSum = Utils.checksum(cksum_buffer, cksum_buffer_length);

			// Nachdem nun die Prüfsumme vorhanden ist, das Paket erneut serialisieren.
			byte[] sendbuf = new byte[PacketSize];
			if (Utils.Serialize(this, sendbuf, PacketSize, PingData) == -1) return;
			Buf = sendbuf;
			Size = PacketSize;
		}
	}
}