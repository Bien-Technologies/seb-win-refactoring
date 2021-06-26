using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using SafeExamBrowser.I18n.Contracts;
using SafeExamBrowser.UserInterface.Contracts.Windows;
using SafeExamBrowser.UserInterface.Contracts.Windows.Data;
using SafeExamBrowser.UserInterface.Contracts.Windows.Events;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SafeExamBrowser.Configuration.Contracts;
using SafeExamBrowser.Logging.Contracts;
using SafeExamBrowser.Server.Contracts.Data;

namespace SafeExamBrowser.UserInterface.Desktop.Windows
{
	using Image = System.Drawing.Image;

	// SelectedDate="{Binding Path= AvailableFrom, Mode=TwoWay, TargetNullValue={x:Static System:DateTime.Today}}"
	internal partial class UserLoginDialog : Window, IUserLoginDialog
	{
		private readonly ILogger _logger;
		private readonly SessionConfiguration _session;
		private readonly ConnectionInfo connectionInfo;
		private WindowClosingEventHandler _closing;
		public DateTime? AvailableFrom { get; set; } = DateTime.Today;
		private UserLoginDialogResult result = new UserLoginDialogResult {Success = false};

		event WindowClosingEventHandler IWindow.Closing
		{
			add => _closing += value;
			remove => _closing -= value;
		}

		internal UserLoginDialog(ILogger logger, SessionConfiguration session, ConnectionInfo connectionInfo)
		{
			_logger = logger;
			_session = session;
			this.connectionInfo = connectionInfo;

			InitializeComponent();

			DataContext = this;

			// DOB.DisplayDateEnd = DateTime.Today;
			DOB.Text = "30-09-2021"; // DateTime.Today.ToShortDateString();
			Username.Text = "9873763134";
			
			//LoginMessage.Text = message;
			Title = "Candidate Details";
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			
			Closing += (o, args) =>
			{
				_closing?.Invoke();
			};

			Loaded += (o, args) => Activate();
			Username.KeyUp += Username_KeyUp;
			DOB.KeyUp += DOB_KeyUp;
		}

		public void BringToForeground()
		{
			Dispatcher.Invoke(Activate);
		}

		public UserLoginDialogResult Show(IWindow parent = null)
		{
			return Dispatcher.Invoke(() =>
			{
				if (parent is Window window)
				{
					Owner = window;
					WindowStartupLocation = WindowStartupLocation.CenterOwner;
				}

				if (ShowDialog() is true)
				{
					result.Username = Username.Text;
					result.DateOfBirth = DOB.SelectedDate.Value;
					result.Success = true;
				}

				return result;
			});
		}

		public void Message(string message, MessageBoxImage messageBoxImage)
		{
			MessageBox.Show(this, message, Title, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
		}

		private async Task<bool> FetchServerData()
		{
			using (var client = new HttpClient())
			{
				var dob = DOB.SelectedDate.Value.ToString("dd-MM-yyyy");

				var apiJson = JsonConvert.DeserializeObject(connectionInfo.Api) as JObject;

				var uri = $"{apiJson.GetValue("ValidateCandidateEndpoint")}?username={Username.Text}&dob={dob}";
				var request = new HttpRequestMessage(HttpMethod.Get, uri);
				// request.Headers.Add("Authorization", $"Bearer {_connectionInfo.Oauth2Token}");

				var response = await client.SendAsync(request); // .GetAwaiter().GetResult();
				if (response.IsSuccessStatusCode)
				{
					var rawJson = JsonConvert.DeserializeObject(Extract(response.Content)) as JObject;
					var apiResult =
						JsonConvert.DeserializeObject<UserLoginDialogResult>(rawJson["data"].ToString());
					if (apiResult is null)
					{
						Message("Invalid information, Please try again.", MessageBoxImage.Exclamation);
						return false;
					}

					result = apiResult;
					
					result.Username = Username.Text;
					result.DateOfBirth = DOB.SelectedDate.Value;
					
					_session.Settings.Proctoring.Candidate.Username = Username.Text;
					_session.Settings.Proctoring.Candidate.DateOfBirth = result.DateOfBirth;
					_session.Settings.Proctoring.Candidate.CandidateKey = result.CandidateKey;
					_session.Settings.Proctoring.Candidate.CandidateName = result.Name;
					
					return true;
				}
				else
				{
					Message("Error while connecting with server. Please try again", MessageBoxImage.Error);
					return false;
				}
			}
		}

		private string Extract(HttpContent content)
		{
			var task = Task.Run(async () => await content.ReadAsStreamAsync());
			var stream = task.GetAwaiter().GetResult();
			var reader = new StreamReader(stream);

			return reader.ReadToEnd();
		}
		
		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private void Username_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter && DOB.Focusable)
			{
				DOB.Focus();
			}
		}

		private void DOB_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				LoginFinishButton.Focus();
				// DialogResult = true;
				// Close();
			}
		}

		private static bool IsValidPhone(string phone)
		{
			if (string.IsNullOrWhiteSpace(phone)) return false;
			Regex r = new Regex(@"^\+?\d{0,2}\-?\d{4,5}\-?\d{5,6}");
			return r.IsMatch(phone);
		}

		private static bool IsValidEmail(string email)
		{
			if (string.IsNullOrWhiteSpace(email))
				return false;

			try
			{
				// Normalize the domain
				email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
					RegexOptions.None, TimeSpan.FromMilliseconds(200));

				// Examines the domain part of the email and normalizes it.
				string DomainMapper(Match match)
				{
					// Use IdnMapping class to convert Unicode domain names.
					var idn = new IdnMapping();

					// Pull out and process domain name (throws ArgumentException on invalid)
					string domainName = idn.GetAscii(match.Groups[2].Value);

					return match.Groups[1].Value + domainName;
				}
			}
			catch (RegexMatchTimeoutException e)
			{
				return false;
			}
			catch (ArgumentException e)
			{
				return false;
			}

			try
			{
				return Regex.IsMatch(email,
					@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
					RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
			}
			catch (RegexMatchTimeoutException)
			{
				return false;
			}
		}

		private async void LoginFinishButton_OnClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(Username.Text) || string.IsNullOrWhiteSpace(DOB.Text))
			{
				Message("Please fill all details", MessageBoxImage.Error);
				return;
			}

			if ((IsValidPhone(Username.Text) || IsValidEmail(Username.Text)))
			{
				if (await FetchServerData())
				{
					DialogResult = false;
					result.Success = true;
					Close();
				}
			}
			else
			{
				Message(@"Either email\phone is not in correct format. Try again!",
					MessageBoxImage.Error);
			}
		}
	}
}