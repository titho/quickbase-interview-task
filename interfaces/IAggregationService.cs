using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend;

public interface IAggregationService
{
    List<Tuple<string, int>> GetCountryPopulations();
    Task<List<Tuple<string, int>>> GetCountryPopulationsAsync();
}