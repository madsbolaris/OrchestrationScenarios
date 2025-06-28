using System;
using System.Collections.Generic;
using System.Linq;
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

	public int Width => CalculateWidth();
	public int Height => CalculateHeight();

	public Message(View parentView, string text, string sender, DateTime sentAt, Pos position, string? toolName = null, string? toolCallId = null)
	{
		_text = text;
		_sender = sender;
		_sentAt = sentAt;
		_toolName = toolName;
		_toolCallId = toolCallId;
		_parentView = parentView;

		UpdateText();

		_renderedView = new View
		{
			X = _sender == "User" ? Pos.Right(_parentView) - Width - 2 : 2,
			Y = position + 1,
			Width = Width - 2,
			Height = Height - 2,
			CanFocus = false,
			ColorScheme = new ColorScheme
			{
				Normal = new Terminal.Gui.Attribute(
					_sender == "User" ? Color.BrightCyan : Color.White,
					Color.Black
				)
			},
			Border = new Border
			{
				BorderStyle = BorderStyle.Rounded,
				BorderBrush = _sender == "User" ? Color.BrightCyan : Color.White
			},
			Text = _renderedText
		};

		_parentView.Add(_renderedView);
	}

	private void UpdateText()
	{
		string prefix = _toolCallId == null
			? $"{_sender}:"
			: $"{_toolName} #{_toolCallId}:";

		int contentWidth = MaxContentWidth();
		var wrappedLines = WrapLines(new[] { prefix }.Concat(_text.Split('\n')), contentWidth);
		_renderedText = string.Join('\n', wrappedLines);
	}

	private List<string> WrapLines(IEnumerable<string> lines, int maxWidth)
	{
		var result = new List<string>();
		foreach (var line in lines)
		{
			int i = 0;
			while (i < line.Length)
			{
				int len = Math.Min(maxWidth, line.Length - i);
				result.Add(line.Substring(i, len));
				i += len;
			}

			// For empty lines
			if (line.Length == 0)
				result.Add("");
		}
		return result;
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

		return Math.Min(maxLineLength + 2, maxAllowedWidth);
	}

	private int CalculateHeight()
	{
		return _renderedText.Split('\n').Length + 2;
	}

	private int MaxContentWidth()
	{
		int parentWidth = _parentView.Frame.Width;
		return (int)(parentWidth * 0.75) - 4; // minus border + padding
	}

	public void AppendContent(string text)
	{
		_text += text;
		UpdateText();

		_renderedView.Text = _renderedText;
		_renderedView.Width = Width - 2;
		_renderedView.Height = Height - 2;
		_renderedView.X = _sender == "User" ? Pos.Right(_parentView) - Width - 2 : 2;
	}

	public void Redraw()
	{
		_renderedView.SetNeedsDisplay();
	}
}
