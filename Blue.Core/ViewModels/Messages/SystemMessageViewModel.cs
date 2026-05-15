using Blue.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Blue.Core.ViewModels.Messages
{
	public class SystemMessageViewModel : MessageViewModel
	{
		public SystemMessageViewModel(IMessage message) : base(message)
		{
		}
	}
}
