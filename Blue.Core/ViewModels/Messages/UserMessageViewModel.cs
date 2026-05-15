using Blue.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Blue.Core.ViewModels.Messages
{
	public class UserMessageViewModel : MessageViewModel
	{
		public UserMessageViewModel(IMessage message) : base(message)
		{
		}
	}
}
