namespace CodeIndex.VS.VSSearch
{
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Search.Data;
    using Microsoft.VisualStudio.Utilities;

    [Export(typeof(ISearchItemsSourceProvider))]
    [Name("Full text search source provider")]
    [ProducesResultType(CodeSearchResultType.File)]
    internal sealed class SearchItemsSourceProvider : ISearchItemsSourceProvider
    {
        private readonly SearchClientManager searchClientManager;

        [ImportingConstructor]
        public SearchItemsSourceProvider(SearchClientManager searchClientManager)
        {
            this.searchClientManager = searchClientManager;
        }

        public ISearchItemsSource CreateItemsSource() => new SearchItemsSource(this.searchClientManager.SearchClient);
    }
}
