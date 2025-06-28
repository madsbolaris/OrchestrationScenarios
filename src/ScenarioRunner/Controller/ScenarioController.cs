using Microsoft.Extensions.Hosting;
using Terminal.Gui;
using ScenarioRunner.Helpers;
using ScenarioRunner.Interfaces;
using Common.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AgentsSdk.Models.Messages.Types;
using AgentsSdk.Models.Messages.Content;

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

        void ShowScenarioSelection()
        {
            var win = new Window()
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

            CancellationTokenSource? cts = null;

            listView.OpenSelectedItem += async args =>
            {
                cts = new CancellationTokenSource();
                var selectedScenario = scenarios[args.Item];
                top.Remove(win);

                var streamWin = new Window()
                {
                    X = 0,
                    Y = 1,
                    Width = Dim.Fill(),
                    Height = Dim.Fill(),
                    Border = flatBorderStyle,
                    ColorScheme = colorScheme
                };

                var loggerFactory = _provider.GetRequiredService<ILoggerFactory>();

                var openAIChatView = new ChatView(
                    onInput: null,
                    title: $"{selectedScenario.Name} (OpenAI)",
                    showInput: false,
                    loggerFactory: loggerFactory
                )
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Percent(50),
                    Height = Dim.Fill(1),
                    ColorScheme = colorScheme
                };

                var copilotChatView = new ChatView(
                    onInput: null,
                    title: $"{selectedScenario.Name} (Copilot Studio)",
                    showInput: false,
                    loggerFactory: loggerFactory
                )
                {
                    X = Pos.Right(openAIChatView),
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = Dim.Fill(1),
                    ColorScheme = colorScheme
                };

                var backButton = new Button("Back")
                {
                    X = Pos.Center(),
                    Y = Pos.AnchorEnd(1),
                    ColorScheme = colorScheme,
                    HotKey = (Key)0,
                    HotKeySpecifier = '\xffff',
                };

                backButton.Clicked += () =>
                {
                    if (cts is { IsCancellationRequested: false })
                    {
                        cts.Cancel();
                        cts.Dispose();
                        cts = null;
                    }

                    top.Remove(streamWin);
                    ShowScenarioSelection();
                };

                streamWin.Add(openAIChatView, copilotChatView, backButton);// Register custom keybindings for focus and escape
                
                // Track cycleable views
                var focusableViews = new List<View> { openAIChatView, copilotChatView, backButton };
                var currentFocusIndex = 0;
                focusableViews[currentFocusIndex].SetFocus();

                // Register keybindings
                streamWin.KeyPress += (e) =>
                {
                    if (e.KeyEvent.Key == Key.CursorRight)
                    {
                        currentFocusIndex = (currentFocusIndex + 1) % focusableViews.Count;
                        focusableViews[currentFocusIndex].SetFocus();
                        e.Handled = true;
                    }
                    else if (e.KeyEvent.Key == Key.CursorLeft)
                    {
                        currentFocusIndex = (currentFocusIndex - 1 + focusableViews.Count) % focusableViews.Count;
                        focusableViews[currentFocusIndex].SetFocus();
                        e.Handled = true;
                    }
                    else if (e.KeyEvent.Key == Key.Esc)
                    {
                        backButton.OnClicked();
                        e.Handled = true;
                    }
                };

                top.Add(streamWin);
                Application.Refresh();

                var startingMessages = selectedScenario.GetStartingMessages();

                Application.MainLoop.Invoke(() =>
                {
                    foreach (var message in startingMessages)
                    {
                        switch (message)
                        {
                            case UserMessage:
                                openAIChatView.StartMessage("User");
                                copilotChatView.StartMessage("User");
                                break;
                            case AgentMessage:
                                openAIChatView.StartMessage("Agent");
                                copilotChatView.StartMessage("Agent");
                                break;
                            case ToolMessage tool:
                                openAIChatView.StartMessage("Tool", tool.ToolType, tool.ToolCallId);
                                copilotChatView.StartMessage("Tool", tool.ToolType, tool.ToolCallId);
                                break;
                        }

                        foreach (var content in message.Content)
                        {
                            switch (content)
                            {
                                case TextContent text:
                                    openAIChatView.AppendToLastMessage(text.Text);
                                    copilotChatView.AppendToLastMessage(text.Text);
                                    break;
                                case ToolCallContent call:
                                    var header = $"{call.Name} #{call.ToolCallId}\n";
                                    openAIChatView.AppendToLastMessage(header);
                                    copilotChatView.AppendToLastMessage(header);
                                    break;
                                case ToolResultContent result:
                                    var resultText = result.Results?.ToString() ?? "";
                                    openAIChatView.AppendToLastMessage(resultText);
                                    copilotChatView.AppendToLastMessage(resultText);
                                    break;
                            }
                        }
                    }
                });

                _ = Task.Run(async () =>
                {
                    try
                    {
                        var copilotStream = selectedScenario.RunCopilotStudioStream(cts.Token);
                        var openAIStream = selectedScenario.RunOpenAIStream(cts.Token);

                        await Task.WhenAll(
                            ChatRenderHelper.DisplayStreamToChatViewAsync(copilotStream, copilotChatView),
                            ChatRenderHelper.DisplayStreamToChatViewAsync(openAIStream, openAIChatView)
                        );
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        Application.MainLoop.Invoke(() => MessageBox.ErrorQuery("Error", ex.Message, "Ok"));
                    }
                });
            };

            win.Add(listView);
            top.Add(win);
            Application.Refresh();
            Application.MainLoop.Invoke(() => listView.SetFocus());
        }

        ShowScenarioSelection();

        Application.Top.ColorScheme = colorScheme;
        Application.Run();
    }

}
