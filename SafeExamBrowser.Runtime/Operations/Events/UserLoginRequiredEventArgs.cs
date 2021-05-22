using System;
using SafeExamBrowser.Communication.Contracts.Data;
using SafeExamBrowser.Core.Contracts.OperationModel.Events;
using SafeExamBrowser.UserInterface.Contracts.MessageBox;

namespace SafeExamBrowser.Runtime.Operations.Events
{
	internal class UserLoginRequiredEventArgs: ActionRequiredEventArgs
	{
		public string Username { get; set; }
		
		public DateTime? DateOfBirth { get; set; }

		public bool Success { get; set; }
	}
}