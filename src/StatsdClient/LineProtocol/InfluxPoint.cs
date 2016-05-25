using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Telegraf {

	public class InfluxPoint {
		public readonly string Measurement;
		public readonly IReadOnlyDictionary<string, object> Fields;
		public readonly IReadOnlyDictionary<string, string> Tags;
		public readonly DateTime? UtcTimestamp;

		public InfluxPoint(
			string measurement,
			IReadOnlyDictionary<string, object> fields,
			IReadOnlyDictionary<string, string> tags = null,
			DateTime? utcTimestamp = null)
		{
			if (string.IsNullOrEmpty(measurement)) throw new ArgumentException("A measurement name must be specified");
			if (fields == null || fields.Count == 0) throw new ArgumentException("At least one field must be specified");

			foreach (var f in fields)
				if (string.IsNullOrEmpty(f.Key)) throw new ArgumentException("Fields must have non-empty names");

			if (tags != null)
				foreach (var t in tags)
					if (string.IsNullOrEmpty(t.Key)) throw new ArgumentException("Tags must have non-empty names");

			if (utcTimestamp != null && utcTimestamp.Value.Kind != DateTimeKind.Utc)
				throw new ArgumentException("Timestamps must be specified as UTC");

			Measurement = measurement;
			Fields = fields;
			Tags = tags;
			UtcTimestamp = utcTimestamp;
		}

		public void Format(TextWriter textWriter, IDictionary<string,string> staticTags)
		{
			if (textWriter == null) throw new ArgumentNullException(nameof(textWriter));

			textWriter.Write(InfluxSyntax.EscapeName(Measurement));

			IEnumerable<KeyValuePair<string,string>> tags = staticTags;
			if (Tags != null) {
				tags = staticTags.Concat(Tags);
			}

			// Tags should be sorted by key before being sent for best performance.
			foreach (var t in tags.OrderBy(t => t.Key))
			{
				if (string.IsNullOrEmpty(t.Value))
					continue;

				textWriter.Write(',');
				textWriter.Write(InfluxSyntax.EscapeName(t.Key));
				textWriter.Write('=');
				textWriter.Write(InfluxSyntax.EscapeName(t.Value));
			}

			var fieldDelim = ' ';
			foreach (var f in Fields)
			{
				textWriter.Write(fieldDelim);
				fieldDelim = ',';
				textWriter.Write(InfluxSyntax.EscapeName(f.Key));
				textWriter.Write('=');
				textWriter.Write(InfluxSyntax.FormatValue(f.Value));
			}

			if (UtcTimestamp != null)
			{
				textWriter.Write(' ');
				textWriter.Write(InfluxSyntax.FormatTimestamp(UtcTimestamp.Value));
			}
		}
	}

}