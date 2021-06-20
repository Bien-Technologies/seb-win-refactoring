using System;

namespace SafeExamBrowser.Settings.Proctoring
{
	public class CandidateModel
	{
		public string Username { get; set; }
		
		public DateTime? DateOfBirth { get; set; }

		public string ExamCode { get; set; }
		
		public DateTime ExamDate { get; set; }
		
		public string ExamTime { get; set; }
		
		public int Duration { get; set; }
		
		public string Topic { get; set; }
		
		public string CandidateName { get; set; }
		
		public string CandidateKey { get; set; }
	}
}