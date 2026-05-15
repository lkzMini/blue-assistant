using Blue.Core.Classes;
using Blue.Core.ViewModels.Messages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blue.Helpers
{
    public partial class MessageSelector : DataTemplateSelector
	{
		public DataTemplate AssistantMessageTemplate { get; set; }
		public DataTemplate UserMessageTemplate { get; set; }
		public DataTemplate SystemMessageTemplate { get; set; }

		protected override DataTemplate SelectTemplateCore(object item)
		{
			return item switch
			{
				AssistantMessageViewModel => AssistantMessageTemplate,
				UserMessageViewModel => UserMessageTemplate,
				SystemMessageViewModel => SystemMessageTemplate,
				_ => base.SelectTemplateCore(item)
			};
		}
	}
}
