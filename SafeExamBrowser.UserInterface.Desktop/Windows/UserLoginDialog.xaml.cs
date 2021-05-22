using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
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
	}

	// SelectedDate="{Binding Path= AvailableFrom, Mode=TwoWay, TargetNullValue={x:Static System:DateTime.Today}}"
	internal partial class UserLoginDialog : Window, IUserLoginDialog, IWebcamCaptureView
	{
		private readonly WebcamCapturePresenter _webCamCapture;
		private IText _text;
		private WindowClosingEventHandler _closing;
		public DateTime? AvailableFrom { get; set; } = DateTime.Today;

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
						Message = "Please close to camera and take capture image.",
						Title = "Capture Front face image - Step 2/8"
					}
				},
				{
					"ProfilePicLeft", new CaptureImageStep()
					{
						Message = "Please close to camera and take move your face to left side and capture image.",
						Title = "Capture left side of face image - Step 3/8"
					}
				},
				{
					"ProfilePicRight", new CaptureImageStep()
					{
						Message = "Please close to camera and take move your face to right side and capture image.",
						Title = "Capture right side of face - Step 4/8"
					}
				},
				{
					"ProfilePicDown", new CaptureImageStep()
					{
						Message = "Please close to camera and take move your face/chin to down side and capture image.",
						Title = "Capture face down image - Step 5/8"
					}
				},
				{
					"ProfilePicUp", new CaptureImageStep()
					{
						Message = "Please close to camera and take move your face to up side and capture image.",
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
				}
			}).ToList();

		event WindowClosingEventHandler IWindow.Closing
		{
			add => _closing += value;
			remove => _closing -= value;
		}

		internal UserLoginDialog(string message, string title, IText text)
		{
			this._text = text;
			Title = title;
			
			InitializeComponent();
			InitializeDialog("UserDetails");

			DataContext = this;
			Loaded += OnLoaded;

			DOB.DisplayDateEnd = DateTime.Today;
			DOB.Text = DateTime.Today.ToShortDateString();
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
				var result = new UserLoginDialogResult {Success = false};

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
			MessageBox.Show(message, Title, MessageBoxButton.OK, MessageBoxImage.Error);
		}
		
		private void InitializeDialog(string stepKey)
		{
			_currentStep = _captureImageSteps
				.FirstOrDefault(a => a.Key == stepKey);

			CaptureImageMessage.Text = _currentStep.Value.Message;
			Title = _currentStep.Value.Title;

			switch (_currentStep.Key)
			{
				case "UserDetails":
				{
					LoginMessage.Text = _currentStep.Value.Message;
					GroupBoxLogin.Visibility = Visibility.Visible;
					GroupBoxImage.Visibility = Visibility.Hidden;
					GroupBoxFinish.Visibility = Visibility.Hidden;
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
					if (_currentStep.Value.Image == null)
					{
						imgCapture.Source = null;	
					}
					else
					{
						Dispatcher.Invoke(() =>
						{
							imgCapture.Source = GetImage(_currentStep.Value.Image);
						});
					}
					
					GroupBoxLogin.Visibility = Visibility.Hidden;
					GroupBoxImage.Visibility = Visibility.Visible;
					GroupBoxFinish.Visibility = Visibility.Hidden;
					break;
				}
				case "UploadData":
				{
					GroupBoxLogin.Visibility = Visibility.Hidden;
					GroupBoxImage.Visibility = Visibility.Hidden;
					GroupBoxFinish.Visibility = Visibility.Visible;
					FinishMessage.Text = _currentStep.Value.Message;
					FinishUsername.Text = $"Email/Mobile: {Username.Text}";
					FinishDob.Text = $"Date of birth: {DOB.Text}";

					Dispatcher.Invoke(() =>
					{
						ImgFront.Source = GetImage(_captureImageSteps[1].Value.Image);
					});

					Dispatcher.Invoke(() =>
					{
						ImgLeft.Source = GetImage(_captureImageSteps[2].Value.Image);
					});
					Dispatcher.Invoke(() =>
					{
						ImgRight.Source = GetImage(_captureImageSteps[3].Value.Image);
					});
					Dispatcher.Invoke(() =>
					{
						ImgDown.Source = GetImage(_captureImageSteps[4].Value.Image);
					});
					Dispatcher.Invoke(() =>
					{
						ImgUp.Source = GetImage(_captureImageSteps[5].Value.Image);
					});
					Dispatcher.Invoke(() =>
					{
						ImgAdmitCard.Source = GetImage(_captureImageSteps[6].Value.Image);
					});
					Dispatcher.Invoke(() =>
					{
						ImgGovId.Source = GetImage(_captureImageSteps[7].Value.Image);
					});
					
					break;
				}
			}
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

		private void FinishBackButton_OnClick(object sender, RoutedEventArgs e)
		{
			InitializeDialog("GovId");
		}

		private void UploadButton_OnClick(object sender, RoutedEventArgs e)
		{
			FinishMessage.Text = "Please wait while uploading details to server.";
			
			// DialogResult = true;
			// Close();
		}

		private void CaptureImageBackButton_OnClick(object sender, RoutedEventArgs e)
		{
			_currentStep = _captureImageSteps[_captureImageSteps.IndexOf(_currentStep) - 1];
			InitializeDialog(_currentStep.Key);
		}

		private void CaptureImageNextButton_OnClick(object sender, RoutedEventArgs e)
		{
			if (_currentStep.Value.Image == null)
			{
				Message("Please capture image to complete the step.", MessageBoxImage.Exclamation);
				return;
			}

			_currentStep = _captureImageSteps[_captureImageSteps.IndexOf(_currentStep) + 1];
			InitializeDialog(_currentStep.Key);
		}

		private void LoginNextButton_OnClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(Username.Text) || string.IsNullOrWhiteSpace(DOB.Text))
			{
				Message("Please fill all details", MessageBoxImage.Error);
				return;
			}

			if (IsValidPhone(Username.Text) || IsValidEmail(Username.Text))
			{
				InitializeDialog("ProfilePicFront");
			}
			else
			{
				Message(@"Either email\phone is not in correct format. Try again!",
					MessageBoxImage.Error);
			}
		}
	}
}