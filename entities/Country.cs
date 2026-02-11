using System.Collections.Generic;

public class Country
{
    public int CountryId { get; set; }
    public required string CountryName { get; set; }
    public ICollection<State> States { get; set; } = [];
}