public class CommandArticle(CommandContext context, CosmosDb cosmosDb, CosmosDbDynamic cosmosDbDynamic, TableStorageClient tableStorageClient)
{
    public async Task Add()
    {
        var list = await UtilTableStorageDynamic.SingleOrDefaultAsync<AirportDto>(tableStorageClient.Client, "Global|AirportDto", "My");
        return;

        await context.UserAuthenticate();
        var article = new ArticleDto { Text = "Banana", Name = Guid.NewGuid().ToString() };
        
        article = await cosmosDb.InsertAsync(article);

        var articleList = await cosmosDb.Select<ArticleDto>().ToListAsync();
        var articleDynamicList = await cosmosDbDynamic.Select<ArticleDto>().ToListAsync();

        var articleDynamic = new Dictionary<string, object>
            {
                { "name", Guid.NewGuid().ToString() },
                { "text", "Apple" },
                { "price", 88 },
                { "new01", "Abc" },
            };
        var d = await cosmosDbDynamic.InsertAsync<ArticleDto>(articleDynamic);
    }
}

public class ArticleDto : DocumentDto
{
    public string? Text { get; set; }

    public int? Price { get; set; }
}

public class AirportDto : TableEntityDto
{
    public string? Name { get; set; }
}