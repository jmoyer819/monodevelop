// 
// AspNetParser.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MonoDevelop.Core;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;
using MonoDevelop.AspNet.Projects;
using MonoDevelop.AspNet.WebForms.Parser;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor.Projection;
using MonoDevelop.AspNet.WebForms.CSharp;
using MonoDevelop.Ide.Editor.Util;

namespace MonoDevelop.AspNet.WebForms
{
	public class WebFormsParser : TypeSystemParser
	{
		public override System.Threading.Tasks.Task<ParsedDocument> Parse (ParseOptions parseOptions, System.Threading.CancellationToken cancellationToken)
		{
			var info = new WebFormsPageInfo ();
			var errors = new List<Error> ();

			var parser = new XmlParser (
				new WebFormsRootState (),
				true
			);
			
			try {
				parser.Parse (parseOptions.Content.CreateReader ());
			} catch (Exception ex) {
				LoggingService.LogError ("Unhandled error parsing ASP.NET document '" + (parseOptions.FileName ?? "") + "'", ex);
				errors.Add (new Error (ErrorType.Error, "Unhandled error parsing ASP.NET document: " + ex.Message));
			}

			// get the errors from the StateEngine parser
			errors.AddRange (parser.Errors);

			// populating the PageInfo instance
			XDocument xDoc = parser.Nodes.GetRoot ();
			info.Populate (xDoc, errors);
			
			var type = AspNetAppProject.DetermineWebSubtype (parseOptions.FileName);
			if (type != info.Subtype) {
				if (info.Subtype == WebSubtype.None) {
					errors.Add (new Error (ErrorType.Error, "File directive is missing", new DocumentLocation (1, 1)));
				} else {
					type = info.Subtype;
					errors.Add (new Error (ErrorType.Warning, "File directive does not match page extension", new DocumentLocation (1, 1)));
				}
			}
			
			var result = new WebFormsParsedDocument (parseOptions.FileName, type, info, xDoc);
			result.AddRange (errors);
			
			return System.Threading.Tasks.Task.FromResult((ParsedDocument)result);
		}

		public override bool CanGenerateProjection (string mimeType, string buildAction, string[] supportedLanguages)
		{
			// TODO: Check mimeType and languages.
			return true;
		}

		public override Task<IReadOnlyList<Projection>> GenerateProjections (ParseOptions options, CancellationToken cancellationToken)
		{
			var task = GenerateParsedDocumentProjection (options, cancellationToken);
			return Task.FromResult (task.Result.Projections);
		}

		public override Task<ParsedDocumentProjection> GenerateParsedDocumentProjection (ParseOptions options, CancellationToken cancellationToken)
		{
			var parsedDocument = Parse (options, cancellationToken).Result as WebFormsParsedDocument;
			var documentInfo = new DocumentInfo (parsedDocument, GetUsings ());
			var document = SimpleReadonlyDocument.CreateReadonlyDocumentAsync (options.Content, options.FileName, "", cancellationToken).Result;
			var projection = new CSharpProjector ().CreateProjection (documentInfo, document, true).Result;
			var projections = new List<Projection> ();
			projections.Add (projection);
			parsedDocument.UpdateProjections (projections);
			var parsedDocumentProjection = new ParsedDocumentProjection (parsedDocument, projections);
			return Task.FromResult (parsedDocumentProjection);
		}

		public override Task<IReadOnlyList<Projection>> GetPartialProjectionsAsync (DocumentContext ctx, TextEditor editor, ParsedDocument currentParsedDocument, CancellationToken cancellationToken)
		{
//			var documentInfo = new DocumentInfo ((WebFormsParsedDocument)currentParsedDocument, GetUsings ());
//			var projection = new CSharpProjector ().CreateProjection (documentInfo, editor, true).Result;
//			projection.CreateProjectedEditor (ctx);
//			var projections = new List<Projection> ();
//			projections.Add (projection);
//			return Task.FromResult ((IReadOnlyList<Projection>)projections);
			var webFormsParsedDocument = (WebFormsParsedDocument)currentParsedDocument;
			return Task.FromResult (webFormsParsedDocument.Projections);
		}

		// HACk: Hard coded usings for now.
		static IEnumerable<string> GetUsings ()
		{
			return new [] {
				"System",
				"System.Web",
				"System",
				"System.Collections",
				"System.Collections.Specialized",
				"System.Configuration",
				"System.Text",
				"System.Text.RegularExpressions",
				"System.Web",
				"System.Web.Caching",
				"System.Web.Profile",
				"System.Web.Security",
				"System.Web.SessionState",
				"System.Web.UI",
				"System.Web.UI.HtmlControls",
				"System.Web.UI.WebControls",
				"System.Web.UI.WebControls.WebParts"
			};
		}
	}
}
