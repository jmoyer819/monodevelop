//
// XmlCompletionKeyHandler.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor.Extension;

namespace MonoDevelop.Xml.Editor
{
	/// <summary>
	/// This key handler prevents the ProjectedCompletionExtension from closing the
	/// completion window when an opening tag &lt; is typed in.
	/// </summary>
	class XmlCompletionKeyHandler : ICompletionKeyHandler
	{
		/// <summary>
		/// Prevent typing in an opening tag inserting the completion text when part of an 
		/// element name is typed in. This maintains the existing XML editor behaviour.
		/// </summary>
		public bool PreProcessKey (CompletionListWindow listWindow, KeyDescriptor descriptor, out KeyActions keyAction)
		{
			keyAction = KeyActions.None;
			if (descriptor.KeyChar == '<' && !string.IsNullOrEmpty (listWindow.PartialWord)) {
				keyAction = KeyActions.CloseWindow;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Prevent the ProjectedCompletionExtension from closing the completion window when
		/// an opening tag is typed in by preventing further processing of the key press.
		/// </summary>
		public bool PostProcessKey (CompletionListWindow listWindow, KeyDescriptor descriptor, out KeyActions keyAction)
		{
			keyAction = KeyActions.None;
			if (descriptor.KeyChar == '<') {
				return true;
			}
			return false;
		}
	}
}

