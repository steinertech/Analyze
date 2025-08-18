namespace App.Server.App.Command
{
    public class CommandArticle(CosmosDb2 cosmosDb)
    {
        public async Task Add()
        {
            var article = new ArticleDto { Text = "Banana", Name = Guid.NewGuid().ToString() };
            article = await cosmosDb.InsertAsync(article);
        }
    }
}
