using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Telegraf {

	public interface ITextUDP {
		void Send(string command);
	}

	public class TextUDP : IDisposable, ITextUDP {
		public IPEndPoint IPEndpoint { get; private set; }

		public Exception LastUdpSocketException { get; private set; }
		public long UdpSocketExceptionCount;

		readonly int _maxUdpPacketSizeBytes;
		readonly Socket _udpSocket;
		readonly string _name;

		/// <summary>
		/// Creates a new TextUDP class for lower level access to statsd.
		/// </summary>
		/// <param name="name">Hostname or IP (v4) address of the statsd server.</param>
		/// <param name="port">Port of the statsd server. Default is 8125.</param>
		/// <param name="maxUdpPacketSizeBytes">Max packet size, in bytes. This is useful to tweak if your MTU size is different than normal. Set to 0 for no limit. Default is MetricsConfig.DefaultMaxUDPPacketSize.</param>
		public TextUDP(string name, int port = 8125,
			int maxUdpPacketSizeBytes = MetricsConfig.DefaultMaxUDPPacketSize) {
			_name = name;
			_maxUdpPacketSizeBytes = maxUdpPacketSizeBytes;

			_udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

			var ipAddress = GetIpv4Address(name);
			IPEndpoint = new IPEndPoint(ipAddress, port);
		}

		IPAddress GetIpv4Address(string name) {
			IPAddress ipAddress;
			var isValidIpAddress = IPAddress.TryParse(name, out ipAddress);

			if (!isValidIpAddress) {
				ipAddress = GetIpFromHostname();
			}

			return ipAddress;
		}

		IPAddress GetIpFromHostname() {
			var addressList = Dns.GetHostEntry(_name).AddressList;
			var ipv4Addresses = addressList.Where(x => x.AddressFamily != AddressFamily.InterNetworkV6);

			return ipv4Addresses.First();
		}

		public void Send(string command) {
			Send(Encoding.ASCII.GetBytes(command));
		}

		void Send(byte[] encodedCommand) {
			if (_maxUdpPacketSizeBytes > 0 && encodedCommand.Length > _maxUdpPacketSizeBytes) {
				// If the command is too big to send, linear search backwards from the maximum
				// packet size to see if we can find a newline delimiting two stats. If we can,
				// split the message across the newline and try sending both componenets individually
				var newline = Encoding.ASCII.GetBytes("\n")[0];
				for (var i = _maxUdpPacketSizeBytes; i > 0; i--) {
					if (encodedCommand[i] != newline) {
						continue;
					}

					var encodedCommandFirst = new byte[i];
					Array.Copy(encodedCommand, encodedCommandFirst, encodedCommandFirst.Length);
						// encodedCommand[0..i-1]
					Send(encodedCommandFirst);

					var remainingCharacters = encodedCommand.Length - i - 1;
					if (remainingCharacters > 0) {
						var encodedCommandSecond = new byte[remainingCharacters];
						Array.Copy(encodedCommand, i + 1, encodedCommandSecond, 0, encodedCommandSecond.Length);
							// encodedCommand[i+1..end]
						Send(encodedCommandSecond);
					}

					return; // We're done here if we were able to split the message.
					// At this point we found an oversized message but we weren't able to find a 
					// newline to split upon. We'll still send it to the UDP socket, which upon sending an oversized message 
					// will fail silently if the user is running in release mode or report a SocketException if the user is 
					// running in debug mode.
					// Since we're conservative with our MAX_UDP_PACKET_SIZE, the oversized message might even
					// be sent without issue.
				}
			}
			try {
				_udpSocket.SendTo(encodedCommand, encodedCommand.Length, SocketFlags.None, IPEndpoint);
			}
			catch (Exception ex) {
				Interlocked.Increment(ref UdpSocketExceptionCount);
				LastUdpSocketException = ex;
			}
		}

		//reference : https://lostechies.com/chrispatterson/2012/11/29/idisposable-done-right/
		bool _disposed;

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~TextUDP() {
			// Finalizer calls Dispose(false)
			Dispose(false);
		}

		protected virtual void Dispose(bool disposing) {
			if (_disposed) {
				return;
			}

			if (disposing) {
				if (_udpSocket != null) {
					try {
						_udpSocket.Close();
					}
					catch (Exception) {
						//Swallow since we are not using a logger, should we add LibLog and start logging??
					}
				}
			}

			_disposed = true;
		}
	}

}