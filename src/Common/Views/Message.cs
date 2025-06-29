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
	private int _lastComputedWidth = -1;

	private string _lastRenderedText = "";


	public Message(View parentView, string text, string sender, DateTime sentAt, Pos position, string? toolName = null, string? toolCallId = null)
	{
		_text = text;
		_sender = sender;
		_sentAt = sentAt;
		_toolName = toolName;
		_toolCallId = toolCallId;
		_parentView = parentView;

		_renderedView = new View
		{
			Y = position + 1,
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
			}
		};

		_parentView.Add(_renderedView);
		_parentView.LayoutComplete += (_) => UpdateLayout();
		UpdateLayout();
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
			if (string.IsNullOrEmpty(line))
			{
				result.Add("");
				continue;
			}

			var words = line.Split(' ');
			var currentLine = "";

			foreach (var word in words)
			{
				if (word.Length > maxWidth)
				{
					if (!string.IsNullOrEmpty(currentLine))
					{
						result.Add(currentLine);
						currentLine = "";
					}

					for (int i = 0; i < word.Length; i += maxWidth)
					{
						result.Add(word.Substring(i, Math.Min(maxWidth, word.Length - i)));
					}
					continue;
				}

				if ((currentLine.Length + word.Length + (currentLine.Length > 0 ? 1 : 0)) > maxWidth)
				{
					result.Add(currentLine);
					currentLine = word;
				}
				else
				{
					currentLine += (currentLine.Length > 0 ? " " : "") + word;
				}
			}

			if (!string.IsNullOrEmpty(currentLine))
				result.Add(currentLine);
		}
		return result;
	}

	private void UpdateLayout()
	{
		int contentWidth = MaxContentWidth();

		if (contentWidth == _lastComputedWidth && _text == _lastRenderedText)
			return;

		_lastComputedWidth = contentWidth;
		_lastRenderedText = _text;

		UpdateText();

		_renderedView.Text = _renderedText;
		_renderedView.Width = Width - 2;
		_renderedView.Height = Height - 2;
		_renderedView.X = _sender == "User" ? Pos.Right(_parentView) - Width - 2 : 2;

		_renderedView.SetNeedsDisplay();
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
		UpdateLayout();
	}

	public void Redraw()
	{
		_renderedView.SetNeedsDisplay();
	}
	public void SetY(int newY)
	{
		_renderedView.Y = newY + 1;
	}
}
