namespace CodeIndex.VS
{
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    [Export]
    internal sealed class SearchClientManager : IVsSolutionEvents
    {
        private readonly IVsSolution solution;

        [ImportingConstructor]
        public SearchClientManager(SVsServiceProvider serviceProvider)
        {
            // TODO: refactor to async (non-COM RPC, which can deadlock).
            this.solution = serviceProvider.GetService<SVsSolution, IVsSolution>();

            // TODO: eliminate JTF run, which causes UI delays.
            ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                this.solution.AdviseSolutionEvents(this, out _);
                ((IVsSolutionEvents)this).OnAfterOpenSolution(null, 0);
            });
        }

        public SearchClient SearchClient { get; } = new SearchClient();

        int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            this.SearchClient.WorkspaceDirectory = this.GetSolutionPath();
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterCloseSolution(object pUnkReserved)
        {
            this.SearchClient.WorkspaceDirectory = null;
            return VSConstants.S_OK;
        }

        private string? GetSolutionPath()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (ErrorHandler.Failed(this.solution.GetSolutionInfo(out var location, out _, out _)))
            {
                location = null;
            }

            return location;
        }
    }
}
