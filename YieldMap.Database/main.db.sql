--SQLite Maestro 12.1.0.1
------------------------------------------
--Host     : localhost
--Database : C:\Users\Rustam Guseynov\Documents\Visual Studio 2012\Projects\Yield Map v3.1\YieldMap.Database\main.db


CREATE TABLE Borrower (
  id          integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name        varchar(50) NOT NULL,
  id_Country  integer NOT NULL,
  /* Foreign keys */
  FOREIGN KEY (id_Country)
    REFERENCES Country(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
);

CREATE INDEX RefBorrower_Index01
  ON Borrower
  (id);

CREATE INDEX RefBorrower_Index02
  ON Borrower
  (id);

CREATE TABLE Chain (
  id        integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name      varchar(50) NOT NULL UNIQUE,
  id_Feed   integer,
  Expanded  date,
  /* Foreign keys */
  FOREIGN KEY (id_Feed)
    REFERENCES Feed(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
);

CREATE INDEX RefChain_Index01
  ON Chain
  (id);

CREATE INDEX RefChain_Index02
  ON Chain
  (Name);

CREATE TABLE Country (
  id    integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name  varchar(50) NOT NULL UNIQUE
);

CREATE INDEX RefCountry_Index01
  ON Country
  (id);

CREATE INDEX RefCountry_Index02
  ON Country
  (Name);

CREATE TABLE Currency (
  id    integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name  varchar(50)
);

CREATE INDEX RefCurrency_Index01
  ON Currency
  (id);

CREATE TABLE Feed (
  id           integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name         varchar(50),
  Description  varchar(50)
);

CREATE TABLE Industry (
  id    integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name  varchar(50) UNIQUE
);

CREATE INDEX RefIndustry_Index01
  ON Industry
  (id);

CREATE INDEX RefIndustry_Index02
  ON Industry
  (Name);

CREATE TABLE InstrumentBond (
  id              integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  id_Issuer       integer,
  id_Borrower     integer,
  id_Currency     integer,
  BondStructure   text,
  RateStructure   text,
  IssueSize       integer,
  Name            varchar(50) NOT NULL,
  IsCallable      bit,
  IsPutable       bit,
  Series          varchar(50),
  id_Isin         integer,
  id_Ric          integer,
  id_Ticker       integer,
  id_SubIndustry  integer,
  id_Type         integer,
  Issue           date,
  Maturity        date,
  id_Seniority    integer,
  /* Foreign keys */
  FOREIGN KEY (id_Seniority)
    REFERENCES Seniority(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT, 
  FOREIGN KEY (id_Issuer)
    REFERENCES Issuer(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT, 
  FOREIGN KEY (id_Isin)
    REFERENCES Isin(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT, 
  FOREIGN KEY (id_Ric)
    REFERENCES Ric(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT, 
  FOREIGN KEY (id_Ticker)
    REFERENCES Ticker(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT, 
  FOREIGN KEY (id_SubIndustry)
    REFERENCES SubIndustry(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT, 
  FOREIGN KEY (id_Type)
    REFERENCES TypeOfInstrument(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT, 
  FOREIGN KEY (id_Borrower)
    REFERENCES Borrower(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT, 
  FOREIGN KEY (id_Currency)
    REFERENCES Currency(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
);

CREATE INDEX InstrumentBond_Index01
  ON InstrumentBond
  (id);

CREATE TABLE InstrumentCustomBond (
  id             integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name           varchar(50),
  BondStructure  text,
  RateStructure  text,
  id_Currency    integer,
  Issue          date,
  Maturity       date,
  /* Foreign keys */
  FOREIGN KEY (id_Currency)
    REFERENCES Currency(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
);

CREATE INDEX InstrumentCustomBond_Index01
  ON InstrumentCustomBond
  (id);

CREATE TABLE Isin (
  id       integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name     varchar(50) NOT NULL UNIQUE,
  id_Feed  integer,
  /* Foreign keys */
  FOREIGN KEY (id_Feed)
    REFERENCES Feed(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
);

CREATE INDEX RefIsin_Index01
  ON Isin
  (id);

CREATE INDEX RefIsin_Index02
  ON Isin
  (Name);

CREATE TABLE Issuer (
  id          integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name        varchar(50) NOT NULL,
  id_Country  integer NOT NULL,
  /* Foreign keys */
  FOREIGN KEY (id_Country)
    REFERENCES Country(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
);

CREATE INDEX RefIssuer_Index01
  ON Issuer
  (id);

CREATE TABLE Rating (
  id               integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Value            integer NOT NULL,
  Name             varchar(50) UNIQUE,
  id_RatingAgency  integer,
  /* Foreign keys */
  FOREIGN KEY (id)
    REFERENCES RatingAgency(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
);

CREATE INDEX RefRating_Index01
  ON Rating
  (id);

CREATE INDEX RefRating_Index02
  ON Rating
  (Name);

CREATE TABLE RatingAgency (
  id    integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name  varchar(50) NOT NULL UNIQUE
);

CREATE INDEX RefRatingAgency_Index01
  ON RatingAgency
  (id);

CREATE INDEX RefRatingAgency_Index02
  ON RatingAgency
  (Name);

CREATE TABLE RatingToBond (
  id         integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  id_Rating  integer,
  id_Bond    integer,
  /* Foreign keys */
  FOREIGN KEY (id_Bond)
    REFERENCES InstrumentBond(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT, 
  FOREIGN KEY (id_Rating)
    REFERENCES Rating(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
);

CREATE TABLE RawBondInfo (
  id               integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  BondStructure    text,
  RateStructure    text,
  IssueSize        integer,
  IssuerName       varchar(50),
  BorrowerName     varchar(50),
  Coupon           float(50),
  Issue            date,
  Maturity         date,
  Currency         varchar(50),
  ShortName        varchar(50),
  IsCallable       bit NOT NULL,
  IsPutable        bit NOT NULL,
  IsFloater        bit NOT NULL,
  IsConvertible    bit NOT NULL,
  IsStraight       bit NOT NULL,
  Ticker           varchar(50),
  Series           varchar(50),
  BorrowerCountry  varchar(50),
  IssuerCountry    varchar(50),
  Isin             varchar(50),
  ParentTicker     varchar(50),
  Seniority        varchar(50),
  Industry         varchar(50),
  SubIndustry      varchar(50),
  Instrument       varchar(50),
  Ric              text
);

CREATE INDEX RawBondInfo_Index01
  ON RawBondInfo
  (id);

CREATE TABLE RawFrnData (
  id          integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Cap         float(50),
  Floor       float(50),
  Margin      float(50),
  "Index"     varchar(50),
  id_RawBond  integer,
  Frequency   text,
  /* Foreign keys */
  FOREIGN KEY (id_RawBond)
    REFERENCES RawBondInfo(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
);

CREATE INDEX RawFrnData_Index01
  ON RawFrnData
  (id);

CREATE TABLE RawRating (
  id          integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  "Date"      date,
  Rating      varchar(50),
  Source      varchar(50),
  Issue       bit,
  id_RawBond  integer,
  /* Foreign keys */
  FOREIGN KEY (id_RawBond)
    REFERENCES RawBondInfo(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
);

CREATE INDEX RawRating_Index01
  ON RawRating
  (id);

CREATE TABLE Ric (
  id       integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name     varchar(50) NOT NULL,
  Isin_id  integer,
  Feed_id  integer,
  /* Foreign keys */
  FOREIGN KEY (Feed_id)
    REFERENCES Feed(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT, 
  FOREIGN KEY (Isin_id)
    REFERENCES Isin(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
);

CREATE INDEX RefRic_Index01
  ON Ric
  (id);

CREATE TABLE RicToChain (
  id        integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Ric_id    integer NOT NULL,
  Chain_id  integer NOT NULL,
  /* Foreign keys */
  FOREIGN KEY (Ric_id)
    REFERENCES Ric(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT, 
  FOREIGN KEY (Chain_id)
    REFERENCES Chain(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
);

CREATE INDEX RefRicToChain_Index01
  ON RicToChain
  (id);

CREATE TABLE Seniority (
  id    integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name  varchar(50) NOT NULL UNIQUE
);

CREATE INDEX RefSeniority_Index01
  ON Seniority
  (id);

CREATE INDEX RefSeniority_Index02
  ON Seniority
  (Name);

CREATE TABLE SubIndustry (
  id           integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name         varchar(50) NOT NULL UNIQUE,
  id_Industry  integer NOT NULL,
  /* Foreign keys */
  FOREIGN KEY (id_Industry)
    REFERENCES Industry(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
);

CREATE INDEX RefSubIndustry_Index01
  ON SubIndustry
  (id);

CREATE INDEX RefSubIndustry_Index02
  ON SubIndustry
  (Name);

CREATE TABLE Ticker (
  id               integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name             varchar(50) NOT NULL UNIQUE,
  id_ParentTicker  integer,
  /* Foreign keys */
  FOREIGN KEY (id_ParentTicker)
    REFERENCES Ticker(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
);

CREATE INDEX RefTicker_Index01
  ON Ticker
  (id);

CREATE INDEX RefTicker_Index02
  ON Ticker
  (Name);

