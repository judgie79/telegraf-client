using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Telegraf {

	public class AsyncSender : ISender {
		readonly ConcurrentQueue<InfluxPoint> _queue = new ConcurrentQueue<InfluxPoint>();
		private readonly Dictionary<string, string> _tags;

		long _discarded;
		long _failed;
		readonly int _maxLevel;
		readonly int _batchSize;
		readonly ITextUDP _udp;

		public long CountDiscarded() {
			return _discarded;
		}

		public long CountFailed() {
			return _failed;
		}

		public AsyncSender(ITextUDP udp, Dictionary<string, string> tags, int maxLevel, int batchSize) {
			_maxLevel = maxLevel;
			_batchSize = batchSize;
			_udp = udp;
			_tags = tags;
		}

		public void Send(InfluxPoint point) {
			if (_queue.Count > _maxLevel) {
				Interlocked.Increment(ref _discarded);
				return;
			}
			_queue.Enqueue(point);
		}

		public void KeepDelivering(CancellationToken token) {
			var list = new List<InfluxPoint>(_batchSize);

			while (!token.IsCancellationRequested) {
				try {
					list.Clear();
					InfluxPoint point;
					while (_queue.TryDequeue(out point) && (list.Count < list.Capacity)) {
						list.Add(point);
					}

					if (list.Count == 0) {
						// nothing to send. Sleep
						token.WaitHandle.WaitOne(200);
						continue;
					}

					using (var writer = new StringWriter()) {
						for (int i = 0; i < list.Count; i++) {
							if (i > 0) {
								writer.Write('\n');
							}
							list[i].Format(writer, _tags);
							
						}
						_udp.Send(writer.ToString());
					}
				}
				catch (Exception ex) {
					Interlocked.Increment(ref _failed);
					DiagnosticsLog.WriteLine("Failed to send metrics: {0}", ex);
				}
			}
		}
	}

}