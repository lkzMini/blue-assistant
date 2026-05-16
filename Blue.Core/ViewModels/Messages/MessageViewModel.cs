using CommunityToolkit.Mvvm.ComponentModel;
using Blue.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Blue.Core.ViewModels
{
	public abstract partial class MessageViewModel : ObservableObject
	{
		[ObservableProperty]
		private string messageText = "";

		protected IMessage Message;
		public MessageViewModel(IMessage message) 
		{ 
			this.Message = message;
			MessageText = this.Message.MessageText;
		}
	}
}
