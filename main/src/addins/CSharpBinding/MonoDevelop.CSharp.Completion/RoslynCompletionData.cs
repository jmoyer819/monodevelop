﻿//
// CompletionDataWrapper.cs
//
// Author:
//       mkrueger <>
//
// Copyright (c) 2017 ${CopyrightHolder}
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide;
using Microsoft.CodeAnalysis.CSharp.Completion;
using MonoDevelop.Ide.Gui;
using Microsoft.VisualStudio.Platform;
using Microsoft.VisualStudio.Text;
using MonoDevelop.Ide.CodeTemplates;
using System.Linq;
using System.Text;
using MonoDevelop.Ide.Editor.Highlighting;

namespace MonoDevelop.CSharp.Completion
{
	class RoslynCompletionData : CompletionData
	{
		readonly Microsoft.CodeAnalysis.Document doc;
		readonly ITextSnapshot triggerBuffer;
		readonly CompletionService completionService;
	
		public CompletionItem CompletionItem { get; private set; }

		Lazy<CompletionProvider> provider;

		public CompletionProvider Provider {
			get {
				return provider.Value;
			}
		}

		public RoslynCompletionData (Microsoft.CodeAnalysis.Document document, ITextBuffer triggerBuffer, CompletionService completionService, CompletionItem completionItem)
		{
			this.doc = document;
			this.triggerBuffer = triggerBuffer.CurrentSnapshot;
			this.completionService = completionService;
			CompletionItem = completionItem;
			provider = new Lazy<CompletionProvider> (delegate {
				return ((CSharpCompletionService)completionService).GetProvider (CompletionItem);
			});
		}

		public override bool MuteCharacter (char keyChar, string partialWord)
		{
			return false;
		}

		public override string DisplayText {
			get {
				return CompletionItem.DisplayText;
			}
		}

		public override string Description {
			get {
				var description = completionService.GetDescriptionAsync (doc, CompletionItem).Result;
				return description.Text;
			}
		}

		public override string CompletionText {
			get {
				return CompletionItem.DisplayText;
			}
		}


		public override IconId Icon {
			get {
				if (CompletionItem.Tags.Contains ("Snippet")) {
					var template = CodeTemplateService.GetCodeTemplates (CSharp.Formatting.CSharpFormatter.MimeType).FirstOrDefault (t => t.Shortcut == CompletionItem.DisplayText);
					if (template != null)
						return template.Icon;
				}
				var modifier = GetItemModifier ();
				var type = GetItemType ();
				return "md-" + modifier + type;
			}
		}

		public override string GetDisplayDescription (bool isSelected)
		{
			if (CompletionItem.Properties.TryGetValue ("DescriptionMarkup", out string result))
				return result;
			return base.GetDisplayDescription (isSelected);
		}

		public override string GetRightSideDescription (bool isSelected)
		{
			if (CompletionItem.Properties.TryGetValue ("RightSideMarkup", out string result))
				return result;
			return null;
		}

		public override bool IsCommitCharacter (char keyChar, string partialWord)
		{
			return base.IsCommitCharacter (keyChar, partialWord);
		}

		public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, KeyDescriptor descriptor)
		{
			var editor = IdeApp.Workbench.ActiveDocument?.Editor;
			if (editor == null || Provider == null) {
				base.InsertCompletionText (window, ref ka, descriptor);
				return;
			}

			var completionChange = Provider.GetChangeAsync (doc, CompletionItem, null, default (CancellationToken)).WaitAndGetResult (default (CancellationToken));
			var currentBuffer = editor.GetPlatformTextBuffer ();

			var textChange = completionChange.TextChange;
			var triggerSnapshotSpan = new SnapshotSpan (triggerBuffer, new Microsoft.VisualStudio.Text.Span (textChange.Span.Start, textChange.Span.Length));
			var mappedSpan = triggerSnapshotSpan.TranslateTo (currentBuffer.CurrentSnapshot, SpanTrackingMode.EdgeInclusive);
			window.CompletionWidget.Replace (mappedSpan.Start, mappedSpan.Length, completionChange.TextChange.NewText);

			if (completionChange.NewPosition.HasValue)
				window.CompletionWidget.CaretOffset = completionChange.NewPosition.Value;
		}

