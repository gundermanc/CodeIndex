namespace CodeIndex.VS.VSSearch
{
    using Microsoft.VisualStudio.Search.Data;
    using System;
    using System.Collections.Immutable;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class SearchItemsSource : ISearchItemsSource
    {
        private readonly SearchClient searchClient;

        public SearchItemsSource(SearchClient searchClient)
        {
            this.searchClient = searchClient
                ?? throw new ArgumentNullException(nameof(searchClient));
        }

        public void Dispose()
        {
        }

        public void InvokeResult(string resultId)
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> IsResultApplicableAsync(string resultId, CancellationToken cancellationToken) => Task.FromResult(true);

        public async Task PerformSearchAsync(ISearchQuery searchQuery, ISearchCallback searchCallback, CancellationToken cancellationToken)
        {
            var result = await this.searchClient.SearchAsync(searchQuery.QueryString, cancellationToken);

            foreach (var match in result.Matches)
            {
                searchCallback.AddItem(
                    new CodeSearchResult(
                        CodeSearchResult.CreateResultId(match.Value.Word, match.Value.FileName, 0, 0),
                        CodeSearchResultType.File,
                        primarySortText: match.Value.Word,
                        view: new SearchResultView(match.Value.Word, match.Value.FileName)));
            }
        }

        public Task PopulateSearchResultViewsAsync(
            ImmutableArray<SearchResult> searchResults,
            CancellationToken cancellationToken) => Task.CompletedTask;

        public Task WarmupSearchAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
