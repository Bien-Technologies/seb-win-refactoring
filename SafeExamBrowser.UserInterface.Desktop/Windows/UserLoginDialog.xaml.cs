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


	class CaptureImageStep
	{
		public string Message { get; set; }

		public string Title { get; set; }

		public Image Image { get; set; }

		public string Username { get; set; }

		public string Dob { get; set; }

		public RequiredDocument SelectedDocument { get; set; }
	}

	// SelectedDate="{Binding Path= AvailableFrom, Mode=TwoWay, TargetNullValue={x:Static System:DateTime.Today}}"
	internal partial class UserLoginDialog : Window, IUserLoginDialog, IWebcamCaptureView
	{
		private readonly WebcamCapturePresenter _webCamCapture;
		private readonly ILogger _logger;
		private readonly SessionConfiguration _session;
		private readonly ConnectionInfo _connectionInfo;
		private WindowClosingEventHandler _closing;
		public DateTime? AvailableFrom { get; set; } = DateTime.Today;
		private UserLoginDialogResult result = new UserLoginDialogResult {Success = false};
		private string connectionToken;

		private delegate (bool IsSuccess, string Status) DocumentStatusChanged(UserLoginDialog window, JObject json);

		private readonly DocumentStatusChanged _documentStatusChanged = OnDocumentStatusChanged;

		private KeyValuePair<string, CaptureImageStep> _currentStep;

		private readonly List<KeyValuePair<string, CaptureImageStep>> _captureImageSteps =
			(new Dictionary<string, CaptureImageStep>
			{
				{
					"UserDetails", new CaptureImageStep()
					{
						Message = "Please fill all details.",
						Title = "User Details - Step 1/8"
					}
				},
				{
					"ProfilePicFront", new CaptureImageStep()
					{
						Message = "Please come close to camera and take capture image.",
						Title = "Capture Front face image - Step 2/8"
					}
				},
				{
					"ProfilePicLeft", new CaptureImageStep()
					{
						Message = "Please come close to camera and take move your face to left side and capture image.",
						Title = "Capture left side of face image - Step 3/8"
					}
				},
				{
					"ProfilePicRight", new CaptureImageStep()
					{
						Message =
							"Please come close to camera and take move your face to right side and capture image.",
						Title = "Capture right side of face - Step 4/8"
					}
				},
				{
					"ProfilePicDown", new CaptureImageStep()
					{
						Message =
							"Please come close to camera and take move your face/chin to down side and capture image.",
						Title = "Capture face down image - Step 5/8"
					}
				},
				{
					"ProfilePicUp", new CaptureImageStep()
					{
						Message = "Please come close to camera and take move your face to up side and capture image.",
						Title = "Capture up side of face - Step 6/8"
					}
				},
				{
					"AdmitCard", new CaptureImageStep()
					{
						Message = "Please show your admit card for the exam.",
						Title = "Capture Admit card - Step 7/8"
					}
				},
				{
					"GovId", new CaptureImageStep()
					{
						Message = "Please show your personal identity issued by government.",
						Title = "Capture Personal Identity - Step 8/8"
					}
				},
				{
					"UploadData", new CaptureImageStep()
					{
						Message = "Please Upload your information and wait while its being verified.",
						Title = "Upload and Verify"
					}
				},
				{
					"waiting", new CaptureImageStep()
					{
						Message = "Please your profile/document(s) are being verified.",
						Title = "Waiting...."
					}
				}
			}).ToList();

		event WindowClosingEventHandler IWindow.Closing
		{
			add => _closing += value;
			remove => _closing -= value;
		}

		internal UserLoginDialog(ILogger logger, SessionConfiguration session, ConnectionInfo connectionInfo)
		{
			_logger = logger;
			_session = session;
			_connectionInfo = connectionInfo;

			InitializeComponent();
			InitializeDialog("UserDetails");

			DataContext = this;
			Loaded += OnLoaded;

			// DOB.DisplayDateEnd = DateTime.Today;
			DOB.Text = "30-09-2021"; // DateTime.Today.ToShortDateString();
			Username.Text = "9873763134";

			_webCamCapture = new WebcamCapturePresenter(this);
			// CaptureImageButton.Click += OnBtnSnapShotClick;
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
					result.DateOfBirth = DOB.SelectedDate;
					result.FrontFaceImage = _captureImageSteps[1].Value.Image;
					result.LeftSideImage = _captureImageSteps[2].Value.Image;
					result.RightSideImage = _captureImageSteps[3].Value.Image;
					result.DownSideImage = _captureImageSteps[4].Value.Image;
					result.UpSideImage = _captureImageSteps[5].Value.Image;
					result.AdmitCardImage = _captureImageSteps[6].Value.Image;
					result.GovIdCardImage = _captureImageSteps[7].Value.Image;

					result.Success = true;
				}

				return result;
			});
		}

		public void Message(string message, MessageBoxImage messageBoxImage)
		{
			MessageBox.Show(this, message, Title, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
		}

		private void InitializeDialog(string stepKey)
		{
			_currentStep = _captureImageSteps
				.FirstOrDefault(a => a.Key == stepKey);

			Title = _currentStep.Value.Title;
			CbDocuments.Visibility = Visibility.Hidden;

			GroupBoxWaiting.Visibility = Visibility.Hidden;
			GroupBoxLogin.Visibility = Visibility.Hidden;
			GroupBoxImage.Visibility = Visibility.Hidden;
			GroupBoxFinish.Visibility = Visibility.Hidden;

			switch (_currentStep.Key)
			{
				case "UserDetails":
				{
					LoginMessage.Text = _currentStep.Value.Message;
					GroupBoxLogin.Visibility = Visibility.Visible;
					InitializePasswordDialog("User Details");
					break;
				}
				case "ProfilePicFront":
				case "ProfilePicLeft":
				case "ProfilePicRight":
				case "ProfilePicDown":
				case "ProfilePicUp":
				case "AdmitCard":
				case "GovId":
				{
					CaptureImageMessage.Text = _currentStep.Value.Message;
					if (_currentStep.Value.Image == null)
					{
						imgCapture.Source = null;
					}
					else
					{
						Dispatcher.Invoke(() => { imgCapture.Source = GetImage(_currentStep.Value.Image); });
					}

					if (_currentStep.Key == "AdmitCard")
					{
						SupportedDocuments.Clear();
						SupportedDocuments.Add(new RequiredDocument() {Id = -1, DocumentName = "-- Select --"});
						foreach (var document in result.Documents.Where(a => a.IsMandatory).ToList())
						{
							SupportedDocuments.Add(document);
						}

						CbDocuments.Visibility = Visibility.Visible;
						CbDocuments.SelectedIndex = 0;
					}
					else if (_currentStep.Key == "GovId")
					{
						SupportedDocuments.Clear();
						SupportedDocuments.Add(new RequiredDocument() {Id = -1, DocumentName = "-- Select --"});
						foreach (var document in result.Documents.Where(a => a.IsMandatory == false).ToList())
						{
							SupportedDocuments.Add(document);
						}

						CbDocuments.Visibility = Visibility.Visible;
						CbDocuments.SelectedIndex = 0;
					}

					GroupBoxImage.Visibility = Visibility.Visible;
					break;
				}
				case "UploadData":
				{
					GroupBoxFinish.Visibility = Visibility.Visible;
					FinishMessage.Text = _currentStep.Value.Message;
					FinishUsername.Text = $"Email/Mobile: {Username.Text}";
					FinishDob.Text = $"Date of birth: {DOB.Text}";

					Dispatcher.Invoke(() =>
					{
						ImgFront.Source = GetImage(_captureImageSteps[1].Value.Image);
						ImgLeft.Source = GetImage(_captureImageSteps[2].Value.Image);
						ImgRight.Source = GetImage(_captureImageSteps[3].Value.Image);
						ImgDown.Source = GetImage(_captureImageSteps[4].Value.Image);
						ImgUp.Source = GetImage(_captureImageSteps[5].Value.Image);
						ImgAdmitCard.Source = GetImage(_captureImageSteps[6].Value.Image);
						ImgGovId.Source = GetImage(_captureImageSteps[7].Value.Image);
					});

					break;
				}
				case "waiting":
				{
					GroupBoxWaiting.Visibility = Visibility.Visible;
					GroupBoxWaiting.UpdateLayout();
					break;
				}
			}

			tbDocType.Visibility = CbDocuments.Visibility;
			UpdateLayout();
		}

		private void InitializePasswordDialog(string title)
		{
			//LoginMessage.Text = message;
			Title = title;
			WindowStartupLocation = WindowStartupLocation.CenterScreen;

			// LoginCancelButton.Content = text.Get(TextKey.UserLoginDialog_Cancel);
			// LoginCancelButton.Click += CancelButton_Click;

			//ConfirmButton.Content = text.Get(TextKey.UserLoginDialog_Confirm);
			//ConfirmButton.Click += ConfirmButton_Click;

			Closing += (o, args) =>
			{
				Disconnect?.Invoke(this, EventArgs.Empty);
				_closing?.Invoke();
			};

			Loaded += (o, args) => Activate();
			Username.KeyUp += Username_KeyUp;
			DOB.KeyUp += DOB_KeyUp;
		}

		private async Task<bool> FetchServerData()
		{
			using (var client = new HttpClient())
			{
				var dob = DOB.SelectedDate.Value.ToString("dd-MM-yyyy");

				var apiJson = JsonConvert.DeserializeObject(_connectionInfo.Api) as JObject;

				var uri = $"{apiJson.GetValue("ValidateCandidateEndpoint")}?username={Username.Text}&dob={dob}";
				var request = new HttpRequestMessage(HttpMethod.Get, uri);
				// request.Headers.Add("Authorization", $"Bearer {_connectionInfo.Oauth2Token}");

				var response = await client.SendAsync(request); // .GetAwaiter().GetResult();
				if (response.IsSuccessStatusCode)
				{
					TryParseConnectionToken(response, out connectionToken);
					var rawJson = JsonConvert.DeserializeObject(Extract(response.Content)) as JObject;
					var results =
						JsonConvert.DeserializeObject<IEnumerable<UserLoginDialogResult>>(rawJson["data"].ToString());
					if (results.Any())
					{
						if (results.Count() > 1)
						{
							// todo: need to take decision if multiple exams are scheduled
						}
						else
						{
							result = results.FirstOrDefault();
							rawJson = await QueryStatus(); // query document status if documents were already been uploaded.
							if (rawJson != null)
							{
								var (isSuccess, status) = _documentStatusChanged(this, rawJson);
								return status == "REJECTED";
							}
						}
					}
					else
					{
						Message("Sorry, We couldn't find scheduled exam for current date", MessageBoxImage.Exclamation);
						return false;
					}

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
				LoginNextButton.Focus();
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


		#region webcam

		private System.Drawing.Image _actualCamImage;

		private string _selectedVideoDevice;

		private System.Drawing.Image _snapShotImage;

		public event EventHandler Connect;

		public event EventHandler<string> DeviceSelected;

		public event EventHandler Disconnect;

		public event EventHandler Load;

		public event EventHandler<string> ResolutionSelected;

		public event PropertyChangedEventHandler PropertyChanged;

		public event EventHandler SaveSnapShot;

		public event EventHandler SnapShot;

		public System.Drawing.Image ActualCamImage
		{
			get => _actualCamImage;
			set
			{
				_actualCamImage = value;
				Dispatcher.Invoke(() => { imgVideo.Source = GetImage(_actualCamImage); });
				OnPropertyChanged();
			}
		}

		public string SelectedVideoDevice
		{
			get => _selectedVideoDevice;
			set
			{
				_selectedVideoDevice = value;
				OnPropertyChanged();
			}
		}

		public Image SnapShotImage
		{
			get => _snapShotImage;
			set
			{
				_snapShotImage = value;
				imgCapture.Source = GetImage(_snapShotImage);
				OnPropertyChanged();
			}
		}

		public BindingList<string> SupportedFrameSizes { get; set; } = new BindingList<string>();

		public BindingList<RequiredDocument> SupportedDocuments { get; set; } = new BindingList<RequiredDocument>();

		public BindingList<string> VideoDevices { get; set; } = new BindingList<string>();

		public void EnableConnectionControls(bool b)
		{
		}

		public string GetExportPath()
		{
			var dialog = new SaveFileDialog();
			var result = dialog.ShowDialog();
			if (result.HasValue && result.Value)
			{
				return dialog.FileName;
			}

			return null;
		}

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private BitmapImage GetImage(Image image)
		{
			using (var ms = new MemoryStream())
			{
				image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
				ms.Position = 0;
				var bi = new BitmapImage();
				bi.BeginInit();
				bi.CacheOption = BitmapCacheOption.OnLoad;
				bi.StreamSource = ms;
				bi.EndInit();
				return bi;
			}
		}

		private void OnBtnSnapShotClick(object sender, RoutedEventArgs e)
		{
			try
			{
				SnapShot?.Invoke(this, EventArgs.Empty);
				Dispatcher.Invoke(() =>
				{
					imgCapture.Source = GetImage(_snapShotImage);
					_currentStep.Value.Image = (Image) _snapShotImage.Clone();
				});
			}
			catch (Exception ex)
			{
				Message("Please check if camera is working", MessageBoxImage.Exclamation);
			}
		}

		private void OnCbVideoDevicesSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			DeviceSelected?.Invoke(this, (string) CbVideoDevices.SelectedItem);
			Disconnect?.Invoke(this, EventArgs.Empty);

			Connect?.Invoke(this, EventArgs.Empty);
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			Load?.Invoke(this, EventArgs.Empty);
		}

		private void OnCbResolutionsSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ComboBox cb = sender as ComboBox ?? throw new ArgumentException(nameof(sender));
			ResolutionSelected?.Invoke(this, (string) cb.SelectedItem);
		}

		#endregion

		private async Task<bool> UploadData()
		{
			using (var client = new HttpClient())
			{
				client.Timeout = TimeSpan.FromMinutes(30);

				var apiJson = JsonConvert.DeserializeObject(_connectionInfo.Api) as JObject;

				var uri = $"{apiJson.GetValue("CandidateDataUploadEndpoint")}";
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
				request.Headers.Add("SEBConnectionToken", connectionToken);
				// request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("multipart/form-data"));

				var content = new MultipartFormDataContent
				{
					{new StringContent(result.CandidateKey), "CandidateKey"},
					{new StringContent(result.ExamCode), "ExamCode"},
					{new StringContent(result.ExamScheduleKey), "ExamScheduleKey"}
				};

				var docs = new JArray();
				for (int i = 1; i <= 7; i++)
				{
					MemoryStream m = new MemoryStream();
					var step = _captureImageSteps[i];
					var image = step.Value.Image;
					image.Save(m, System.Drawing.Imaging.ImageFormat.Png);
					m.Seek(0, SeekOrigin.Begin);
					var filename = $"{Guid.NewGuid():N}.png";
					var streamContent = new ByteArrayContent(m.ToArray());

					// streamContent.Headers.Add("Content-Type", "application/octet-stream");
					content.Add(streamContent, step.Key, filename);

					if (step.Key == "AdmitCard" || step.Key == "GovId")
					{
						docs.Add(new JObject(new JProperty($"{step.Value.SelectedDocument.Id}", filename)));
					}
				}

				content.Add(new StringContent($"{docs.ToString()}"), "Documents");
				request.Content = content;

				var response = await client.SendAsync(request); //.GetAwaiter().GetResult();
				var responseContent = Extract(response.Content);

				if (response?.IsSuccessStatusCode == true)
				{
					TryParseConnectionToken(response, out connectionToken);

					var rawJson = JsonConvert.DeserializeObject(responseContent) as JObject;
					rawJson = rawJson["data"] as JObject;
					var waitingList = rawJson["waiting_list"].ToObject<int>();
					WaitingBackButton.IsEnabled = false;

					InitializeDialog("waiting");
					_currentStep.Value.Message =
						$"Please wait while your document(s) are being verified. Your token number is {waitingList}";

					await Task.Delay(20);
					
					result.Success = false;
					return true;
				}

				var output = Extract(response.Content);
				_logger.Log(output);
				Message("Error while connecting with server. Please try again", MessageBoxImage.Error);
				return false;
			}
		}

		private async Task<JObject> QueryStatus()
		{
			using (var httpclient = new HttpClient())
			{
				var apiJson = JsonConvert.DeserializeObject(_connectionInfo.Api) as JObject;
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

		private void PreSelectDocumentType()
		{
			if (_currentStep.Key == "AdmitCard" || _currentStep.Key == "GovId")
			{
				var ctr = 0;
				foreach (var document in SupportedDocuments)
				{
					if (document.Id == _currentStep.Value.SelectedDocument.Id)
					{
						CbDocuments.SelectedIndex = ctr;
						break;
					}

					ctr++;
				}
			}
		}
		
		private void FinishBackButton_OnClick(object sender, RoutedEventArgs e)
		{
			InitializeDialog("GovId");
			PreSelectDocumentType();
		}

		private async void UploadButton_OnClick(object sender, RoutedEventArgs e)
		{
			FinishMessage.Text = "Please wait while uploading details to server.";
			try
			{
				if (await UploadData())
				{
					do
					{
						var rawJson = await QueryStatus();
						if (rawJson == null)
						{
							break; //todo: think how to handle transient error(s)
						}
						else
						{
							var (IsSuccess, Status) = _documentStatusChanged(this, rawJson); // trigger event
							if (IsSuccess) break;
						}

						await Task.Delay(TimeSpan.FromSeconds(20));
					} while (true);
				}
			}
			catch (Exception exception)
			{
				_logger.Error(exception.ToString());
				Console.WriteLine(exception);
			}
		}

		private void CaptureImageBackButton_OnClick(object sender, RoutedEventArgs e)
		{
			_currentStep = _captureImageSteps[_captureImageSteps.IndexOf(_currentStep) - 1];
			InitializeDialog(_currentStep.Key);

			PreSelectDocumentType();
		}

		private void CaptureImageNextButton_OnClick(object sender, RoutedEventArgs e)
		{
			if (_currentStep.Value.Image == null)
			{
				Message("Please capture image to complete the step.", MessageBoxImage.Exclamation);
				return;
			}

			if (_currentStep.Key == "AdmitCard" || _currentStep.Key == "GovId")
			{
				if (CbDocuments.SelectedItem is null || CbDocuments.SelectedIndex < -1 ||
				    ((RequiredDocument) CbDocuments.SelectedItem).Id <= 0)
				{
					Message("Kindly select document type to proceed further", MessageBoxImage.Error);
					return;
				}
			}

			_currentStep = _captureImageSteps[_captureImageSteps.IndexOf(_currentStep) + 1];
			InitializeDialog(_currentStep.Key);
		}

		private async void LoginNextButton_OnClick(object sender, RoutedEventArgs e)
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
					InitializeDialog("ProfilePicFront");
				}
			}
			else
			{
				Message(@"Either email\phone is not in correct format. Try again!",
					MessageBoxImage.Error);
			}
		}

		private void CbDocuments_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (CbDocuments.SelectedIndex <= 0) return;
			_currentStep.Value.SelectedDocument = (RequiredDocument) CbDocuments.SelectedItem;

			if (_currentStep.Key == "AdmitCard")
			{
				tbAdmitCard.Text = _currentStep.Value.SelectedDocument.DocumentName;
			}
			else if (_currentStep.Key == "GovId")
			{
				tbPersonalIdentity.Text = _currentStep.Value.SelectedDocument.DocumentName;
			}
		}

		internal bool TryParseConnectionToken(HttpResponseMessage response, out string connectionToken)
		{
			connectionToken = default(string);

			try
			{
				var hasHeader = response.Headers.TryGetValues("SEBConnectionToken", out var values);

				if (hasHeader)
				{
					connectionToken = values.First();
				}
				else
				{
					_logger.Error("Failed to retrieve connection token!");
				}
			}
			catch (Exception e)
			{
				_logger.Error("Failed to parse connection token!", e);
			}

			return connectionToken != default(string);
		}

		private void WaitingBackButton_OnClick(object sender, RoutedEventArgs e)
		{
			InitializeDialog("GovId");
			PreSelectDocumentType();
		}

		private void WaitingCancelButton_OnClick(object sender, RoutedEventArgs e)
		{
			result.Success = (string) WaitingCancelButton.Content == "Finish";
			Close();
		}

		private static (bool IsSuccess, string Status) OnDocumentStatusChanged(UserLoginDialog window, JObject rawJson)
		{
			var status = rawJson["status"].ToObject<string>();
			var flag = true;
			window.Dispatcher.Invoke(() =>
			{
				var currentSTep = window._captureImageSteps.FirstOrDefault(a => a.Key == "waiting");

				if (status == "PENDING")
				{
					var waitingList = rawJson["waiting_list"].ToObject<int>();
					window.WaitingMessage.Text =
						$"Please wait while your document(s) are being verified. Your token number is {waitingList}";
					flag = false;
					window.WaitingBackButton.IsEnabled = false;
				}
				else if (status == "REJECTED")
				{
					var comments = rawJson["comments"].ToObject<string>();
					window.WaitingMessage.Text =
						$"Your document(s) have been Rejected. Please re-upload requested documents";
					window.WaitingMessage.Visibility = Visibility.Visible;
					window.WaitingReject.Text = comments;
					window.WaitingBackButton.IsEnabled = true;
				}
				else if (status == "APPROVED")
				{
					window.WaitingMessage.Text = $"Your document has been approved.";
					window.WaitingCancelButton.Content = "Finish";
				}

				currentSTep.Value.Message = window.WaitingMessage.Text;
				if (window._currentStep.Key != currentSTep.Key && status != "REJECTED")
				{
					window.InitializeDialog(currentSTep.Key);
					window.UpdateLayout();
				}
			});

			return (flag, status);
		}
	}
}