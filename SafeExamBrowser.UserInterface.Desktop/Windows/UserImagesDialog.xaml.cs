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
	internal partial class UserImagesDialog : Window, IUserImagesDialog, IWebcamCaptureView
	{
		private readonly WebcamCapturePresenter webCamCapture;
		private readonly ILogger logger;
		private readonly SessionConfiguration session;
		private WindowClosingEventHandler closing;
		public DateTime? AvailableFrom { get; set; } = DateTime.Today;
		private readonly ConnectionInfo connectionInfo;
		private string connectionToken;
		private delegate (bool IsSuccess, string Status) DocumentStatusChanged(UserImagesDialog window, JObject json);

		private Exam selectedExam;
		
		private readonly DocumentStatusChanged documentStatusChanged = OnDocumentStatusChanged;
		private UserImagesDialogResult result = new UserImagesDialogResult();
		private KeyValuePair<string, CaptureImageStep> currentStep;

		private readonly List<KeyValuePair<string, CaptureImageStep>> captureImageSteps =
			(new Dictionary<string, CaptureImageStep>
			{
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
						Message = "Please wait while your profile/document(s) are being verified.",
						Title = "Waiting...."
					}
				}
			}).ToList();

		event WindowClosingEventHandler IWindow.Closing
		{
			add => closing += value;
			remove => closing -= value;
		}

		internal UserImagesDialog(ILogger logger, SessionConfiguration session, ConnectionInfo connectionInfo, string candidateKey, string scheduledExamCode)
		{
			this.connectionInfo = connectionInfo;

			result = new UserImagesDialogResult()
			{
				Success = false,
				CandidateKey = candidateKey,
				ExamScheduleKey = scheduledExamCode
			};
			
			this.logger = logger;
			this.session = session;
			
			InitializeComponent();
			InitializeDialog("ProfilePicFront");

			DataContext = this;
			Loaded += OnLoaded;

			webCamCapture = new WebcamCapturePresenter(this);
			// CaptureImageButton.Click += OnBtnSnapShotClick;
		}

		public void BringToForeground()
		{
			Dispatcher.Invoke(Activate);
		}

		public UserImagesDialogResult Show(IWindow parent = null, object args = null)
		{
			selectedExam = (Exam) args;
			result.Documents = JsonConvert.DeserializeObject<IEnumerable<RequiredDocument>>(selectedExam.DocumentsJson);

			var str = JsonConvert.SerializeObject(session.Settings);
			
			return Dispatcher.Invoke(() =>
			{
				if (parent is Window window)
				{
					Owner = window;
					WindowStartupLocation = WindowStartupLocation.CenterOwner;
				}

				InitializePasswordDialog("");
				
				if (ShowDialog() is true)
				{
					result.FrontFaceImage = captureImageSteps[0].Value.Image;
					result.LeftSideImage = captureImageSteps[1].Value.Image;
					result.RightSideImage = captureImageSteps[2].Value.Image;
					result.DownSideImage = captureImageSteps[3].Value.Image;
					result.UpSideImage = captureImageSteps[4].Value.Image;
					result.AdmitCardImage = captureImageSteps[5].Value.Image;
					result.GovIdCardImage = captureImageSteps[6].Value.Image;

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
			currentStep = captureImageSteps
				.FirstOrDefault(a => a.Key == stepKey);

			Title = currentStep.Value.Title;
			CbDocuments.Visibility = Visibility.Hidden;

			GroupBoxWaiting.Visibility = Visibility.Hidden;
			GroupBoxImage.Visibility = Visibility.Hidden;
			GroupBoxFinish.Visibility = Visibility.Hidden;

			switch (currentStep.Key)
			{
				case "ProfilePicFront":
				case "ProfilePicLeft":
				case "ProfilePicRight":
				case "ProfilePicDown":
				case "ProfilePicUp":
				case "AdmitCard":
				case "GovId":
				{
					CaptureImageMessage.Text = currentStep.Value.Message;
					if (currentStep.Value.Image == null)
					{
						imgCapture.Source = null;
					}
					else
					{
						Dispatcher.Invoke(() => { imgCapture.Source = GetImage(currentStep.Value.Image); });
					}

					if (currentStep.Key == "AdmitCard")
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
					else if (currentStep.Key == "GovId")
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
					FinishMessage.Text = currentStep.Value.Message;

					Dispatcher.Invoke(() =>
					{
						ImgFront.Source = GetImage(captureImageSteps[0].Value.Image);
						ImgLeft.Source = GetImage(captureImageSteps[1].Value.Image);
						ImgRight.Source = GetImage(captureImageSteps[2].Value.Image);
						ImgDown.Source = GetImage(captureImageSteps[3].Value.Image);
						ImgUp.Source = GetImage(captureImageSteps[4].Value.Image);
						ImgAdmitCard.Source = GetImage(captureImageSteps[5].Value.Image);
						ImgGovId.Source = GetImage(captureImageSteps[6].Value.Image);
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
				closing?.Invoke();
			};

			Loaded += (o, args) => Activate();
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
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
			var diagResult = dialog.ShowDialog();
			if (diagResult.HasValue && diagResult.Value)
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
					currentStep.Value.Image = (Image) _snapShotImage.Clone();
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

				var apiJson = JsonConvert.DeserializeObject(connectionInfo.Api) as JObject;

				var uri = $"{apiJson.GetValue("CandidateDataUploadEndpoint")}";
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
				request.Headers.Add("SEBConnectionToken", connectionToken);
				// request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("multipart/form-data"));

				var content = new MultipartFormDataContent
				{
					{new StringContent(result.CandidateKey), "CandidateKey"},
					{new StringContent(result.ExamScheduleKey), "ExamScheduleKey"}
				};

				var docs = new JArray();
				for (int i = 0; i <= 6; i++)
				{
					MemoryStream m = new MemoryStream();
					var step = captureImageSteps[i];
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
					currentStep.Value.Message =
						$"Please wait while your document(s) are being verified. Your token number is {waitingList}";

					await Task.Delay(20);
					
					result.Success = false;
					return true;
				}

				var output = Extract(response.Content);
				logger.Log(output);
				Message("Error while connecting with server. Please try again", MessageBoxImage.Error);
				return false;
			}
		}

		private string Extract(HttpContent content)
		{
			var task = Task.Run(async () => await content.ReadAsStreamAsync());
			var stream = task.GetAwaiter().GetResult();
			var reader = new StreamReader(stream);

			return reader.ReadToEnd();
		}
		
		private async Task<JObject> QueryStatus()
		{
			using (var httpclient = new HttpClient())
			{
				var apiJson = JsonConvert.DeserializeObject(connectionInfo.Api) as JObject;
				var uri =
					$"{apiJson.GetValue("QueryUploadedDataStatusEndpoint")}/{result.CandidateKey}/{result.ExamScheduleKey}";
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
			if (currentStep.Key == "AdmitCard" || currentStep.Key == "GovId")
			{
				var ctr = 0;
				foreach (var document in SupportedDocuments)
				{
					if (document.Id == currentStep.Value.SelectedDocument.Id)
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
			var oldMessage = FinishMessage.Text;
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
							var (IsSuccess, Status) = documentStatusChanged(this, rawJson); // trigger event
							if (IsSuccess) break;
						}

						await Task.Delay(TimeSpan.FromSeconds(20));
					} while (true);
				}
			}
			catch (Exception exception)
			{
				FinishMessage.Text = oldMessage;
				logger.Error(exception.ToString());
				Console.WriteLine(exception);
				Message("Oops! Failed to upload data to server. Please try again or contact to Company", MessageBoxImage.Exclamation);
			}
		}

		private void CaptureImageBackButton_OnClick(object sender, RoutedEventArgs e)
		{
			currentStep = captureImageSteps[captureImageSteps.IndexOf(currentStep) - 1];
			InitializeDialog(currentStep.Key);

			PreSelectDocumentType();
		}

		private void CaptureImageNextButton_OnClick(object sender, RoutedEventArgs e)
		{
			if (currentStep.Value.Image == null)
			{
				Message("Please capture image to complete the step.", MessageBoxImage.Exclamation);
				return;
			}

			if (currentStep.Key == "AdmitCard" || currentStep.Key == "GovId")
			{
				if (CbDocuments.SelectedItem is null || CbDocuments.SelectedIndex < -1 ||
				    ((RequiredDocument) CbDocuments.SelectedItem).Id <= 0)
				{
					Message("Kindly select document type to proceed further", MessageBoxImage.Error);
					return;
				}
			}

			currentStep = captureImageSteps[captureImageSteps.IndexOf(currentStep) + 1];
			InitializeDialog(currentStep.Key);
		}

		private void CbDocuments_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (CbDocuments.SelectedIndex <= 0) return;
			currentStep.Value.SelectedDocument = (RequiredDocument) CbDocuments.SelectedItem;

			if (currentStep.Key == "AdmitCard")
			{
				tbAdmitCard.Text = currentStep.Value.SelectedDocument.DocumentName;
			}
			else if (currentStep.Key == "GovId")
			{
				tbPersonalIdentity.Text = currentStep.Value.SelectedDocument.DocumentName;
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
					logger.Error("Failed to retrieve connection token!");
				}
			}
			catch (Exception e)
			{
				logger.Error("Failed to parse connection token!", e);
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

		private static (bool IsSuccess, string Status) OnDocumentStatusChanged(UserImagesDialog window, JObject rawJson)
		{
			var status = rawJson["status"].ToObject<string>();
			var flag = true;
			window.Dispatcher.Invoke(() =>
			{
				var currentSTep = window.captureImageSteps.FirstOrDefault(a => a.Key == "waiting");
				
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
				if (window.currentStep.Key != currentSTep.Key && status != "REJECTED")
				{
					window.InitializeDialog(currentSTep.Key);
					window.UpdateLayout();
				}
			});

			return (flag, status);
		}
	}
}