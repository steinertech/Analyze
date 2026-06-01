public class CommandJob(CommandContextService context, StorageService storage, CosmosDbService cosmosDb)
{
    public async Task Run()
    {
        // Job logo
        await context.UserAuthAsync();
        var list = await storage.List();
        var logo = list.Where(item => item.IsFolder == false && item.FolderOrFileName == "logo.png").FirstOrDefault();
        if (logo != null)
        {
            var data = await storage.Download(logo.FolderOrFileName);
        }
        // Job count
        var job = await cosmosDb.SelectByNameAsync<GridJobDto>("JobCount");
        if (job == null)
        {
            job = await cosmosDb.InsertAsync(new GridJobDto() { Id = Guid.NewGuid().ToString(), Name = "JobCount", Status = GridJobStatus.Scheduled });
        }
        else
        {
            var dateText = DateTime.UtcNow.ToString("yyyy-dd-MM HH:mm:ss");
            if ((job.Text?.Length ?? 0) < 19)
            {
                job.Text = dateText + " - 1";
            }
            else
            {
                var count = 1;
                if (int.TryParse(job.Text?.Substring(dateText.Length + 3), out count))
                {
                    count += 1;
                }
                job.Text = dateText + " - " + count.ToString();
            }
            job.Status = GridJobStatus.Running;
            job = await cosmosDb.UpdateAsync(job);
        }
    }
}
