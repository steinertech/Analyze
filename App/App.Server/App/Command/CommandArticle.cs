public class CommandArticle(CommandContext context, CosmosDb cosmosDb, CosmosDbDynamic cosmosDbDynamic, TableStorage tableStorage)
{
    public async Task Add()
    {
        await context.UserAuthenticateAsync();
        var article = new ArticleDto { Id = Guid.NewGuid().ToString(), Text = "Banana", Name = Guid.NewGuid().ToString() };
        
        article = await cosmosDb.InsertAsync(article);

        var articleList = await cosmosDb.Select<ArticleDto>().ToListAsync();
        var articleDynamicList = await cosmosDbDynamic.Select<ArticleDto>().ToListAsync();

        var articleDynamic = new Dictionary<string, object>
            {
                { "id", Guid.NewGuid().ToString() },
                { "name", Guid.NewGuid().ToString() },
                { "text", "Apple" },
                { "price", 88 },
                { "new01", "Abc" },
            };
         await cosmosDbDynamic.InsertAsync<ArticleDto>(articleDynamic);
    }
}

public class ArticleDto : DocumentDto
{
    public string? Text { get; set; }

    public int? Price { get; set; }
}
