namespace CodeIndex.VS.VSSearch
{
    using System.Threading;
    using Microsoft.VisualStudio.Search.Data;

    internal sealed class SearchResultView : SearchResultViewBase
    {
        public SearchResultView(string title, string description) : base(title, description)
        {
        }

        public override void Invoke(CancellationToken cancellationToken)
        {
            // HACK: no need to implement, VS has a magic contract where this is implicitly
            // defined for any CodeSearchResult with a result id of the correct format.
        }
    }
}
