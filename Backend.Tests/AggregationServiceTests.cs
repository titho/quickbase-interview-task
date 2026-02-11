using Backend;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Backend.Tests;

public class AggregationServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;

    public AggregationServiceTests()
    {
        // In-memory SQLite — shared connection stays open for the test lifetime
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private IStatService MockStatService(params (string Country, int Population)[] data)
    {
        var mock = Substitute.For<IStatService>();
        mock.GetCountryPopulations()
            .Returns(data.Select(d => Tuple.Create(d.Country, d.Population)).ToList());
        return mock;
    }

    private void SeedDb(params (string Country, string State, string City, int Population)[] rows)
    {
        foreach (var (countryName, stateName, cityName, population) in rows)
        {
            var country = _context.Countries.FirstOrDefault(c => c.CountryName == countryName);
            if (country == null)
            {
                country = new Country { CountryName = countryName };
                _context.Countries.Add(country);
                _context.SaveChanges();
            }

            var state = _context.States.FirstOrDefault(s => s.StateName == stateName && s.CountryId == country.CountryId);
            if (state == null)
            {
                state = new State { StateName = stateName, CountryId = country.CountryId };
                _context.States.Add(state);
                _context.SaveChanges();
            }

            _context.Cities.Add(new City
            {
                CityName = cityName,
                StateId = state.StateId,
                Population = population
            });
            _context.SaveChanges();
        }
    }

    [Fact]
    public void Returns_StatService_Data_When_DB_Is_Empty()
    {
        var statService = MockStatService(
            ("Germany", 81_000_000),
            ("Chile", 17_000_000)
        );
        var sut = new AggregationService(_context, statService);

        var result = sut.GetCountryPopulations();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Item1 == "Germany" && r.Item2 == 81_000_000);
        Assert.Contains(result, r => r.Item1 == "Chile" && r.Item2 == 17_000_000);
    }

    [Fact]
    public void Returns_DB_Data_When_StatService_Is_Empty()
    {
        SeedDb(
            ("Germany", "Bavaria", "Munich", 1_500_000),
            ("Germany", "Bavaria", "Nuremberg", 500_000)
        );
        var statService = MockStatService();
        var sut = new AggregationService(_context, statService);

        var result = sut.GetCountryPopulations();

        Assert.Single(result);
        Assert.Equal("Germany", result[0].Item1);
        Assert.Equal(2_000_000, result[0].Item2);
    }

    [Fact]
    public void DB_Overrides_StatService_On_Duplicate_Country()
    {
        SeedDb(
            ("Germany", "Bavaria", "Munich", 1_500_000),
            ("Germany", "Berlin", "Berlin", 3_500_000)
        );
        var statService = MockStatService(("Germany", 81_000_000));
        var sut = new AggregationService(_context, statService);

        var result = sut.GetCountryPopulations();

        Assert.Single(result);
        Assert.Equal("Germany", result[0].Item1);
        // DB sum is 5_000_000, NOT the stat service's 81_000_000
        Assert.Equal(5_000_000, result[0].Item2);
    }

    [Fact]
    public void Combines_Unique_Countries_From_Both_Sources()
    {
        SeedDb(
            ("Germany", "Bavaria", "Munich", 1_500_000)
        );
        var statService = MockStatService(("Chile", 17_000_000));
        var sut = new AggregationService(_context, statService);

        var result = sut.GetCountryPopulations();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Item1 == "Germany" && r.Item2 == 1_500_000);
        Assert.Contains(result, r => r.Item1 == "Chile" && r.Item2 == 17_000_000);
    }

    [Fact]
    public void Returns_Empty_When_Both_Sources_Are_Empty()
    {
        var statService = MockStatService();
        var sut = new AggregationService(_context, statService);

        var result = sut.GetCountryPopulations();

        Assert.Empty(result);
    }

    [Fact]
    public void Sums_Population_Across_Multiple_Cities_And_States()
    {
        SeedDb(
            ("USA", "California", "Los Angeles", 4_000_000),
            ("USA", "California", "San Francisco", 800_000),
            ("USA", "New York", "New York City", 8_300_000),
            ("USA", "Texas", "Houston", 2_300_000)
        );
        var statService = MockStatService();
        var sut = new AggregationService(_context, statService);

        var result = sut.GetCountryPopulations();

        Assert.Single(result);
        Assert.Equal("USA", result[0].Item1);
        Assert.Equal(15_400_000, result[0].Item2);
    }

    [Fact]
    public void Multiple_Countries_In_DB_With_Partial_Overlap()
    {
        SeedDb(
            ("Germany", "Bavaria", "Munich", 1_500_000),
            ("France", "Ile-de-France", "Paris", 2_100_000)
        );
        var statService = MockStatService(
            ("Germany", 81_000_000),
            ("Chile", 17_000_000)
        );
        var sut = new AggregationService(_context, statService);

        var result = sut.GetCountryPopulations();

        Assert.Equal(3, result.Count);
        // Germany: DB overrides stat service
        Assert.Contains(result, r => r.Item1 == "Germany" && r.Item2 == 1_500_000);
        // France: DB only
        Assert.Contains(result, r => r.Item1 == "France" && r.Item2 == 2_100_000);
        // Chile: stat service only
        Assert.Contains(result, r => r.Item1 == "Chile" && r.Item2 == 17_000_000);
    }
}
