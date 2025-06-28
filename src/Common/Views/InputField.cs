using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Terminal.Gui;

namespace Common.Views;

public class InputField
{
	private View _renderedView;
	private TextField _inputWindow;
	private Action<string> _onEnter;
	private View _parentView;

	private Label _label;

	public InputField(View parentView, Action<string> onEnter, Pos Y)
	{
		_parentView = parentView;

		// Create a frame view that will hold the text field
		_renderedView = new FrameView()
		{
			X = 0,
			Y = Y + 1,
			Width = Dim.Fill(),
			Height = 3, // Increase the height to accommodate the border
			Border = new Border()
			{
				BorderStyle = BorderStyle.Single,
				BorderBrush = Color.White
			},
		};

		// Create label for the input window
		_label = new Label("Enter your message:")
		{
			X = 1,
			Y = Y,
			Width = Dim.Fill(),
			Height = 1,
			TextAlignment = TextAlignment.Left,
			ColorScheme = new ColorScheme
			{
				Normal = new Terminal.Gui.Attribute(Color.White, Color.Black)
			}
		};
		_parentView.Add(_label);

		// Create the input window
		_inputWindow = new TextField("")
		{
			Y = 0,
			Width = Dim.Fill() - 6,
			Height = 1
		};

		_inputWindow.KeyPress += (args) =>
		{
			if (args.KeyEvent.Key == Key.Enter && _inputWindow.Text.Length > 0)
			{
				AddMessage();
			}
		};
		_renderedView.Add(_inputWindow);

		_parentView.Add(_renderedView);

		_onEnter = onEnter;
	}

	public void Redraw()
	{
		//_parentView.Redraw(_renderedView.Bounds);
	}

	public void SetFocus()
	{
		if (_inputWindow != null)
		{
			//_inputWindow.SetFocus();
		}
	}

	private void AddMessage()
	{
		if (_inputWindow != null && _inputWindow.Text.Length > 0)
		{
			if (_onEnter != null)
			{
				_onEnter(_inputWindow.Text.ToString()!);
			}

			_inputWindow.Text = string.Empty; // Clear the input window
		}
	}
}