//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Text.Document;

    /// <summary>
    /// Creates editor views.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// ITextEditorFactoryService2 factory = null;
    /// </remarks>
    public interface ITextEditorFactoryService2
    {
        IWpfTextView CreateTextView (MonoDevelop.Ide.Editor.TextEditor textEditor, ITextViewRoleSet roles = null, IEditorOptions parentOptions = null);
    }
}
