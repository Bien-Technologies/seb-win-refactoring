/*
 * Copyright (c) 2021 ETH Zürich, Educational Development and Technology (LET)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;

namespace SafeExamBrowser.Server.Contracts.Data
{
	/// <summary>
	/// Defines a server exam.
	/// </summary>
	public class Exam
	{
		/// <summary>
		/// The identifier of the exam.
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// The name of the learning management system (LMS) on which the exam is running.
		/// </summary>
		public string LmsName { get; set; }

		/// <summary>
		/// The name of the exam.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The URL of the exam.
		/// </summary>
		public string Url { get; set; }
		
		public DateTime? ScheduleDate { get; set; }
		
		public string ExamCode { get; set; }
		
		public int Duration { get; set; }
		
		public string ExamTime { get; set; }
		
		public string DocumentsJson { get; set; }
	}
}
