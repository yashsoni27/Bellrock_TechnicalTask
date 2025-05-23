﻿namespace DotNetInterview.API.Domain;

public record Item
{
    public Guid Id { get; set; }
    public string Reference { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public decimal? CurrentPrice {get; set; } = 0;
    public string? Status { get; set; }
    public ICollection<Variation> Variations { get; set; } = new List<Variation>();
}
