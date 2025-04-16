public class CommandTree
{
    public ComponentDto Run(ComponentDto? component)
    {
        if (component == null)
        {
            component = new ComponentDto();
            component.List = [new ComponentTextDto { Text = "Hello" }];
        }
        foreach (var item in component.ListAll())
        {
            if (item is ComponentTextDto label)
            {
                label.Text += ".";
            }
        }
        return component;
    }
}
