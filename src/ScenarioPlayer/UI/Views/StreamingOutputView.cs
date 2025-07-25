using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace ScenarioPlayer.UI.Views;

public class StreamingOutputView : FrameView
{
    private readonly TextView _logView;

    public StreamingOutputView() : base()
    {
        _logView = new TextView
        {
            Text = "Agent Output",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true
        };

        Add(_logView);
    }

    public void AppendLine(string line)
    {
        Application.Invoke(() =>
        {
            _logView.Text += line + "\n";
            // _logView.ScrollToEnd();
        });
    }

    public void AppendRaw(string text)
    {
        Application.Invoke(() =>
        {
            _logView.Text += text;
            // _logView.ScrollToEnd();
        });
    }

    public void ClearText()
    {
        Application.Invoke(() =>
        {
            _logView.Text = "";
        });
    }
}
