using System.ComponentModel.DataAnnotations;

public class EmailSettings
{
    [Required]
    public string DisplayName { get; set; } = "";

    [Required]
    [EmailAddress]
    public string From { get; set; } = "";

    public List<string> AdminEmails { get; set; } = new();

    [Required]
    public string ApiKey { get; set; } = "";
}