using SafeExamBrowser.UserInterface.Contracts.Windows.Data;

namespace SafeExamBrowser.UserInterface.Contracts.Windows
{
	public interface IUserLoginDialog : IWindow
	{
		/// <summary>
		/// Shows the dialog as topmost window. If a parent window is specified, the dialog is rendered modally for the given parent.
		/// </summary>
		UserLoginDialogResult Show(IWindow parent = null);
	}
	
	public interface IUserImagesDialog : IWindow
	{
		/// <summary>
		/// Shows the dialog as topmost window. If a parent window is specified, the dialog is rendered modally for the given parent.
		/// </summary>
		UserImagesDialogResult Show(IWindow parent = null, object args = null);
	}
}