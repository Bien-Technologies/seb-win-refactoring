using System;
using SafeExamBrowser.Communication.Contracts.Data;
using SafeExamBrowser.Configuration.Contracts;
using SafeExamBrowser.Core.Contracts.OperationModel.Events;
using SafeExamBrowser.Server.Contracts.Data;
using SafeExamBrowser.UserInterface.Contracts.MessageBox;

namespace SafeExamBrowser.Runtime.Operations.Events
{
	internal class UserLoginRequiredEventArgs: ActionRequiredEventArgs
	{
		public string Username { get; set; }
		
		public DateTime? DateOfBirth { get; set; }

		public bool Success { get; set; }

		public string Name { get; set; }
		
		public string RollNO { get; set; }
		
		public string CompanyKey { get; set; }
		
		public string CompanyName { get; set; }
		
		public string CandidateKey { get; set; }
		
		public SessionConfiguration SessionContext { get; set; }
		
		public ConnectionInfo ConnectionInfo { get; set; }
	}
	
	internal class UserImagesEventArgs: ActionRequiredEventArgs
	{
		public string Username { get; set; }
		
		public DateTime? DateOfBirth { get; set; }

		public bool Success { get; set; }
		
		public string ExamCode { get; set; }
		
		public string ScheduledExamCode { get; set; }
		
		public DateTime? ExamDate { get; set; }
		
		public string ExamTime { get; set; }
		
		public int Duration { get; set; }
		
		public string Topic { get; set; }
		
		public string CandidateName { get; set; }
		
		public string CompanyKey { get; set; }
		
		public string CompanyName { get; set; }

		public string CandidateKey { get; set; }
		
		public SessionConfiguration SessionContext { get; set; }
		
		public ConnectionInfo ConnectionInfo { get; set; }

		public Exam SelectedExam { get; set; }
	}
}