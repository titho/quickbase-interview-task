using System;
using Backend;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("Started");
Console.WriteLine("Getting DB Connection...");

var services = new ServiceCollection();

services.AddDbContext<AppDbContext>(o => o.UseSqlite("Data Source=app.db"));
services.AddScoped<IStatService, ConcreteStatService>();
services.AddScoped<IAggregationService, AggregationService>();

var provider = services.BuildServiceProvider();
var database = provider.GetRequiredService<AppDbContext>();

database.Database.EnsureCreated();

Console.WriteLine("Connected");

var aggregator = provider.GetRequiredService<IAggregationService>();

var result = aggregator.GetCountryPopulations();

foreach (var item in result)
{
    Console.WriteLine(item.Item1 + " " + item.Item2);
}


