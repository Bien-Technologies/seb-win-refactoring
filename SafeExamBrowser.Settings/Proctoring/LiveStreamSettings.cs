using System;

namespace SafeExamBrowser.Settings.Proctoring
{
	/// <summary>
	/// All settings for the meeting provider Custom live streaming.
	/// </summary>
	[Serializable]
	public class LiveStreamSettings
	{
		/// <summary>
		/// Determines whether the user can use the chat.
		/// </summary>
		public bool AllowChat { get; set; }

		/// <summary>
		/// Determines whether the user can use close captions.
		/// </summary>
		public bool AllowCloseCaptions { get; set; }

		/// <summary>
		/// Determines whether the user can use the raise hand feature.
		/// </summary>
		public bool AllowRaiseHand { get; set; }

		/// <summary>
		/// Determines whether the user can record the meeting.
		/// </summary>
		public bool AllowRecording { get; set; }

		/// <summary>
		/// Determines whether the audio starts muted.
		/// </summary>
		public bool AudioMuted { get; set; }

		/// <summary>
		/// Determines whether the meeting runs in an audio-only mode.
		/// </summary>
		public bool AudioOnly { get; set; }

		/// <summary>
		/// Determines whether proctoring with Jitsi Meet is enabled.
		/// </summary>
		public bool Enabled { get; set; } = true;

		/// <summary>
		/// Determines whether the user may receive the video stream of other meeting participants.
		/// </summary>
		public bool ReceiveAudio { get; set; }

		/// <summary>
		/// Determines whether the user may receive the audio stream of other meeting participants.
		/// </summary>
		public bool ReceiveVideo { get; set; }

		/// <summary>
		/// The name of the meeting room.
		/// </summary>
		public string RoomName { get; set; } = "bienexam";

		/// <summary>
		/// Determines whether the audio stream of the user will be sent to the server.
		/// </summary>
		public bool SendAudio { get; set; }

		/// <summary>
		/// Determines whether the video stream of the user will be sent to the server.
		/// </summary>
		public bool SendVideo { get; set; }

		/// <summary>
		/// The URL of the Jitsi Meet server.
		/// </summary>
		public string ServerUrl { get; set; } = "wss://inteliexcel.in:5443/WebRTCApp/websocket";

		/// <summary>
		/// The subject of the meeting.
		/// </summary>
		public string Subject { get; set; }

		/// <summary>
		/// The authentication token for the meeting.
		/// </summary>
		public string Token { get; set; }

		/// <summary>
		/// Determines whether the video starts muted.
		/// </summary>
		public bool VideoMuted { get; set; }
		
		/// <summary>
		/// The number of the meeting.
		/// </summary>
		public int MeetingNumber { get; set; }

		/// <summary>
		/// The user name to be used for the meeting.
		/// </summary>
		public string UserName { get; set; }
	}
}