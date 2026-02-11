using System.Collections.Generic;

public class State
{
    public int StateId { get; set; }
    public required string StateName { get; set; }
    public int CountryId { get; set; }
    public Country Country { get; set; } = null!;
    public ICollection<City> Cities { get; set; } = [];
}