using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Backend;

public class AggregationService(AppDbContext context, IStatService statService) : IAggregationService
{
    public List<Tuple<string, int>> GetCountryPopulations()
    {
        var statResults = statService.GetCountryPopulations().ToDictionary(x => x.Item1, x => x.Item2);

        var dbResults = context.Countries.Select(c => new
        {
            c.CountryName,
            Population = c.States.SelectMany(s => s.Cities).Sum(ci => ci.Population)
        }).ToList();

        foreach (var item in dbResults)
        {
            statResults[item.CountryName] = item.Population;
        }

        return statResults.Select(x => Tuple.Create(x.Key, x.Value)).ToList();
    }

    public async Task<List<Tuple<string, int>>> GetCountryPopulationsAsync()
    {
        var statResults = statService.GetCountryPopulations().ToDictionary(x => x.Item1, x => x.Item2);

        var dbResults = await context.Countries.Select(c => new
        {
            c.CountryName,
            Population = c.States.SelectMany(s => s.Cities).Sum(ci => ci.Population)
        }).ToListAsync();

        foreach (var item in dbResults)
        {
            statResults[item.CountryName] = item.Population;
        }

        return statResults.Select(x => Tuple.Create(x.Key, x.Value)).ToList();
    }
}