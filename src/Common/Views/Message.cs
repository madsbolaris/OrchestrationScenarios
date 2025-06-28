using System;
using Terminal.Gui;

namespace Common.Views;

public class Message
{
	private View _renderedView;
	private View _parentView;
	private string _text = "";
	private string _sender = "";
	private DateTime _sentAt;
	private string? _toolName;
	private string? _toolCallId;
	private string _renderedText = "";

	public int Height => CalculateHeight();
	public int Width => CalculateWidth();

	public Message(View parentView, string text, string sender, DateTime sentAt, Pos position, string? toolName = null, string? toolCallId = null)
	{
		_text = text;
		_sender = sender;
		_sentAt = sentAt;
		_toolName = toolName;
		_toolCallId = toolCallId;
		_parentView = parentView;

		_renderedView = new View()
		{
			X = _sender == "You" ? Pos.Right(_parentView) - Width - 2 : 2,
			Y = position + 1,
			Width = Width - 2,
			Height = Height - 2,
			CanFocus = false,
			ColorScheme = new ColorScheme
			{
				Normal = new Terminal.Gui.Attribute(sender == "You" ? Color.BrightCyan : Color.White, Color.Black)
			},
			Border = new Border
			{
				BorderStyle = BorderStyle.Rounded,
				BorderBrush = sender == "You" ? Color.BrightCyan : Color.White
			}
		};

		UpdateText();
		_parentView.Add(_renderedView);
	}

	private void UpdateText()
	{
		var prefix = _toolCallId == null
			? $"{_sender}:"
			: $"{_toolName} #{_toolCallId}:";

		_renderedText = $"{prefix}\n{_text}";

		_renderedView.Text = _renderedText;
	}

	public void AppendContent(string text)
	{
		_text += text;
		UpdateText();

		_renderedView.Width = Width - 2;
		_renderedView.Height = Height - 2;
		_renderedView.X = _sender == "You" ? Pos.Right(_parentView) - Width - 2 : 2;
	}

	public void Redraw()
	{
		_renderedView.SetNeedsDisplay();
	}

	private int CalculateHeight()
	{
		int height = 3;
		foreach (var line in _text.Split('\n'))
		{
			height++;
			int lineLen = 0;
			foreach (var word in line.Split(' '))
			{
				if (lineLen + word.Length > Width - 2)
				{
					height++;
					lineLen = 0;
				}
				lineLen += word.Length + 1;
			}
		}
		return height;
	}

	private int CalculateWidth()
	{
		int parentWidth = _parentView.Frame.Width;
		int maxAllowedWidth = (int)(parentWidth * 0.75);

		int maxLineLength = _renderedText
			.Split('\n')
			.Select(line => line.Length)
			.DefaultIfEmpty(0)
			.Max();

		// Add 2 for padding/border
		int paddedWidth = maxLineLength + 2;

		// Clamp to max allowed width
		return Math.Min(paddedWidth, maxAllowedWidth);
	}
}
