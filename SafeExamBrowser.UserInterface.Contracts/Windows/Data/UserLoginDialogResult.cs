using System;

namespace SafeExamBrowser.UserInterface.Contracts.Windows.Data
{
	/// <summary>
	/// Defines the user interaction result of an <see cref="IUserLoginDialog"/>.
	/// </summary>
	public class UserLoginDialogResult
	{
		/// <summary>
		/// The username e.g. email/mobile entered by the user, or <c>null</c> if the interaction was unsuccessful.
		/// </summary>
		public string Username { get; set; }
		
		public DateTime? DateOfBirth { get; set; }

		/// <summary>
		/// Indicates whether the user confirmed the dialog or not.
		/// </summary>
		public bool Success { get; set; }

		public System.Drawing.Image FrontFaceImage { get; set; }
		
		public System.Drawing.Image LeftSideImage { get; set; }
		
		public System.Drawing.Image RightSideImage { get; set; }
		
		public System.Drawing.Image UpSideImage { get; set; }
		
		public System.Drawing.Image DownSideImage { get; set; }
		
		public System.Drawing.Image AdmitCardImage { get; set; }
		
		public System.Drawing.Image GovIdCardImage { get; set; }
	}
}