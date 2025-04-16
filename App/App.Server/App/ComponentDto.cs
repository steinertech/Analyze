public class ComponentDto
{
    public List<ComponentDto>? List { get; set; }

    private void listAll(List<ComponentDto> result)
    {
        result.Add(this);
        if (List != null) 
        {
            foreach (var item in List)
            {
                item.listAll(result);
            }
        }
    }

    public List<ComponentDto> ListAll()
    {
        var result = new List<ComponentDto>();
        listAll(result);
        return result;
    }
}

public class ComponentTextDto : ComponentDto
{
    public string? Text { get; set; }
}

public class ComponentButtonDto : ComponentDto
{
    public bool? IsClick { get; set; }
}
