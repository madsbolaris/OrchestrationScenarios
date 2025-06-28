using Terminal.Gui;

namespace ScenarioRunner.Views;

public class StreamingOutputView : FrameView
{
    private readonly TextView _logView;

    public StreamingOutputView() : base("Agent Output")
    {
        _logView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true,
            Multiline = true
        };

        Add(_logView);
    }

    public void AppendLine(string line)
    {
        Application.MainLoop.Invoke(() =>
        {
            _logView.Text += line + "\n";
            _logView.MoveEnd();
        });
    }

    public void AppendRaw(string text)
    {
        Application.MainLoop.Invoke(() =>
        {
            _logView.Text += text;
            _logView.MoveEnd();
        });
    }

    public void Clear()
    {
        Application.MainLoop.Invoke(() => _logView.Text = "");
    }
} 
