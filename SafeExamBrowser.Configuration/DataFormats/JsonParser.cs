using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using SafeExamBrowser.Configuration.Contracts;
using SafeExamBrowser.Configuration.Contracts.Cryptography;
using SafeExamBrowser.Configuration.Contracts.DataCompression;
using SafeExamBrowser.Configuration.Contracts.DataFormats;
using SafeExamBrowser.Logging.Contracts;
using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SafeExamBrowser.Configuration.DataFormats
{
	public class JsonParser : IDataParser
	{
		private const string JSON_PREFIX = "{";
		
		private IDataCompressor compressor;
		private ILogger logger;

		public JsonParser(IDataCompressor compressor, ILogger logger)
		{
			this.compressor = compressor;
			this.logger = logger;
		}
		
		public bool CanParse(Stream data)
		{
			try
			{
				var longEnough = data.Length > JSON_PREFIX.Length;

				if (longEnough)
				{
					var prefix = ReadPrefix(data);
					var isValid = JSON_PREFIX.Equals(prefix, StringComparison.OrdinalIgnoreCase);

					logger.Debug($"'{data}' starting with '{prefix}' {(isValid ? "matches" : "does not match")} the {FormatType.Xml} format.");

					return isValid;
				}

				logger.Debug($"'{data}' is not long enough ({data.Length} bytes) to match the {FormatType.Xml} format.");
			}
			catch (Exception e)
			{
				logger.Error($"Failed to determine whether '{data}' with {data?.Length / 1000.0} KB data matches the {FormatType.Xml} format!", e);
			}

			return false;
		}

		public ParseResult TryParse(Stream data, PasswordParameters password = null)
		{
			var prefix = ReadPrefix(data);
			var isValid = JSON_PREFIX.Equals(prefix, StringComparison.OrdinalIgnoreCase);
			var result = new ParseResult { Status = LoadStatus.InvalidData };

			if (isValid)
			{
				data = compressor.IsCompressed(data) ? compressor.Decompress(data) : data;
				data.Seek(0, SeekOrigin.Begin);

				using (JsonTextReader reader = new JsonTextReader(new StreamReader(data)))
				{
					JObject o2 = (JObject)JToken.ReadFrom(reader);
					var rawJson = o2["data"] as JObject;
					
					result.RawData = rawJson.ToObject<Dictionary<string, object>>();
					result.Status = LoadStatus.Success;
				}
				
				result.Format = FormatType.Json;
			}
			else
			{
				logger.Error($"'{data}' starting with '{prefix}' does not match the {FormatType.Xml} format!");
			}

			return result;
		}
		
		private string ReadPrefix(Stream data)
		{
			var prefixData = new byte[JSON_PREFIX.Length];

			if (compressor.IsCompressed(data))
			{
				prefixData = compressor.Peek(data, JSON_PREFIX.Length);
			}
			else
			{
				data.Seek(0, SeekOrigin.Begin);
				data.Read(prefixData, 0, JSON_PREFIX.Length);
			}

			return Encoding.UTF8.GetString(prefixData);
		}
	}
}