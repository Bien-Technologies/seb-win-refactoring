using System;
using System.Collections.Generic;

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

		public string CandidateKey { get; set; }
		
		public string Name { get; set; }
		
		public string RollNO { get; set; }
		
		public string CompanyKey { get; set; }
		
		public string CompanyName { get; set; }
		
		public string HasPendingDocuments { get; set; }
	}

	public class RequiredDocument
	{
		public int Id { get; set; }
		
		public bool IsMandatory { get; set; }
		
		public string DocumentName { get; set; }
	}
	
	public class UserImagesDialogResult : UserLoginDialogResult
	{

		public System.Drawing.Image FrontFaceImage { get; set; }
		
		public System.Drawing.Image LeftSideImage { get; set; }
		
		public System.Drawing.Image RightSideImage { get; set; }
		
		public System.Drawing.Image UpSideImage { get; set; }
		
		public System.Drawing.Image DownSideImage { get; set; }
		
		public System.Drawing.Image AdmitCardImage { get; set; }
		
		public System.Drawing.Image GovIdCardImage { get; set; }
		
		public string ExamCode { get; set; }
		
		public string ExamScheduleKey { get; set; }
		
		public DateTime ExamDate { get; set; }
		
		public string ExamTime { get; set; }
		
		public int Duration { get; set; }
		
		public string Topic { get; set; }

		public IEnumerable<RequiredDocument> Documents { get; set; }
	}
}