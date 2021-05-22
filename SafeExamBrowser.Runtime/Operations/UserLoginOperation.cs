using SafeExamBrowser.Core.Contracts.OperationModel;
using SafeExamBrowser.Core.Contracts.OperationModel.Events;
using SafeExamBrowser.I18n.Contracts;
using SafeExamBrowser.Logging.Contracts;
using SafeExamBrowser.Runtime.Operations.Events;

namespace SafeExamBrowser.Runtime.Operations
{
	internal class UserLoginOperation  : SessionOperation
	{
		private readonly ILogger logger;
		
		internal UserLoginOperation(ILogger logger, SessionContext context) : base(context)
		{
			this.logger = logger;
		}

		public override event ActionRequiredEventHandler ActionRequired;
		
		public override event StatusChangedEventHandler StatusChanged;
		
		public override OperationResult Perform()
		{
			if (Context.Next.Settings.Proctoring.Enabled)
			{
				return ShowUserLoginDialog();
			}

			logger.Info("Remote proctoring is disabled, skipping disclaimer.");

			return OperationResult.Success;
		}

		public override OperationResult Revert()
		{
			return OperationResult.Success;
		}

		public override OperationResult Repeat()
		{
			if (Context.Next.Settings.Proctoring.Enabled)
			{
				return ShowUserLoginDialog();
			}

			logger.Info("Remote proctoring is disabled, skipping disclaimer.");

			return OperationResult.Success;
		}
		
		private OperationResult ShowUserLoginDialog()
		{
			var args = new UserLoginRequiredEventArgs();
			StatusChanged?.Invoke(TextKey.UserLoginDialog_Title);
			ActionRequired?.Invoke(args);

			if (args.Success)
			{
				logger.Info("The user confirmed.");
				return OperationResult.Success;
			}
			else
			{
				logger.Warn("The user did not confirm the credentials! Aborting session initialization...");

				return OperationResult.Aborted;
			}
		}
	}
}