using Clippy.Core.Classes;
using Clippy.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Clippy.Core.Interfaces;
using Clippy.Core.Enums;
using Clippy.Core.ViewModels.Messages;
using System.Collections.ObjectModel;
using Clippy.Core.Factories;
using System.Linq.Expressions;
using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace Clippy.Core.ViewModels
{
	public partial class ClippyViewModel : ObservableObject
	{
		private const string ThinkingCharacterImageFileName = "Dino.png";
		private const string IdleCharacterImageFileName = "Dino.Idle.png";
		private const string HappyCharacterImageFileName = "Dino.Happy.png";
		private const string OllamaEndpoint = "http://localhost:11434";
		private const string RequiredOllamaModel = "phi3:latest";

		public ObservableCollection<MessageViewModel> MessagesVM = new();
		public ObservableCollection<IMessage> Messages = new();

		[ObservableProperty]
        private bool isClippyEnabled = true;

        [ObservableProperty]
        private bool isPinned = true;

		[ObservableProperty]
		private string currentText = "";

		[ObservableProperty]
		private DateTime updatedAt = DateTime.Now;

		[ObservableProperty]
		private string characterImageSource = GetCharacterImageSource(CharacterVisualState.Idle);

		public IChatService ChatService;

        public ISettingsService SettingsService;

        public ClippyViewModel(IChatService chatService, ISettingsService settingsService)
        {
            ChatService = chatService;
            SettingsService = settingsService;
            isPinned = SettingsService.AutoPin;
			SetupChat();
        }

		private void SetupChat()
		{
			AddMessage(new Message(Role.System, Constants.DEFAULT_SYSTEM_PROMPT));
			AddMessage(new Message(Role.Assistant, Constants.FIRST_CLIPPY_MESSAGE));
		}

		private MessageViewModel AddMessage(IMessage message)
		{
			var ViewModel = MessageFactory.GetMessageViewModel(message);
			MessagesVM.Add(ViewModel);
			Messages.Add(message);
			return ViewModel;
		}

		private MessageViewModel AddMessageViewModel(IMessage message)
		{
			var ViewModel = MessageFactory.GetMessageViewModel(message);
			MessagesVM.Add(ViewModel);
			return ViewModel;
		}

		private enum CharacterVisualState
		{
			Idle,
			Thinking,
			Happy
		}

		private static string GetCharacterImageSource(CharacterVisualState state)
		{
			var fileName = state switch
			{
				CharacterVisualState.Idle => IdleCharacterImageFileName,
				CharacterVisualState.Thinking => ThinkingCharacterImageFileName,
				CharacterVisualState.Happy => HappyCharacterImageFileName,
				_ => ThinkingCharacterImageFileName
			};

			var assetPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Dino", fileName);
			if (!File.Exists(assetPath))
				fileName = ThinkingCharacterImageFileName;

			return $"ms-appx:///Assets/Dino/{fileName}";
		}

		private void SetCharacterVisualState(CharacterVisualState state) => CharacterImageSource = GetCharacterImageSource(state);

		[RelayCommand(IncludeCancelCommand = true)]
		public async Task SendPrompt(CancellationToken cancellationToken)
		{
			try
			{
				if (!String.IsNullOrEmpty(CurrentText))
				{
					AddMessage(new Message(Role.User, CurrentText)); // update UI here
					CurrentText = "";
					await Task.Delay(300);

					var messageVM = AddMessageViewModel(new Message(Role.Assistant, "")) as ClippyMessageViewModel;
					SetCharacterVisualState(CharacterVisualState.Thinking);
					UpdatedAt = DateTime.Now;
					messageVM?.StartStreamText(cancellationToken);

					try
					{
						await foreach (var chunk in ChatService.StreamChatAsync(Messages, cancellationToken))
							messageVM?.AddStreamText(chunk);
					}
					catch (TaskCanceledException)
					{
						SetCharacterVisualState(CharacterVisualState.Idle);
						return; // Task cancelled so it is ok
					}
					catch (Exception e)
					{
						var failureMessage = GetChatFailureMessage(e);
						if (messageVM is not null)
						{
							messageVM.EndStreamText();
							messageVM.MessageText = failureMessage;
						}
						else
						{
							AddMessageViewModel(new Message(Role.Assistant, failureMessage));
						}

						SetCharacterVisualState(CharacterVisualState.Idle);
						return;
					}

					messageVM?.EndStreamText();
					if (messageVM is not null && !String.IsNullOrEmpty(messageVM.MessageText))
					{
						Messages.Add(new Message(Role.Assistant, messageVM.MessageText));
						SetCharacterVisualState(CharacterVisualState.Happy);
					}
					else
					{
						SetCharacterVisualState(CharacterVisualState.Idle);
					}
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine(e.Message);
			}
		}

		private static string GetChatFailureMessage(Exception exception)
		{
			var message = exception.Message ?? string.Empty;

			if (ContainsIgnoreCase(message, "404") || ContainsIgnoreCase(message, "not found") || ContainsIgnoreCase(message, "model"))
			{
				return $"I couldn't use the required local Ollama model `{RequiredOllamaModel}`. Open a terminal and run `ollama pull {RequiredOllamaModel}`, then try again.";
			}

			if (exception is HttpRequestException ||
				ContainsIgnoreCase(message, "connection") ||
				ContainsIgnoreCase(message, "refused") ||
				ContainsIgnoreCase(message, "unreachable"))
			{
				return $"I couldn't connect to Ollama at {OllamaEndpoint}. Make sure Ollama is installed and running, then run `ollama pull {RequiredOllamaModel}` before chatting with Blue.";
			}

			return $"I couldn't get a response from Ollama. Make sure Ollama is running at {OllamaEndpoint} and the `{RequiredOllamaModel}` model is installed.";
		}

		private static bool ContainsIgnoreCase(string source, string value) =>
			source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;

		[RelayCommand]
        private void RefreshChat()
		{
			MessagesVM.Clear();
			Messages.Clear();
			SetupChat();
			SetCharacterVisualState(CharacterVisualState.Idle);
		}
    }
}
