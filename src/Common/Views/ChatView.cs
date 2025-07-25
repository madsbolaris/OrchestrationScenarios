using System;
using Terminal.Gui;
using Microsoft.Extensions.Logging;
using Terminal.Gui.Views;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;

namespace Common.Views;

public class ChatView : Window
{
	private readonly Action<string>? _onInput;
	private readonly MessageHistory _messageHistory;

	public ChatView(string title, ILoggerFactory loggerFactory, Action<string>? onInput = null)
	{
		Title = title;

		_messageHistory = new MessageHistory(this, loggerFactory);
		_onInput = onInput;

		// Add the Copy XML button
		var copyButton = new Button
		{
			Text = "Copy XML",
			X = Pos.AnchorEnd(12),
			Y = 0,
			CanFocus = true
		};

		copyButton.Accepting += (s, e) =>
		{
			if (Clipboard.IsSupported)
			{
				Clipboard.TrySetClipboardData(_messageHistory.Xml);
				MessageBox.Query("Copied", "Conversation XML copied to clipboard.", buttons: new[] { "OK" });
			}
			else
			{
				MessageBox.ErrorQuery("Clipboard Not Supported", "Terminal clipboard is not available.", "OK");
			}
			e.Handled = true;
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
