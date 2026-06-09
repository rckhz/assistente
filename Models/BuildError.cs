namespace the_bolotas.Models;

public class BuildError
{
    public string Code { get; set; } = "";
    public string File { get; set; } = "";
    public int Line { get; set; }
    public string Message { get; set; } = "";
}