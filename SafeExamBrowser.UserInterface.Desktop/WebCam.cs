using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Image = System.Drawing.Image;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;

namespace SafeExamBrowser.UserInterface.Desktop
{
	public interface IWebcamCaptureView
	{
		event EventHandler Connect;

		event EventHandler<string> DeviceSelected;

		event EventHandler Disconnect;

		event EventHandler Load;

		event EventHandler<string> ResolutionSelected;

		event EventHandler SaveSnapShot;

		event EventHandler SnapShot;

		Image ActualCamImage { get; set; }

		string SelectedVideoDevice { get; set; }

		Image SnapShotImage { get; set; }

		BindingList<string> SupportedFrameSizes { get; set; }

		BindingList<string> VideoDevices { get; set; }

		void EnableConnectionControls(bool b);

		string GetExportPath();

		void Message(string message, MessageBoxImage messageBoxImage);
	}

	public class WebcamCapturePresenter
	{
		private readonly object _sync = new object();
		private Bitmap _actualFrame;
		private FilterInfoCollection _devices;
		private VideoCaptureDevice _selectedVideoDevice;
		private readonly IWebcamCaptureView _view;

		public WebcamCapturePresenter(IWebcamCaptureView view)
		{
			_view = view;
			view.Load += OnViewLoad;
			view.Connect += OnViewConnect;
			view.DeviceSelected += OnViewDeviceSelected;
			view.SnapShot += OnViewSnapshot;
			view.Disconnect += OnViewDisconnect;
			view.SaveSnapShot += OnViewSaveSnapShot;
			view.ResolutionSelected += OnViewResolutionSelected;
		}

		private void OnViewResolutionSelected(object sender, string e)
		{
			var frameSize = e.Split('x');
			var widht = int.Parse(frameSize[0]);
			var height = int.Parse(frameSize[1]);
			var selectedResolution = _selectedVideoDevice.VideoCapabilities
				.SingleOrDefault(x => x.FrameSize.Height == height && x.FrameSize.Width == widht);
			if (selectedResolution == null)
			{
				_view.Message("Application Error", MessageBoxImage.Error);
			}

			_selectedVideoDevice.VideoResolution = selectedResolution;
		}

		private void OnViewSaveSnapShot(object sender, EventArgs e)
		{
			var path = _view.GetExportPath();
			if (path == null) return;
			try
			{
				_view.SnapShotImage.Save(path);
			}
			catch (Exception exception)
			{
				_view.Message("Error saving image.", MessageBoxImage.Error);
			}
		}

		private void Disconnect()
		{
			_selectedVideoDevice?.SignalToStop();
			// _selectedVideoDevice?.WaitForStop();
			_view.EnableConnectionControls(true);
		}

		private IEnumerable<string> EnumeratedSupportedFrameSizes(string e)
		{
			_selectedVideoDevice = null;
			foreach (FilterInfo filterInfo in _devices)
			{
				if (filterInfo.Name == e)
				{
					_selectedVideoDevice = new VideoCaptureDevice(filterInfo.MonikerString);
					break;
				}
			}

			if (_selectedVideoDevice == null)
			{
				yield break;
			}

			var videoCapabilities = _selectedVideoDevice.VideoCapabilities;
			foreach (VideoCapabilities videoCapability in videoCapabilities)
			{
				yield return $"{videoCapability.FrameSize.Width} x {videoCapability.FrameSize.Height}";
			}
		}

		private void OnVideoDeviceNewFrame(object sender, NewFrameEventArgs eventargs)
		{
			lock (_sync)
			{
				Bitmap image = (Bitmap)eventargs.Frame.Clone();
				if (_actualFrame != null)
				{
					_actualFrame.Dispose();
					_actualFrame = null;
				}

				_actualFrame = image;
			}

			_view.ActualCamImage = _actualFrame;
		}

		private void OnViewConnect(object sender, EventArgs e)
		{
			if (_view.SelectedVideoDevice == "-- Select Camera --") return;
			
			if (_view.SelectedVideoDevice == null)
			{
				_view.Message("No device selected!", MessageBoxImage.Error);
				return;
			}

			_view.EnableConnectionControls(false);
			_selectedVideoDevice.NewFrame += OnVideoDeviceNewFrame;
			_selectedVideoDevice.Start();
		}

		private void OnViewDeviceSelected(object sender, string e)
		{
			if (_view.VideoDevices.Count == 0) return;
			_view.SelectedVideoDevice = e;
			_view.SupportedFrameSizes.Clear();
			foreach (var frameSize in EnumeratedSupportedFrameSizes(e))
			{
				_view.SupportedFrameSizes.Add(frameSize);
			}
		}

		private void OnViewDisconnect(object sender, EventArgs e)
		{
			Disconnect();
		}

		private void OnViewLoad(object sender, EventArgs e)
		{
			var audioDevices = new FilterInfoCollection(FilterCategory.AudioInputDevice);
			_devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
			_view.VideoDevices.Add("-- Select Camera --");
			foreach (FilterInfo filterInfo in _devices)
			{
				_view.VideoDevices.Add(filterInfo.Name);
			}
		}

		private void OnViewSnapshot(object sender, EventArgs e)
		{
			lock (_sync)
			{
				try
				{
					_view.SnapShotImage = (Image)_actualFrame.Clone();
				}
				catch (Exception exception)
				{
					Console.WriteLine(exception);
					throw;
				}
			}
		}
	}

	class Helper
	{
		//Block Memory Leak
		[System.Runtime.InteropServices.DllImport("gdi32.dll")]
		public static extern bool DeleteObject(IntPtr handle);
		public static BitmapSource bs;
		public static IntPtr ip;
		public static BitmapSource LoadBitmap(System.Drawing.Bitmap source)
		{

			ip = source.GetHbitmap();

			bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ip, IntPtr.Zero, System.Windows.Int32Rect.Empty,

				System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

			DeleteObject(ip);

			return bs;

		}
		public static void SaveImageCapture(BitmapSource bitmap)
		{
			JpegBitmapEncoder encoder = new JpegBitmapEncoder();
			encoder.Frames.Add(BitmapFrame.Create(bitmap));
			encoder.QualityLevel = 100;


			// Configure save file dialog box
			Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
			dlg.FileName = "Image"; // Default file name
			dlg.DefaultExt = ".Jpg"; // Default file extension
			dlg.Filter = "Image (.jpg)|*.jpg"; // Filter files by extension

			// Show save file dialog box
			Nullable<bool> result = dlg.ShowDialog();

			// Process save file dialog box results
			if (result == true)
			{
				// Save document
				string filename = dlg.FileName;
				FileStream fstream = new FileStream(filename, FileMode.Create);
				encoder.Save(fstream);
				fstream.Close();
			}

		}
	}
}
