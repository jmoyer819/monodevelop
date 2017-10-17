using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MonoDevelop.Ide.MonoDevelop.Ide.Composition
{
	[Export(typeof(IAsyncCompletionService))]
	class AsyncCompletionService : IAsyncCompletionService
	{
		public bool TryGetController (ITextView textView, ITextBuffer subjectBuffer, out Controller controller)
		{
			controller = null;
			return false;
		}
	}
}
