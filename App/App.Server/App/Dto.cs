public class RequestDto
{
    public string? CommandName { get; set; }
}

public class ResponseDto
{
    public string? CommandName { get; set; }

    public string? ExceptionText { get; set; }
}