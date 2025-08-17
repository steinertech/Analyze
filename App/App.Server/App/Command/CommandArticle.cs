namespace App.Server.App.Command
{
    public class CommandArticle(CommandContext context, CosmosDb cosmosDb)
    {
        public async Task Add()
        {
            if (await context.IsUserSignIn(cosmosDb))
            {
                var article = new ArticleDto { Text = "Banana", Name = Guid.NewGuid().ToString() };
                article = await cosmosDb.InsertAsync(context, article);
            }
        }
    }
}
