using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terminal.Gui;
using Microsoft.Extensions.Logging;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Input;
using Terminal.Gui.App;

namespace Common.Views;

public class MessageHistory
{
    public List<Message> Messages { get; set; } = [];
    public Pos Bottom => Pos.Bottom(_scrollContainer);

    private readonly ScrollingView _scrollContainer;
    private readonly View _parentView;
    private readonly ILogger _logger;

    private readonly StringBuilder _xmlBuilder = new();
    public string Xml => _xmlBuilder.ToString();
    public void AppendXml(string xml) => _xmlBuilder.Append(xml);
    public void ResetXml() => _xmlBuilder.Clear();

    private readonly Queue<Action> _scrollOperationQueue = new();
    private bool _scrollFlushScheduled = false;
    private bool _shouldAutoScroll = false;

    public View ScrollContainer => _scrollContainer;

    public MessageHistory(View parentView, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<MessageHistory>();
        _parentView = parentView;

        _scrollContainer = new ScrollingView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        parentView.Add(_scrollContainer);

        QueueScrollOp(() => UpdateLayoutAndScroll(autoScroll: true));
    }

    public void StartMessage(string sender, string? toolName = null, string? toolCallId = null)
    {
        if (IsScrolledToBottom())
            _shouldAutoScroll = true;

        QueueScrollOp(() =>
        {
            var top = Messages.Sum(m => m.Height);
            var message = new Message(_scrollContainer, "", sender, DateTime.Now, top, toolName, toolCallId);
            Messages.Add(message);
            RepositionMessages();
        });
    }

    public void AppendToLastMessage(string text)
    {
        if (IsScrolledToBottom())
            _shouldAutoScroll = true;

        QueueScrollOp(() =>
        {
            if (Messages.Count == 0)
                return;

            Messages[^1].AppendContent(text);
            RepositionMessages();
        });
    }

    private void QueueScrollOp(Action op)
    {
        _scrollOperationQueue.Enqueue(op);

        if (_scrollFlushScheduled)
            return;

        _scrollFlushScheduled = true;

        Application.Invoke(() =>
        {
            while (_scrollOperationQueue.Count > 0)
            {
                var next = _scrollOperationQueue.Dequeue();
                next?.Invoke();
            }

            UpdateLayoutAndScroll(_shouldAutoScroll);
            _shouldAutoScroll = false;
            _scrollFlushScheduled = false;
        });
    }

    private bool IsScrolledToBottom()
    {
        var visibleBottom = _scrollContainer.Viewport.Location.Y + _scrollContainer.Viewport.Size.Height;
        var contentHeight = Messages.Sum(m => m.Height);
        return visibleBottom >= contentHeight - 1;
    }

    private void UpdateLayoutAndScroll(bool autoScroll)
    {
        // RepositionMessages();

        // int totalHeight = Messages.Sum(m => m.Height);
        // _scrollContainer.ContentSize = new Size(_scrollContainer.Bounds.Width, totalHeight);

        // if (autoScroll)
        // {
        //     _scrollContainer.ScrollTo(new Point(0, Math.Max(0, totalHeight - _scrollContainer.Bounds.Height)));
        //     _logger.LogDebug("Auto-scrolled to bottom. OffsetY={OffsetY}, ContentHeight={ContentHeight}, ViewportHeight={ViewportHeight}",
        //         _scrollContainer.Viewport.Location.Y, totalHeight, _scrollContainer.Bounds.Height);
        // }
        // else
        // {
        //     _logger.LogDebug("Skipped auto-scroll (user scrolled up).");
        // }
    }

    private void RepositionMessages()
    {
        int y = 0;
        foreach (var message in Messages)
        {
            message.SetY(y);
            y += message.Height;
        }
    }

    // ✅ Custom view that handles scroll keys & mouse wheel
    private class ScrollingView : View
    {
        public ScrollingView()
        {
            CanFocus = true;
            VerticalScrollBar.Visible = true;
            VerticalScrollBar.AutoShow = true;
            HorizontalScrollBar.Visible = false;

            MouseEvent += (s, me) =>
            {
                if (me.Flags.HasFlag(MouseFlags.WheeledDown))
                {
                    ScrollVertical(1);
                    me.Handled = true;
                }
                else if (me.Flags.HasFlag(MouseFlags.WheeledUp))
                {
                    ScrollVertical(-1);
                    me.Handled = true;
                }
            };
        }
    }
}
