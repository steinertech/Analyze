namespace App.Server.App.Command
{
    public class CommandArticle(CommandContext context, CosmosDb2 cosmosDb)
    {
        public async Task Add()
        {
            await context.UserSignInOrganisation();
            var article = new ArticleDto { Text = "Banana", Name = Guid.NewGuid().ToString() };
            article = await cosmosDb.InsertAsync(article);
        }
    }
}
