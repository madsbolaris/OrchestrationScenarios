using System;
using Terminal.Gui;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Common.Views;

public class InputField
{
    readonly View _renderedView;
    readonly TextField _inputWindow;
    readonly Action<string> _onEnter;
    readonly View _parentView;
    readonly Label _label;

    public InputField(View parentView, Action<string> onEnter, Pos Y)
    {
        _parentView = parentView;
        _onEnter = onEnter;

        _label = new Label()
        {
			Text = "Enter your message:",
            X = 1,
            Y = Y,
            Width = Dim.Fill(),
            Height = 1,
            CanFocus = false
        };
        _parentView.Add(_label);

        _renderedView = new FrameView()
        {
            X = 0,
            Y = Y + 1,
            Width = Dim.Fill(),
            Height = 3,
            BorderStyle = LineStyle.Single
        };

        _inputWindow = new TextField()
        {
            X = 1,
            Y = 0,
            Width = Dim.Fill()! - 2,
            Height = 1
        };

        _inputWindow.KeyDown += (s, args) =>
        {
            if (args.KeyCode == Key.Enter && _inputWindow.Text.Length > 0)
            {
                AddMessage();
                args.Handled = true;
            }
        };

        _renderedView.Add(_inputWindow);
        _parentView.Add(_renderedView);
    }

    public void Redraw() => _renderedView.SetNeedsDraw();

    public void SetFocus() => _inputWindow?.SetFocus();

    void AddMessage()
    {
        if (_inputWindow.Text.Length > 0)
        {
            _onEnter?.Invoke(_inputWindow.Text.ToString()!);
            _inputWindow.Text = "";
        }
    }
}
