using Terminal.Gui;
using ScenarioPlayer.Core;
using ScenarioPlayer.Core.Services;
using Common.Views;
using Microsoft.Extensions.Logging;
using ScenarioPlayer.UI.Rendering;

namespace ScenarioPlayer.UI;

public class ScenarioUIController
{
    private readonly IScenarioManager _scenarioManager;
    private readonly ScenarioExecutor _executor;
    private readonly ILoggerFactory _loggerFactory;

    public ScenarioUIController(
        IScenarioManager scenarioManager,
        ScenarioExecutor executor,
        ILoggerFactory loggerFactory)
    {
        _scenarioManager = scenarioManager;
        _executor = executor;
        _loggerFactory = loggerFactory;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var scenarios = _scenarioManager.GetAllScenarios().OrderBy(s => s.Name).ToList();

        Application.Init();

        var colorScheme = new ColorScheme
        {
            Normal = new Terminal.Gui.Attribute(Color.White, Color.Black),
            Focus = new Terminal.Gui.Attribute(Color.Black, Color.Gray),
            HotNormal = new Terminal.Gui.Attribute(Color.BrightBlue, Color.Black),
            HotFocus = new Terminal.Gui.Attribute(Color.Black, Color.BrightBlue)
        };

        var flatBorderStyle = new Border
        {
            BorderStyle = BorderStyle.None,
            Effect3D = false,
            DrawMarginFrame = false
        };

        Application.Top.ColorScheme = colorScheme;

        var top = Application.Top;
        ShowScenarioSelection();

        Application.Run();

        void ShowScenarioSelection()
        {
            var win = new Window
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ColorScheme = colorScheme
            };

            var listView = new ListView(scenarios.Select(s => s.Name).ToList())
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(1),
                ColorScheme = colorScheme
            };

            listView.OpenSelectedItem += async args =>
            {
                var selectedScenario = scenarios[args.Item];

                top.Remove(win);

                var streamWin = new Window
                {
                    X = 0,
                    Y = 1,
                    Width = Dim.Fill(),
                    Height = Dim.Fill(),
                    Border = flatBorderStyle,
                    ColorScheme = colorScheme
                };

                var openAIChatView = CreateChatView("OpenAI", $"{selectedScenario.Name} (OpenAI)");
                var copilotChatView = CreateChatView("CopilotStudio", $"{selectedScenario.Name} (Copilot Studio)");

                var backButton = new Button("Back")
                {
                    X = Pos.Center(),
                    Y = Pos.AnchorEnd(1),
                    ColorScheme = colorScheme,
                    HotKey = (Key)0,
                    HotKeySpecifier = '\xffff',
                };

                var focusable = new List<View> { openAIChatView, copilotChatView, backButton };
                var focusIndex = 0;
                focusable[focusIndex].SetFocus();

                backButton.Clicked += () =>
                {
                    top.Remove(streamWin);
                    ShowScenarioSelection();
                };

                streamWin.KeyPress += (e) =>
                {
                    if (e.KeyEvent.Key == Key.CursorRight)
                    {
                        focusIndex = (focusIndex + 1) % focusable.Count;
                        focusable[focusIndex].SetFocus();
                        e.Handled = true;
                    }
                    else if (e.KeyEvent.Key == Key.CursorLeft)
                    {
                        focusIndex = (focusIndex - 1 + focusable.Count) % focusable.Count;
                        focusable[focusIndex].SetFocus();
                        e.Handled = true;
                    }
                    else if (e.KeyEvent.Key == Key.Esc)
                    {
                        backButton.OnClicked();
                        e.Handled = true;
                    }
                };

                streamWin.Add(openAIChatView, copilotChatView, backButton);
                top.Add(streamWin);
                Application.Refresh();

                foreach (var message in selectedScenario.StartingMessages)
                {
                    ChatRenderHelper.RenderStaticMessage(message, openAIChatView);
                    ChatRenderHelper.RenderStaticMessage(message, copilotChatView);
                }

                _ = Task.Run(async () =>
                {
                    var streams = _executor.RunScenario(selectedScenario, cancellationToken);

                    await Task.WhenAll(
                        ChatRenderHelper.DisplayStreamToChatViewAsync(streams["OpenAI"], openAIChatView),
                        ChatRenderHelper.DisplayStreamToChatViewAsync(streams["CopilotStudio"], copilotChatView)
                    );
                });

                await Task.CompletedTask;
            };

            win.Add(listView);
            top.Add(win);
            Application.Refresh();
            Application.MainLoop.Invoke(() => listView.SetFocus());
        }

        ChatView CreateChatView(string label, string title) =>
            new(title, _loggerFactory)
            {
                X = label == "OpenAI" ? 0 : Pos.Percent(50),
                Y = 0,
                Width = label == "OpenAI" ? Dim.Percent(50) : Dim.Fill(),
                Height = Dim.Fill(1),
                ColorScheme = colorScheme
            };

        await Task.CompletedTask;
    }
}
