using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Blue.Core.ViewModels.Messages
{
	public partial class AssistantMessageViewModel
	{
		[ObservableProperty]
		private string streamingText = "";

		[ObservableProperty]
		private bool isStreaming = false;

		private StringBuilder _streamBuffer = new();
		private Task? _typingTask;
		private CancellationTokenSource? _internalCts;
		private string TotalText = "";

		/// <summary>
		/// Starts the typing animation loop.
		/// </summary>
		public void StartStreamText(CancellationToken cancellationToken, int batchSize = 3, int delayMs = 15)
		{
			IsStreaming = true;

			// Create a linked CTS so EndStreamText can cancel the typing loop
			_internalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			var internalToken = _internalCts.Token;

			_typingTask = Task.Run(async () =>
			{
				try
				{
					while (!internalToken.IsCancellationRequested)
					{
						if (_streamBuffer.Length >= batchSize)
						{
							string chunk = _streamBuffer.ToString(0, batchSize);
							_streamBuffer.Remove(0, batchSize);

							UIThread?.Invoke(() => StreamingText += chunk);

							await Task.Delay(delayMs, internalToken);
						}
						else
						{
							await Task.Delay(10, internalToken);
						}
					}
				}
				catch (TaskCanceledException) { }
				catch (OperationCanceledException) { }
			});
		}

		/// <summary>
		/// Adds new text to the animation buffer.
		/// </summary>
		public void AddStreamText(string text)
		{
			TotalText += text;
			_streamBuffer.Append(text);
		}

		/// <summary>
		/// Ends the typing stream and flushes remaining text instantly.
		/// </summary>
		public void EndStreamText()
		{
			// Cancel the internal typing loop first
			_internalCts?.Cancel();
			_internalCts?.Dispose();
			_internalCts = null;

			if (_streamBuffer.Length > 0)
			{
				StreamingText += _streamBuffer.ToString();
				_streamBuffer.Clear();
			}
			MessageText = TotalText;
			TotalText = "";
			IsStreaming = false;
		}
	}
}
