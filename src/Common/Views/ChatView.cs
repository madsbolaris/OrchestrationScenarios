using System;
using Terminal.Gui;
using Microsoft.Extensions.Logging;

namespace Common.Views;

public class ChatView : Window
{
	private readonly Action<string>? _onInput;
	private readonly MessageHistory _messageHistory;

	public ChatView(string title, ILoggerFactory loggerFactory, Action<string>? onInput = null)
	{
		Title = title;
		ColorScheme = new ColorScheme { Normal = new Terminal.Gui.Attribute(Color.White, Color.Black) };

		_messageHistory = new MessageHistory(this, loggerFactory);
		_onInput = onInput;

		// Always add the Copy XML button in the top-right corner
		var copyButton = new Button("Copy XML")
		{
			X = Pos.AnchorEnd(12), // 12 = button width for alignment; adjust if label changes
			Y = 0,
		};

		copyButton.Clicked += () =>
		{
			if (Clipboard.IsSupported)
			{
				Clipboard.Contents = _messageHistory.Xml;
				MessageBox.Query("Copied", "Conversation XML copied to clipboard.", "OK");
			}
			else
			{
				MessageBox.ErrorQuery("Clipboard Not Supported", "Terminal clipboard is not available.", "OK");
			}
		};

		Add(copyButton);

		// Optionally add input field if enabled
		if (_onInput != null)
		{
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

	public void AppendToLastMessage(string content)
	{
		_messageHistory.AppendToLastMessage(content);
	}

	public MessageHistory MessageHistory => _messageHistory;
}
