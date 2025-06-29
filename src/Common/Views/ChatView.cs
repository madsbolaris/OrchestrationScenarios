using Terminal.Gui;

namespace Common.Views;

using Microsoft.Extensions.Logging; // Add this at the top

public class ChatView : Window
{
	private readonly Action<string> _onInput;
	private readonly MessageHistory _messageHistory;

	public ChatView(string title, ILoggerFactory loggerFactory, Action<string>? onInput = null)
	{
		Title = title;
		ColorScheme = new ColorScheme { Normal = new Terminal.Gui.Attribute(Color.White, Color.Black) };

		_messageHistory = new MessageHistory(this, loggerFactory);

		if (onInput != null)
		{
			_onInput += onInput;
			var inputField = new InputField(this, input =>
			{
				_messageHistory.StartMessage("You");
				_messageHistory.AppendToLastMessage(input);
				_onInput.Invoke(input);
			}, _messageHistory.Bottom);

			inputField.SetFocus();
		}
	}


	public void StartMessage(string sender, string? toolName = null, string? toolCallId = null)
	{
		_messageHistory.StartMessage(sender, toolName, toolCallId);
	}

	public void AppendToLastMessage(string text)
	{
		_messageHistory.AppendToLastMessage(text);
	}
}
