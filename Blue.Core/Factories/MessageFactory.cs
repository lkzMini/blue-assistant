using Blue.Core.Interfaces;
using Blue.Core.ViewModels.Messages;
using Blue.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Blue.Core.Factories
{
	public class MessageFactory
	{
		public static MessageViewModel GetMessageViewModel(IMessage message)
		{
			switch (message.Role)
			{
				case Enums.Role.User:
					return new UserMessageViewModel(message);
				case Enums.Role.Assistant:
					return new AssistantMessageViewModel(message);
				default:
					return new SystemMessageViewModel(message);
			}
		}
	}
}
