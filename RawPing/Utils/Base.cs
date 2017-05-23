using System;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Net;
using System.Net.Sockets;
/// <summary>
/// Source http://www.codeplanet.eu/tutorials/csharp/4-tcp-ip-socket-programmierung-in-csharp.html?start=4
/// </summary>
namespace RawPing.Utils
{
	/// <summary>
	/// Implementierung eines Pings in C#.
	/// </summary>
	class MyPing
	{
		const int SOCKET_ERROR = -1;
		const int ICMP_ECHO = 8;

		public int GetPingTime(string host)
		{
			int nBytes = 0, dwStart = 0, dwStop = 0, PingTime = 0;

			IPHostEntry serverHE, fromHE;
			IcmpPacket packet = new IcmpPacket();

			if (host == null)
				return -1;

			// Einen Raw-Socket erstellen.
			Socket socket = new Socket(AddressFamily.InterNetwork,
									   SocketType.Raw,
									   ProtocolType.Icmp);

			serverHE = Dns.GetHostEntry(host);

			if (serverHE == null)
			{
				return -1; // Fehler
			}

			// Den IPEndPoint des Servers in einen EndPoint konvertieren.
			IPEndPoint ipepServer = new IPEndPoint(serverHE.AddressList[0], 0);
			EndPoint epServer = (ipepServer);

			// Den empfangenen Endpunkt für den Client-Rechner setzen.
			fromHE = Dns.GetHostEntry(Dns.GetHostName());
			IPEndPoint ipEndPointFrom = new IPEndPoint(fromHE.AddressList.Where(m=>m.AddressFamily != AddressFamily.InterNetworkV6).FirstOrDefault(), 0);
			EndPoint EndPointFrom = (ipEndPointFrom);

			int PacketSize = 0;

			for (int j = 0; j < 1; j++)
			{
				// Das zu sendende Paket erstellen.
				packet.Type = ICMP_ECHO;
				packet.SubCode = 0;
				packet.CheckSum = UInt16.Parse("0");
				packet.Identifier = UInt16.Parse("45");
				packet.SequenceNumber = UInt16.Parse("0");

				int PingData = 32;
				packet.Data = new byte[PingData];

				for (int i = 0; i < PingData; i++)
					packet.Data[i] = (byte)'#';

				PacketSize = PingData + 8;


				// Stelle sicher dass das icmp_pkt_buffer Byte Array 
				// eine gerade Zahl ist.
				if (PacketSize % 2 == 1)
					++PacketSize;

				byte[] icmp_pkt_buffer = new byte[PacketSize];

				int index = 0;

				index = Serialize(packet,
								  icmp_pkt_buffer,
								  PacketSize,
								  PingData);

				if (index == -1)
					return -1;

				// Die Prüfsumme für das Paket berechnen.
				double double_length = Convert.ToDouble(index);

				double dtemp = Math.Ceiling(double_length / 2);

				int cksum_buffer_length = Convert.ToInt32(dtemp);

				UInt16[] cksum_buffer = new UInt16[cksum_buffer_length];

				int icmp_header_buffer_index = 0;

				for (int i = 0; i < cksum_buffer_length; i++)
				{
					cksum_buffer[i] = BitConverter.ToUInt16(icmp_pkt_buffer, icmp_header_buffer_index);
					icmp_header_buffer_index += 2;
				}

				UInt16 u_cksum = checksum(cksum_buffer, cksum_buffer_length);
				packet.CheckSum = u_cksum;

				// Nachdem nun die Prüfsumme vorhanden ist, das Paket erneut serialisieren.
				byte[] sendbuf = new byte[PacketSize];

				index = Serialize(packet,
								  sendbuf,
								  PacketSize,
								  PingData);

				if (index == -1)
					return -1;

				dwStart = System.Environment.TickCount; // Starte den Timer

				if ((nBytes = socket.SendTo(sendbuf, PacketSize, 0, epServer)) == SOCKET_ERROR)
				{
					Console.WriteLine("Error calling sendto");
					return -1; // Fehler
				}

				// Initialisiere den Buffer. Der Empfänger-Buffer ist die Größe des
				// ICMP Header plus den IP Header (20 bytes)
				byte[] ReceiveBuffer = new byte[256];

				nBytes = 0;
				nBytes = socket.ReceiveFrom(ReceiveBuffer, 256, 0, ref EndPointFrom);

				if (nBytes == SOCKET_ERROR)
				{
					dwStop = SOCKET_ERROR;
				}
				else
				{
					// Stoppe den Timer
					dwStop = System.Environment.TickCount - dwStart;
				}
			}

			socket.Close();
			PingTime = (int)dwStop;
			return PingTime;
		}

		private static int Serialize(IcmpPacket packet, byte[] Buffer, int PacketSize, int PingData)
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

		private static UInt16 checksum(UInt16[] buffer, int size)
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

	public class IcmpPacket
	{
		public byte Type;               // Message Typ
		public byte SubCode;            // Subcode Typ
		public byte[] Data;             // Byte Array
		public UInt16 CheckSum;         // Checksumme
		public UInt16 Identifier;       // Identifizierer
		public UInt16 SequenceNumber;   // Sequenznummer 
	}
}

