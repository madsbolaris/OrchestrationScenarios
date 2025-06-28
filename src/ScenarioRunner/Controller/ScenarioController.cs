using Microsoft.Extensions.Hosting;
using Terminal.Gui;
using ScenarioRunner.Views;
using ScenarioRunner.Helpers;

namespace ScenarioRunner.Controllers;

public class ScenarioController
{
    private readonly IServiceProvider _provider;
    private readonly IHostApplicationLifetime _lifetime;

    public ScenarioController(IServiceProvider provider, IHostApplicationLifetime lifetime)
    {
        _provider = provider;
        _lifetime = lifetime;
    }

    public async Task StartAsync(List<IScenario> scenarios)
    {
        var top = Application.Top;

        void ShowScenarioSelection()
        {
            var win = new Window("Select Scenario")
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            var listView = new ListView(scenarios.Select(s => s.Name).ToList())
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(1)
            };

            CancellationTokenSource? cts = null;

            listView.OpenSelectedItem += async args =>
            {
                cts = new CancellationTokenSource();
                var selectedScenario = scenarios[args.Item];
                top.Remove(win);

                var output = new StreamingOutputView
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = Dim.Fill()
                };

                top.Add(output);
                Application.Refresh();

                var backButton = new Button("Back")
                {
                    X = Pos.Center(),
                    Y = Pos.AnchorEnd(1)
                };

                backButton.Clicked += () =>
                {
                    if (cts != null && !cts.IsCancellationRequested)
                    {
                        cts.Cancel();
                        cts.Dispose();
                        cts = null;
                    }

                    top.Remove(output);
                    top.Remove(backButton);
                    ShowScenarioSelection();
                };


                top.Add(backButton);
                Application.Refresh();

                try
                {
                    var stream = selectedScenario.RunCopilotStudioStream(cts.Token);
                    await TerminalRenderHelper.DisplayStreamAsync(stream, output);
                }
                catch (OperationCanceledException) { }
                finally
                {
                    if (cts != null)
                    {
                        cts.Dispose();
                        cts = null;
                    }
                }
            };
            
            win.Add(listView);
            Application.Top.Add(win);
            Application.Refresh();
            Application.MainLoop.Invoke(() => listView.SetFocus());
        }

        ShowScenarioSelection();
        Application.Run();
    }
}
