using Blue.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Blue.Core.ViewModels.Messages
{
	public partial class AssistantMessageViewModel : MessageViewModel
	{
		// Run code on the UI Thread in the ViewModel
		// This is useful to update ObservableProperties from background threads
		public Action<Action>? UIThread;

		public AssistantMessageViewModel(IMessage message) : base(message)
		{
		}

		public void Exception(Exception exception)
		{
			// Show error UI
		}
	}
}
