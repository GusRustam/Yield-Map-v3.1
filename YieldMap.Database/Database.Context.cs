﻿//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace YieldMap.Database
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class MainEntities : DbContext
    {
        public MainEntities()
            : base("name=MainEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public DbSet<Chain> Chains { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<Feed> Feeds { get; set; }
        public DbSet<Industry> Industries { get; set; }
        public DbSet<InstrumentType> InstrumentTypes { get; set; }
        public DbSet<Isin> Isins { get; set; }
        public DbSet<LegType> LegTypes { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<RatingAgency> RatingAgencies { get; set; }
        public DbSet<RatingAgencyCode> RatingAgencyCodes { get; set; }
        public DbSet<RicToChain> RicToChains { get; set; }
        public DbSet<Seniority> Seniorities { get; set; }
        public DbSet<Specimen> Specimens { get; set; }
        public DbSet<SubIndustry> SubIndustries { get; set; }
        public DbSet<Ticker> Tickers { get; set; }
        public DbSet<LegalEntity> LegalEntities { get; set; }
        public DbSet<RatingToInstrument> RatingToInstruments { get; set; }
        public DbSet<RatingToLegalEntity> RatingToLegalEntities { get; set; }
        public DbSet<Ric> Rics { get; set; }
        public DbSet<Description> Descriptions { get; set; }
        public DbSet<Instrument> Instruments { get; set; }
        public DbSet<Field> Fields { get; set; }
        public DbSet<FieldDefinition> FieldDefinitions { get; set; }
        public DbSet<FieldGroup> FieldGroups { get; set; }
        public DbSet<PropertyValue> PropertyValues { get; set; }
        public DbSet<Property> Properties { get; set; }
        public DbSet<Idx> Idxes { get; set; }
        public DbSet<Leg> Legs { get; set; }
    }
}