		static Dictionary<string, string> roslynCompletionTypeTable = new Dictionary<string, string> {
			{ "Field", "field" },
			{ "Alias", "field" },
			{ "ArrayType", "field" },
			{ "Assembly", "field" },
			{ "DynamicType", "field" },
			{ "ErrorType", "field" },
			{ "Label", "field" },
			{ "NetModule", "field" },
			{ "PointerType", "field" },
			{ "RangeVariable", "field" },
			{ "TypeParameter", "field" },
			{ "Preprocessing", "field" },

			{ "Constant", "literal" },

			{ "Parameter", "variable" },
			{ "Local", "variable" },

			{ "Method", "method" },

			{ "Namespace", "name-space" },

			{ "Property", "property" },

			{ "Event", "event" },

			{ "Class", "class" },

			{ "Delegate", "delegate" },

			{ "Enum", "enum" },

			{ "Interface", "interface" },

			{ "Struct", "struct" },

			{ "Keyword", "keyword" },

			{ "Snippet", "template"}
		};

		string GetItemType ()
		{
			foreach (var tag in CompletionItem.Tags) {
				if (roslynCompletionTypeTable.TryGetValue (tag, out string result))
					return result;
			}
			LoggingService.LogWarning ("RoslynCompletionData: Can't find item type ' "+ string.Join (",", CompletionItem.Tags) + "'");
			return "literal";
		}

		static Dictionary<string, string> modifierTypeTable = new Dictionary<string, string> {
			{ "Private", "private-" },
			{ "ProtectedAndInternal", "ProtectedOrInternal-" },
			{ "Protected", "protected-" },
			{ "Internal", "internal-" },
			{ "ProtectedOrInternal", "ProtectedOrInternal-" }
		};

		string GetItemModifier ()
		{
			foreach (var tag in CompletionItem.Tags) {
				if (modifierTypeTable.TryGetValue (tag, out string result))
					return result;
			}
			return "";
		}

		public override DisplayFlags DisplayFlags {
			get {
				DisplayFlags result = DisplayFlags.None;

				if (CompletionItem.Tags.Contains ("bold")) {
					result = DisplayFlags.MarkedBold;
				}

				return result;
			}
		}

		public override async Task<TooltipInformation> CreateTooltipInformation (bool smartWrap, CancellationToken cancelToken)
		{
			var tt = new TooltipInformation ();
			var description = await completionService.GetDescriptionAsync (doc, CompletionItem);
			var markup = new StringBuilder ();
			var theme = DefaultSourceEditorOptions.Instance.GetEditorTheme ();

			foreach (var part in description.TaggedParts) {
				markup.Append ("<span foreground=\"");
				markup.Append (GetThemeColor (theme, GetThemeColor (part.Tag)));
				markup.Append ("\">");
				markup.Append (part.Text);
				markup.Append ("</span>");
			}

			tt.SignatureMarkup = markup.ToString ();
			return tt;
		}

		static string GetThemeColor (string tag)
		{
			switch (tag) {
			case TextTags.Keyword:
				return "keyword";

			case TextTags.Class:
				return EditorThemeColors.UserTypes;
			case TextTags.Delegate:
				return EditorThemeColors.UserTypesDelegates;
			case TextTags.Enum:
				return EditorThemeColors.UserTypesEnums;
			case TextTags.Interface:
				return EditorThemeColors.UserTypesInterfaces;
			case TextTags.Module:
				return EditorThemeColors.UserTypes;
			case TextTags.Struct:
				return EditorThemeColors.UserTypesValueTypes;
			case TextTags.TypeParameter:
				return EditorThemeColors.UserTypesTypeParameters;

			case TextTags.Alias:
			case TextTags.Assembly:
			case TextTags.Field:
			case TextTags.ErrorType:
			case TextTags.Event:
			case TextTags.Label:
			case TextTags.Local:
			case TextTags.Method:
			case TextTags.Namespace:
			case TextTags.Parameter:
			case TextTags.Property:
			case TextTags.RangeVariable:
				return "source.cs";

			case TextTags.NumericLiteral:
				return "constant.numeric";

			case TextTags.StringLiteral:
				return "string.quoted";

			case TextTags.Space:
			case TextTags.LineBreak:
				return "source.cs";

			case TextTags.Operator:
				return "keyword.source";

			case TextTags.Punctuation:
				return "punctuation";

			case TextTags.AnonymousTypeIndicator:
			case TextTags.Text:
				return "source.cs";

			default:
				LoggingService.LogWarning ("Warning unexpected text tag: " + tag);
				return "source.cs";
			}
		}

		static string GetThemeColor (EditorTheme theme, string scope)
		{
			return SyntaxHighlightingService.GetColorFromScope (theme, scope, EditorThemeColors.Foreground).ToPangoString ();
		}
	}
}