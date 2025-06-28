using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using Microsoft.Extensions.Logging;

namespace Common.Views;

public class MessageHistory
{
	public List<Message> Messages { get; set; } = [];
	public Pos Bottom => Pos.Bottom(_renderedView);

	private readonly ScrollView _renderedView;
	private readonly View _parentView;
	private readonly ILogger _logger;

	public ScrollView ScrollView => _renderedView;

	private readonly Queue<Action> _scrollOperationQueue = new();
	private bool _scrollFlushScheduled = false;
	private bool _shouldAutoScroll = false;

	public MessageHistory(View parentView, ILoggerFactory loggerFactory)
	{
		_logger = loggerFactory.CreateLogger<MessageHistory>();
		_parentView = parentView;

		_renderedView = new ScrollView
		{
			X = 0,
			Y = 0,
			Width = Dim.Fill(),
			Height = Dim.Fill(),
			ContentSize = new Size(0, 0),
		};

		parentView.Add(_renderedView);

		// Queue initial layout
		QueueScrollOp(() => UpdateLayoutAndScroll(autoScroll: true));
	}

	public void StartMessage(string sender, string? toolName = null, string? toolCallId = null)
	{
		var wantsToScroll = IsScrolledToBottom() || _scrollFlushScheduled;
		if (wantsToScroll)
			_shouldAutoScroll = true;

		QueueScrollOp(() =>
		{
			var top = Messages.Sum(m => m.Height);
			var message = new Message(_renderedView, "", sender, DateTime.Now, top, toolName, toolCallId);
			Messages.Add(message);
		});
	}

	public void AppendToLastMessage(string text)
	{
		var wantsToScroll = IsScrolledToBottom() || _scrollFlushScheduled;
		if (wantsToScroll)
			_shouldAutoScroll = true;

		QueueScrollOp(() =>
		{
			if (Messages.Count == 0)
				return;

			Messages[^1].AppendContent(text);
		});
	}

	private void QueueScrollOp(Action op)
	{
		_scrollOperationQueue.Enqueue(op);

		if (_scrollFlushScheduled)
			return;

		_scrollFlushScheduled = true;

		Application.MainLoop.Invoke(() =>
		{
			while (_scrollOperationQueue.Count > 0)
			{
				var next = _scrollOperationQueue.Dequeue();
				next();
			}

			UpdateLayoutAndScroll(_shouldAutoScroll);
			_shouldAutoScroll = false;
			_scrollFlushScheduled = false;
		});
	}

	private bool IsScrolledToBottom()
	{
		int visibleBottom = _renderedView.ContentOffset.Y + _renderedView.Bounds.Height;
		int contentHeight = Messages.Sum(m => m.Height);
		return visibleBottom >= contentHeight - 1;
	}

	private void UpdateLayoutAndScroll(bool autoScroll)
	{
		int totalHeight = Messages.Sum(m => m.Height);
		int contentHeight = Math.Max(totalHeight, _parentView.Frame.Height - 6);

		_renderedView.ContentSize = new Size(_parentView.Frame.Width - 3, contentHeight);

		if (autoScroll)
		{
			int newOffsetY = Math.Max(0, contentHeight - _renderedView.Bounds.Height);
			_renderedView.ContentOffset = new Point(0, newOffsetY);
			_renderedView.SetNeedsDisplay();

			_logger.LogDebug("Auto-scrolled to bottom. OffsetY={OffsetY}, ContentHeight={ContentHeight}, ViewportHeight={ViewportHeight}",
				newOffsetY, contentHeight, _renderedView.Bounds.Height);
		}
		else
		{
			_logger.LogDebug("Skipped auto-scroll (user scrolled up).");
		}
	}
}
