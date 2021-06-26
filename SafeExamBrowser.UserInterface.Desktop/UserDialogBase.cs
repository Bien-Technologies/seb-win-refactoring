using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SafeExamBrowser.Server.Contracts.Data;
using SafeExamBrowser.UserInterface.Contracts.Windows.Data;

namespace SafeExamBrowser.UserInterface.Desktop.Windows
{
	public class UserDialogBase
	{
		public readonly ConnectionInfo connectionInfo;
		public string connectionToken;
		public readonly UserImagesDialogResult result = new UserImagesDialogResult {Success = false};

		public UserDialogBase(ConnectionInfo connectionInfo)
		{
			this.connectionInfo = connectionInfo;
		}

		public async Task<JObject> QueryStatus()
		{
			using (var httpclient = new HttpClient())
			{
				var apiJson = JsonConvert.DeserializeObject(connectionInfo.Api) as JObject;
				var uri =
					$"{apiJson.GetValue("QueryUploadedDataStatusEndpoint")}?CandidateKey={result.CandidateKey}&ExamScheduleKey={result.ExamScheduleKey}";
				var request = new HttpRequestMessage(HttpMethod.Get, uri);
				request.Headers.Add("SEBConnectionToken", connectionToken);
				var response = await httpclient.SendAsync(request); //.GetAwaiter().GetResult();
				if (response.IsSuccessStatusCode)
				{
					var responseContent = Extract(response.Content);
					var rawJson = JsonConvert.DeserializeObject(responseContent) as JObject;
					rawJson = rawJson["data"] as JObject;

					return rawJson;
				}

				return null;
			}
		}
		
		public string Extract(HttpContent content)
		{
			var task = Task.Run(async () => await content.ReadAsStreamAsync());
			var stream = task.GetAwaiter().GetResult();
			var reader = new StreamReader(stream);

			return reader.ReadToEnd();
		}
	}
}