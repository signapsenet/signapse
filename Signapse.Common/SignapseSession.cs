using Signapse.Data;

namespace Signapse
{
    public class SignapseSession : IDisposable
    {
        public SignapseSession(Uri signapseServer, string applicationKey)
        {

        }

        public Task PutMember(Data.User member)
        {
            return Task.CompletedTask;
        }

        public Task DeleteMember(uint memberID)
        {
            return Task.CompletedTask;
        }

        public Task PutContent(Guid categoryID, ContentDecriptor descriptor)
        {
            return Task.CompletedTask;
        }

        public Task DeleteContent(Guid contentID)
        {
            return Task.CompletedTask;
        }

        public Task PutCategory(ContentCategory category)
        {
            return Task.CompletedTask;
        }

        public Task DeleteCategory(Guid categoryID)
        {
            return Task.CompletedTask;
        }

        public Task LogUsage(uint memberID, Guid contentID, TimeSpan duration)
        {
            return Task.CompletedTask;
        }

        public Task<SignapseServerDescriptor?> GetAffiliate(Guid affiliateID)
        {
            return Task.FromResult((SignapseServerDescriptor?)null);
        }

        public Task<ContentCategory[]> GetCategories()
        {
            return Task.FromResult(new ContentCategory[0]);
        }

        public Task<ContentDecriptor[]> GetContent(Guid categoryID, Guid contentID)
        {
            return Task.FromResult(new ContentDecriptor[0]);
        }

        public Task<SearchResult> Search(Guid categoryID, Guid affiliateID, string search)
        {
            return Task.FromResult(new SearchResult());
        }

        public void Dispose()
        {

        }
    }

    public class SearchResult
    {
        public Guid SearchID { get; set; }

        public uint OffsetEntries { get; set; }
        public uint TotalEntries { get; set; }
        public string Search { get; set; } = string.Empty;

        public ContentDecriptor[] Results { get; set; } = { };
    }

    public class ContentCategory
    {
        public Guid CategoryID { get; set; } = Guid.NewGuid();
        public Guid? AffiliateID { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Icon { get; set; }
    }

    public class ContentDecriptor
    {
        public Guid ContentID { get; set; } = Guid.NewGuid();
        public Guid CategoryID { get; set; }

        public string ContentType { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Thumbnail { get; set; }
    }
}
