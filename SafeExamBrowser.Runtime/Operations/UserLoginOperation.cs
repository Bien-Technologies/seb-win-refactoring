using System;
using SafeExamBrowser.Configuration.Contracts;
using SafeExamBrowser.Core.Contracts.OperationModel;
using SafeExamBrowser.Core.Contracts.OperationModel.Events;
using SafeExamBrowser.I18n.Contracts;
using SafeExamBrowser.Logging.Contracts;
using SafeExamBrowser.Runtime.Operations.Events;
using SafeExamBrowser.Server.Contracts;
using SafeExamBrowser.Server.Contracts.Data;
using SafeExamBrowser.Settings.Proctoring;

namespace SafeExamBrowser.Runtime.Operations
{
	internal class UserLoginOperation  : ConfigurationBaseOperation
	{
		private readonly ILogger logger;
		private readonly IServerProxy server;
		private ConnectionInfo _connectionInfo;

		internal UserLoginOperation(ILogger logger,
			string[] commandLineArgs,
			IConfigurationRepository configuration, 
			SessionContext context,
			IServerProxy server) : base(commandLineArgs, configuration, context)
		{
			this.logger = logger;
			this.server = server;
		}

		public override event ActionRequiredEventHandler ActionRequired;
		
		public override event StatusChangedEventHandler StatusChanged;
		
		public override OperationResult Perform()
		{
			if (Context.Next.Settings.Proctoring.Enabled)
			{
				
				server.Initialize(Context.Next.Settings.Server);

				var (abort, fallback, success) = TryPerformWithFallback(() => server.Connect());
				if (success)
				{
					_connectionInfo = server.GetConnectionInfo();
					
					return ShowUserLoginDialog();
				}
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
			var args = new UserLoginRequiredEventArgs() {SessionContext = Context.Next, ConnectionInfo = _connectionInfo};
			StatusChanged?.Invoke(TextKey.UserLoginDialog_Title);
			ActionRequired?.Invoke(args);

			if (args.Success)
			{
				logger.Info("The user confirmed.");
				Context.Next.Settings.Proctoring.Candidate = new CandidateModel()
				{
					Duration = args.Duration,
					Topic = args.Topic,
					Username = args.Username,
					CandidateKey = args.CandidateKey,
					CandidateName = args.CandidateName,
					ExamCode = args.ExamCode,
					ExamDate = args.ExamDate,
					ExamTime = args.ExamTime,
					DateOfBirth = args.DateOfBirth
				};
				return OperationResult.Success;
			}
			else
			{
				logger.Warn("The user did not confirm the credentials! Aborting session initialization...");

				return OperationResult.Aborted;
			}
		}

		protected override void InvokeActionRequired(ActionRequiredEventArgs args)
		{
			throw new System.NotImplementedException();
		}
		
		private (bool abort, bool fallback, bool success) TryPerformWithFallback(Func<ServerResponse> request)
		{
			var abort = false;
			var fallback = false;
			var success = false;

			while (!success)
			{
				var response = request();

				success = response.Success;

				if (!success && !Retry(response.Message, out abort, out fallback))
				{
					break;
				}
			}

			return (abort, fallback, success);
		}

		private (bool abort, bool fallback, bool success) TryPerformWithFallback<T>(Func<ServerResponse<T>> request, out T value)
		{
			var abort = false;
			var fallback = false;
			var success = false;

			value = default(T);

			while (!success)
			{
				var response = request();

				success = response.Success;
				value = response.Value;

				if (!success && !Retry(response.Message, out abort, out fallback))
				{
					break;
				}
			}

			return (abort, fallback, success);
		}

		private bool Retry(string message, out bool abort, out bool fallback)
		{
			var args = new ServerFailureEventArgs(message, Context.Next.Settings.Server.PerformFallback);

			ActionRequired?.Invoke(args);

			abort = args.Abort;
			fallback = args.Fallback;

			return args.Retry;
		}
	}
}