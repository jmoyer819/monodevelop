//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//


using System;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.VisualStudio.Platform
{
    [Export(typeof(IWaitIndicator))]
    internal class WaitIndicator : IWaitIndicator
    {
        public IWaitContext StartWait (string title, string message, bool allowCancel, bool showProgress)
        {
            return new WaitContext ();
        }

        public WaitIndicatorResult Wait (string title, string message, bool allowCancel, bool showProgress, Action<IWaitContext> action)
        {
            using (var waitContext = StartWait (title, message, allowCancel, showProgress))
            {
                try
                {
                    action (waitContext);

                    return WaitIndicatorResult.Completed;
                }
                catch (OperationCanceledException)
                {
                    return WaitIndicatorResult.Canceled;
                }
                catch (AggregateException e)
                {
                    var operationCanceledException = e.InnerExceptions[0] as OperationCanceledException;
                    if (operationCanceledException != null)
                    {
                        return WaitIndicatorResult.Canceled;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        internal class WaitContext : IWaitContext
        {
            public CancellationToken CancellationToken => throw new NotImplementedException ();

            public bool AllowCancel { get => throw new NotImplementedException (); set => throw new NotImplementedException (); }
            public string Message { get => throw new NotImplementedException (); set => throw new NotImplementedException (); }

            public IProgressTracker ProgressTracker => throw new NotImplementedException ();

            public void Dispose ()
            {
                throw new NotImplementedException ();
            }
        }
    }
}