using Terminal.Gui;
using ScenarioPlayer.Core;
using ScenarioPlayer.Core.Services;
using Common.Views;
using Microsoft.Extensions.Logging;
using ScenarioPlayer.UI.Rendering;
using Terminal.Gui.App;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using System.Collections.ObjectModel;

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
        var top = new Toplevel
        {
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        void ShowScenarioSelection()
        {
            var win = new Window
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            var listView = new ListView
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(1),
                CanFocus = true
            };

            var scenarioNames = new ObservableCollection<string>(scenarios.Select(s => s.Name));
            listView.SetSource<string>(scenarioNames);

            listView.OpenSelectedItem += async (s, args) =>
            {
                var selectedScenario = scenarios[args.Item];
                top.Remove(win);

                var streamWin = new Window
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = Dim.Fill()
                };

                var openAIChatView = CreateChatView("OpenAI", $"{selectedScenario.Name} (OpenAI)");
                // var copilotChatView = CreateChatView("CopilotStudio", $"{selectedScenario.Name} (Copilot Studio)");

                var backButton = new Button
                {
                    Text = "Back",
                    X = Pos.Center(),
                    Y = Pos.AnchorEnd(1),
                    CanFocus = true
                };

                backButton.Accepting += (s, e) =>
                {
                    top.Remove(streamWin);
                    ShowScenarioSelection();
                    e.Handled = true;
                };

                streamWin.Add(openAIChatView, /*copilotChatView,*/ backButton);
                top.Add(streamWin);

                foreach (var message in selectedScenario.StartingMessages)
                {
                    ChatRenderHelper.RenderStaticMessage(message, openAIChatView);
                    // ChatRenderHelper.RenderStaticMessage(message, copilotChatView);
                }

                _ = Task.Run(async () =>
                {
                    var streams = _executor.RunScenario(selectedScenario, cancellationToken);

                    await Task.WhenAll(
                        ChatRenderHelper.DisplayStreamToChatViewAsync(streams["OpenAI"], openAIChatView)
                        // ChatRenderHelper.DisplayStreamToChatViewAsync(streams["CopilotStudio"], copilotChatView)
                    );
                });

                await Task.CompletedTask;
            };

            win.Add(listView);
            top.Add(win);

            Application.Invoke(() => listView.SetFocus());
        }

        ChatView CreateChatView(string label, string title) =>
            new(title, _loggerFactory)
            {
                X = label == "OpenAI" ? 0 : Pos.Percent(50),
                Y = 0,
                Width = label == "OpenAI" ? Dim.Percent(50) : Dim.Fill(),
                Height = Dim.Fill(1),
                CanFocus = true
            };

        ShowScenarioSelection();

        Application.Run(top);
        top.Dispose();

        await Task.CompletedTask;
    }

}
